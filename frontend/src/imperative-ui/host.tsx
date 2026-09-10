"use client"

import { useMemo, useRef } from "react"
import type { ComponentType, ComponentProps, ReactNode } from "react"
import { AppWindow } from "lucide-react"
import { cn } from "@/lib/utils"
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogHeader,
  AlertDialogTitle,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/primitives"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/ui/primitives"
import { ActiveWindowProvider } from "./active-context"
import { useWindowRuntime, type WindowRecord } from "./provider"
import type { WindowCloseReason, WindowIcon } from "./types"

const sizeClasses = {
  small: "max-w-md",
  medium: "max-w-2xl",
  large: "max-w-6xl",
  full: "w-[95vw] max-w-[95vw]",
} as const

/** Renderiza cada janela como uma superfície modal independente. */
export function WindowManagerHost() {
  const runtime = useWindowRuntime()

  return (
    <>
      {runtime.layers.map((record) => (
        <ManagedWindow
          key={record.id}
          record={record}
          isTop={record.id === runtime.activeWindowId}
          runtime={runtime}
        />
      ))}
    </>
  )
}

function ManagedWindow({
  record,
  isTop,
  runtime,
}: {
  record: WindowRecord
  isTop: boolean
  runtime: ReturnType<typeof useWindowRuntime>
}) {
  const scopeRef = useRef<HTMLDivElement | null>(null)
  const Component = record.options.component as ComponentType<Record<string, unknown>>
  const componentProps = record.options.props as Record<string, unknown>
  const title = record.options.title ?? "Janela"
  const description = record.options.description
  const icon = record.options.icon
  const reasonForClose: Extract<WindowCloseReason, "cancel" | "escape" | "outside"> = "cancel"
  const activeContext = useMemo(
    () => ({
      id: record.id,
      get isActive() {
        return runtime.controller.snapshot().activeId === record.id
      },
      scopeRef,
      dismiss: (reason: WindowCloseReason = reasonForClose) => {
        runtime.controller.dismiss(record.id, reason)
      },
      resolve: (value: unknown) => runtime.controller.resolve(record.id, value),
      setDirty: (dirty: boolean) => runtime.controller.markDirty(record.id, dirty),
    }),
    [record.id, runtime.controller],
  )
  const body = (
    <div data-window-scope="true" className="contents">
      <Component {...componentProps} />
    </div>
  )
  const semanticClassName = cn(
    record.options.size ? sizeClasses[record.options.size] : undefined,
    record.options.chrome === "plain" ? "p-0" : undefined,
  )
  const titlebarClassName = cn(
    "shrink-0 border-b px-4 py-3",
    record.options.surface === "sheet" || record.options.presentation === "drawer"
      ? undefined
      : record.options.chrome === "plain"
        ? undefined
        : "-mx-4 -mt-4",
  )
  const requestClose = (reason: Extract<WindowCloseReason, "cancel" | "escape" | "outside">) =>
    runtime.controller.requestDismiss(record.id, reason)

  if (record.options.surface === "confirmation") {
    return (
      <AlertDialog
        open
        onOpenChange={(open) => {
          if (!open) requestClose("cancel")
        }}
      >
        <AlertDialogContent
          ref={scopeRef}
          aria-hidden={!isTop ? true : undefined}
          inert={!isTop ? true : undefined}
          onEscapeKeyDown={(event) => {
            event.preventDefault()
            requestClose("escape")
          }}
        >
          <AlertDialogHeader>
            <AlertDialogTitle>{title}</AlertDialogTitle>
            {description ? (
              <AlertDialogDescription asChild>
                <div>{description}</div>
              </AlertDialogDescription>
            ) : (
              <AlertDialogDescription className="sr-only">
                Confirme ou cancele esta ação.
              </AlertDialogDescription>
            )}
          </AlertDialogHeader>
          <ActiveWindowProvider value={activeContext}>{body}</ActiveWindowProvider>
        </AlertDialogContent>
      </AlertDialog>
    )
  }

  if (record.options.presentation === "drawer" || record.options.surface === "sheet") {
    return (
      <Sheet
        open
        onOpenChange={(open) => {
          if (!open) requestClose("cancel")
        }}
      >
        <SheetContent
          ref={scopeRef}
          side={record.options.side ?? record.options.drawerSide ?? "right"}
          aria-hidden={!isTop ? true : undefined}
          inert={!isTop ? true : undefined}
          className={semanticClassName}
          onOpenAutoFocus={(event) => focusFirstField(event, isTop)}
          onEscapeKeyDown={(event) => {
            event.preventDefault()
            requestClose("escape")
          }}
          onInteractOutside={(event) => {
            if (record.options.closeOnOutside === false) {
              event.preventDefault()
              return
            }
            event.preventDefault()
            requestClose("outside")
          }}
        >
          <SheetHeader className={titlebarClassName} data-window-titlebar="true">
            <WindowTitleContent
              icon={icon}
              title={title}
              description={description}
              Title={SheetTitle}
              Description={SheetDescription}
            />
          </SheetHeader>
          <ActiveWindowProvider value={activeContext}>{body}</ActiveWindowProvider>
        </SheetContent>
      </Sheet>
    )
  }

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) requestClose("cancel")
      }}
    >
      <DialogContent
        ref={scopeRef}
        aria-hidden={!isTop ? true : undefined}
        inert={!isTop ? true : undefined}
        className={semanticClassName}
        onOpenAutoFocus={(event) => focusFirstField(event, isTop)}
        onEscapeKeyDown={(event) => {
          event.preventDefault()
          requestClose("escape")
        }}
        onInteractOutside={(event) => {
          if (record.options.closeOnOutside === false) {
            event.preventDefault()
            return
          }
          event.preventDefault()
          requestClose("outside")
        }}
      >
        <DialogHeader className={titlebarClassName} data-window-titlebar="true">
          <WindowTitleContent
            icon={icon}
            title={title}
            description={description}
            Title={DialogTitle}
            Description={DialogDescription}
          />
        </DialogHeader>
        <ActiveWindowProvider value={activeContext}>{body}</ActiveWindowProvider>
      </DialogContent>
    </Dialog>
  )
}

/**
 * O primeiro campo da janela é o ponto de entrada do teclado. O Radix continua
 * sendo o fallback para janelas sem campos (por exemplo, uma janela só com
 * ações), mas nunca escolhemos Cancelar/Salvar antes de um input real.
 */
function focusFirstField(event: Event, isTop: boolean): void {
  if (!isTop) {
    event.preventDefault()
    return
  }

  const container = event.currentTarget
  if (!(container instanceof HTMLElement)) return

  const field = Array.from(
    container.querySelectorAll<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>(
      "input, textarea, select",
    ),
  ).find((candidate) => {
    if (candidate.disabled || candidate.getAttribute("aria-hidden") === "true") return false
    if (candidate instanceof HTMLInputElement) {
      return !["hidden", "button", "submit", "reset", "image"].includes(candidate.type)
    }
    return true
  })

  if (!field) return
  event.preventDefault()
  field.focus()
}

function WindowTitleContent({
  icon,
  title,
  description,
  Title,
  Description,
}: {
  icon?: WindowIcon
  title: ReactNode
  description?: ReactNode
  Title: ComponentType<ComponentProps<typeof DialogTitle>>
  Description: ComponentType<ComponentProps<typeof DialogDescription>>
}) {
  const Icon = typeof icon === "function" ? icon : null
  const iconNode: ReactNode = Icon ? <Icon /> : (icon as ReactNode)
  return (
    <div className="flex min-w-0 items-center gap-3">
      <div
        aria-hidden="true"
        className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary [&_svg]:size-5"
      >
        {iconNode ?? <AppWindow />}
      </div>
      <div className="min-w-0 space-y-1 pr-8">
        <Title className="truncate text-lg font-semibold">{title}</Title>
        {description ? (
          <Description asChild>
            <div className="text-sm text-muted-foreground">{description}</div>
          </Description>
        ) : (
          <Description className="sr-only">
            Conteúdo da janela.
          </Description>
        )}
      </div>
    </div>
  )
}
