import type { RegisterableHotkey } from "@tanstack/hotkeys"
import type { OpenWindowOptions as ContractOpenWindowOptions } from "@/ui/imperative/contracts"
export type {
  CloseReason,
  ConfirmOptions,
  DiscardRequest,
  FocusTarget,
  NavigationOptions,
  NotificationOptions,
  NotificationType,
  WindowCloseReason,
  WindowId,
  WindowManager,
  WindowIcon,
  WindowOptions,
  WindowPresentation,
  WindowSurface,
  WindowSize,
  WindowPorts,
  WindowResult,
  WindowSnapshot,
  WindowSnapshotEntry,
  Ui,
  WindowService,
  FeedbackService,
  NavigationService,
} from "@/ui/imperative/contracts"

/** Alias mantido para compatibilidade com a API pública existente. */
export type OpenWindowOptions<TProps, TResult = unknown> = ContractOpenWindowOptions<TProps, TResult>

export interface WindowCommand {
  id: string
  hotkey: RegisterableHotkey
  label?: string
  enabled?: boolean
  /** Permite que um comando observe a tecla sem bloquear a ação nativa do elemento focado. */
  preventDefault?: boolean
  stopPropagation?: boolean
  run: (event: KeyboardEvent) => void | Promise<void>
}
