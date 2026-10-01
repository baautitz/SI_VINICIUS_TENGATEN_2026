"use client"

import { createContext, useContext } from "react"
import type { RefObject, ReactNode } from "react"
import type { WindowCloseReason } from "./types"

export interface ActiveWindowContextValue<TResult> {
  id: string
  isActive: boolean
  scopeRef: RefObject<HTMLDivElement | null>
  dismiss: (reason?: WindowCloseReason) => void
  resolve: (value: TResult) => void
  /** Registra a consulta de "sujo", lida só ao tentar fechar a janela. */
  setDirtyCheck: (check: (() => boolean) | null) => void
  /** @deprecated Use setDirtyCheck. */
  setDirty: (dirty: boolean) => void
}

export const ActiveWindowContext = createContext<ActiveWindowContextValue<unknown> | null>(null)

export function ActiveWindowProvider({
  value,
  children,
}: {
  value: ActiveWindowContextValue<unknown>
  children: ReactNode
}) {
  return <ActiveWindowContext.Provider value={value}>{children}</ActiveWindowContext.Provider>
}

export function useActiveWindow<TResult>(): ActiveWindowContextValue<TResult> {
  const value = useOptionalActiveWindow<TResult>()

  if (!value) {
    throw new Error(
      "useActiveWindow() precisa ser usado dentro de uma janela aberta pela UI imperativa.",
    )
  }

  return value as ActiveWindowContextValue<TResult>
}

/** Nome curto da API nova; useActiveWindow permanece como alias compatível. */
export function useWindow<TResult>(): ActiveWindowContextValue<TResult> {
  return useActiveWindow<TResult>()
}

export function useOptionalActiveWindow<TResult>(): ActiveWindowContextValue<TResult> | null {
  return useContext(ActiveWindowContext) as ActiveWindowContextValue<TResult> | null
}
