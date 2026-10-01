import type { ComponentType } from "react"
import type {
  CloseReason,
  WindowCloseReason,
  WindowOptions,
} from "./primitives"
import type { ConfirmOptions } from "./services"

/** O identificador é opaco fora do controller. */
export type WindowId = string & { readonly __windowId: unique symbol }

export type { CloseReason, WindowCloseReason, WindowOptions }

export type WindowResult<TResult> =
  | { status: "confirmed"; value: TResult }
  | { status: "cancelled"; reason: CloseReason }

export interface OpenWindowOptions<TProps, TResult = unknown> extends WindowOptions {
  component: ComponentType<TProps>
  props: TProps
  readonly __result?: TResult
}

/** Interface mínima usada pelo núcleo para devolver o foco sem conhecer o DOM. */
export interface FocusTarget {
  readonly isConnected: boolean
  focus(): void
}

export interface DiscardRequest {
  windowId: WindowId
  reason: Extract<CloseReason, "cancel" | "escape" | "outside">
}

/** Portas de efeitos usadas pelo controller. Nenhuma depende de React ou shadcn. */
export interface WindowPorts {
  confirmDiscard: (request: DiscardRequest) => Promise<boolean>
  restoreFocus: (target: FocusTarget | null) => void
}

export interface WindowSnapshotEntry {
  id: WindowId
  options: OpenWindowOptions<unknown>
  focusTarget: FocusTarget | null
}

export interface WindowSnapshot {
  windows: readonly WindowSnapshotEntry[]
  activeId: WindowId | null
}

export interface WindowController {
  open<TResult, TProps>(
    view: ComponentType<TProps>,
    props: TProps,
    options?: WindowOptions,
  ): Promise<WindowResult<TResult>>
  resolve<TResult>(id: WindowId, value: TResult): void
  dismiss(id: WindowId, reason?: CloseReason): void
  requestDismiss(
    id: WindowId,
    reason?: Extract<CloseReason, "cancel" | "escape" | "outside">,
  ): void
  /** Registra a consulta de "sujo", avaliada só ao fechar. Não republica o snapshot. */
  setDirtyCheck(id: WindowId, check: (() => boolean) | null): void
  /** @deprecated Use setDirtyCheck; mantido durante a migração dos formulários. */
  markDirty(id: WindowId, dirty: boolean): void
  snapshot(): WindowSnapshot
  subscribe(listener: () => void): () => void
  dispose(): void
}

export interface WindowService {
  open<TResult, TProps>(
    view: ComponentType<TProps> | OpenWindowOptions<TProps, TResult>,
    props?: TProps,
    options?: WindowOptions,
  ): Promise<WindowResult<TResult>>
  confirm(options: ConfirmOptions): Promise<boolean>
}
