import type { ComponentType, ReactNode } from "react"
import type { WindowOptions } from "./primitives"
import type { OpenWindowOptions, WindowResult, WindowService } from "./windows"

export interface ConfirmOptions {
  title?: ReactNode
  description?: ReactNode
  confirmLabel?: ReactNode
  cancelLabel?: ReactNode
  confirmVariant?: "default" | "destructive" | "outline" | "secondary" | "ghost"
}

export type NotificationType = "success" | "error" | "info" | "warning" | "loading"

export interface NotificationOptions {
  title: ReactNode
  description?: ReactNode
  type?: NotificationType
  duration?: number
  id?: string | number
}

export interface NavigationOptions {
  replace?: boolean
  scroll?: boolean
}

export interface FeedbackService {
  notify(options: NotificationOptions): void
}

export interface NavigationService {
  go(href: string, options?: NavigationOptions): void
}

/** Fachada estável consumida pelo produto. */
export interface Ui {
  windows: WindowService
  feedback: FeedbackService
  navigation: NavigationService
}

/** Superfície legada temporária durante a migração dos consumidores. */
export interface WindowManager extends Ui {
  open<TResult, TProps>(
    view: ComponentType<TProps> | OpenWindowOptions<TProps, TResult>,
    props?: TProps,
    options?: WindowOptions,
  ): Promise<WindowResult<TResult>>
  confirm(options: ConfirmOptions): Promise<WindowResult<true>>
  notify(options: NotificationOptions): void
  navigate(href: string, options?: NavigationOptions): void
}
