/** Fachada pública da UI imperativa. Implementação visual permanece adaptável. */
export * from "@/imperative-ui"
export { WindowController } from "./controller"
export type {
  CloseReason,
  ConfirmOptions,
  DiscardRequest,
  FocusTarget,
  NavigationOptions,
  NotificationOptions,
  NotificationType,
  OpenWindowOptions,
  Ui,
  WindowCloseReason,
  WindowId,
  WindowManager,
  WindowIcon,
  WindowOptions,
  WindowPorts,
  WindowResult,
  WindowSnapshot,
  WindowSnapshotEntry,
} from "./contracts"
