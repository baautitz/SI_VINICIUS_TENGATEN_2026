"use client"

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  useSyncExternalStore,
} from "react"
import type { ReactNode } from "react"
import {
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogFooter,
  Button,
  Kbd,
  KbdGroup,
} from "@/ui/primitives"
import { WindowController } from "@/ui/imperative/controller"
import { notifyWithSonner } from "@/ui/adapters/feedback-sonner"
import { useNextNavigation } from "@/ui/adapters/navigation-next"
import type {
  ConfirmOptions,
  FocusTarget,
  OpenWindowOptions,
  Ui,
  WindowCloseReason,
  WindowId,
  WindowManager,
  WindowResult,
  WindowSnapshotEntry,
} from "./types"
import type { WindowOptions } from "@/ui/imperative/contracts"
import { useWindow } from "./active-context"

export type WindowRecord = WindowSnapshotEntry

interface WindowRuntimeValue {
  controller: WindowController
  layers: readonly WindowRecord[]
  activeWindowId: WindowId | null
  requestClose: (
    id: WindowId,
    reason: Extract<WindowCloseReason, "cancel" | "escape" | "outside">,
  ) => void
  resolve: (id: WindowId, result: WindowResult<unknown>) => void
  setDirty: (id: WindowId, dirty: boolean) => void
}

const WindowManagerContext = createContext<WindowManager | null>(null)
const WindowRuntimeContext = createContext<WindowRuntimeValue | null>(null)

function restoreFocusTarget(target: FocusTarget | null): void {
  if (!target || !target.isConnected) return
  const isBlocked = () =>
    typeof HTMLElement !== "undefined" &&
    target instanceof HTMLElement &&
    Boolean(target.closest("[inert]"))
  if (typeof window === "undefined") {
    if (!isBlocked()) {
      try { target.focus() } catch { /* opener pode ter sido desabilitado */ }
    }
    return
  }
  window.requestAnimationFrame(() => {
    if (target.isConnected && !isBlocked()) {
      try { target.focus() } catch { /* opener pode ter sido desabilitado */ }
    }
  })
}

export function useWindowManager(): WindowManager {
  const manager = useContext(WindowManagerContext)
  if (!manager) {
    throw new Error("useWindowManager() precisa ser usado dentro de <WindowManagerProvider>.")
  }
  return manager
}

/** API pública semântica. O alias antigo permanece para uma migração sem quebra. */
export function useUi(): Ui {
  const manager = useWindowManager()
  return manager
}

export function useWindowRuntime(): WindowRuntimeValue {
  const runtime = useContext(WindowRuntimeContext)
  if (!runtime) {
    throw new Error("A infraestrutura Imperative UI precisa estar dentro de <WindowManagerProvider>.")
  }
  return runtime
}

export function WindowManagerProvider({ children }: { children: ReactNode }) {
  const [controller] = useState<WindowController>(() => {
    let controller: WindowController
    // A porta de confirmação é chamada somente após a instância estar criada.
    // eslint-disable-next-line prefer-const
    controller = new WindowController({
      restoreFocus: restoreFocusTarget,
      confirmDiscard: async () => {
        const focusTarget =
          typeof document !== "undefined" && document.activeElement instanceof HTMLElement
            ? document.activeElement
            : null
        const result = await controller.open<true, ConfirmOptions>(
          ConfirmWindow,
          {},
          {
            title: "Sair do formulário?",
            description: "Existem alterações não salvas. Deseja descartá-las?",
            surface: "confirmation",
            size: "small",
          },
          focusTarget,
        )
        return result.status === "confirmed"
      },
    })
    return controller
  })
  const snapshot = useSyncExternalStore(
    controller.subscribe,
    controller.snapshot,
    controller.snapshot,
  )

  const captureFocus = useCallback((): FocusTarget | null => {
    if (typeof document === "undefined") return null
    return document.activeElement instanceof HTMLElement ? document.activeElement : null
  }, [])

  const open = useCallback(
    <TResult, TProps>(
      view: React.ComponentType<TProps> | OpenWindowOptions<TProps, TResult>,
      props?: TProps,
      options: WindowOptions = {},
    ) => controller.open<TResult, TProps>(view, props, options, captureFocus()),
    [captureFocus, controller],
  )

  const confirm = useCallback(
    async (options: ConfirmOptions) => {
      const result = await open<true, ConfirmOptions>(
        ConfirmWindow,
        options,
        {
          title: options.title ?? "Confirmar ação",
          description: options.description,
          surface: "confirmation",
          size: "small",
        },
      )
      return result.status === "confirmed"
    },
    [open],
  )
  const confirmLegacy = useCallback(
    async (options: ConfirmOptions): Promise<WindowResult<true>> => {
      const confirmed = await confirm(options)
      return confirmed
        ? { status: "confirmed", value: true }
        : { status: "cancelled", reason: "cancel" }
    },
    [confirm],
  )

  const dismissAll = useCallback(() => controller.dismissAll(), [controller])
  const navigate = useNextNavigation(dismissAll)

  const requestClose = useCallback(
    (
      id: WindowId,
      reason: Extract<WindowCloseReason, "cancel" | "escape" | "outside">,
    ) => controller.requestDismiss(id, reason),
    [controller],
  )

  const resolve = useCallback(
    (id: WindowId, result: WindowResult<unknown>) => {
      if (result.status === "confirmed") controller.resolve(id, result.value)
      else controller.dismiss(id, result.reason)
    },
    [controller],
  )

  const setDirty = useCallback(
    (id: WindowId, dirty: boolean) => controller.markDirty(id, dirty),
    [controller],
  )

  useEffect(() => () => controller.dispose(), [controller])

  const manager = useMemo<WindowManager>(
    () => ({
      windows: { open, confirm },
      feedback: { notify: notifyWithSonner },
      navigation: { go: navigate },
      // Compatibilidade temporária, removida quando todos os consumidores usarem ui.windows.
      open,
      confirm: confirmLegacy,
      notify: notifyWithSonner,
      navigate,
    }),
    [confirm, confirmLegacy, navigate, open],
  )
  const runtime = useMemo<WindowRuntimeValue>(
    () => ({
      controller,
      layers: snapshot.windows,
      activeWindowId: snapshot.activeId,
      requestClose,
      resolve,
      setDirty,
    }),
    [controller, requestClose, resolve, setDirty, snapshot],
  )

  return (
    <WindowManagerContext.Provider value={manager}>
      <WindowRuntimeContext.Provider value={runtime}>{children}</WindowRuntimeContext.Provider>
    </WindowManagerContext.Provider>
  )
}

function ConfirmWindow({
  confirmLabel = "Confirmar",
  cancelLabel = "Cancelar",
  confirmVariant = "default",
}: ConfirmOptions) {
  const window = useWindow<true>()

  return (
    <AlertDialogFooter
      data-default-confirmation="true"
      onKeyDown={(event) => {
        if (!event.altKey || event.key !== "Enter") return
        event.preventDefault()
        event.stopPropagation()
        window.resolve(true)
      }}
    >
      <AlertDialogCancel asChild>
        <Button type="button" variant="outline" onClick={() => window.dismiss("cancel")}>
          {cancelLabel} <Kbd>Esc</Kbd>
        </Button>
      </AlertDialogCancel>
      <AlertDialogAction asChild>
        <Button type="button" variant={confirmVariant} onClick={() => window.resolve(true)}>
          {confirmLabel}
          <KbdGroup className="ml-2">
            <Kbd>Alt</Kbd>
            <Kbd>Enter</Kbd>
          </KbdGroup>
        </Button>
      </AlertDialogAction>
    </AlertDialogFooter>
  )
}

export { WindowManagerContext, WindowRuntimeContext }
