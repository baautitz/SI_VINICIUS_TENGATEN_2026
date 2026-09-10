import type { ComponentType, ReactNode } from "react"

/** Contratos semânticos compartilhados pelas superfícies visuais. */
export type WindowPresentation = "dialog" | "drawer"
export type WindowSurface = "dialog" | "sheet" | "confirmation"
export type WindowSize = "small" | "medium" | "large" | "full"

/** Ícone aceito tanto como elemento quanto como componente da configuração de navegação. */
export type WindowIcon = ReactNode | ComponentType<{ "aria-hidden"?: boolean }>

export type CloseReason =
  | "cancel"
  | "escape"
  | "outside"
  | "discarded"
  | "unmounted"

export type WindowCloseReason = CloseReason

export interface WindowOptions {
  presentation?: WindowPresentation
  title?: ReactNode
  description?: ReactNode
  icon?: WindowIcon
  side?: "top" | "right" | "bottom" | "left"
  /** Alias de migração; novas chamadas devem usar `side`. */
  drawerSide?: "top" | "right" | "bottom" | "left"
  closeOnOutside?: boolean
  surface?: WindowSurface
  size?: WindowSize
  chrome?: "default" | "plain"
}
