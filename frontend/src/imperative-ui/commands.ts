"use client"

import { normalizeRegisterableHotkey, useHotkeys } from "@/ui/adapters/hotkeys-tanstack"
import { useEffect, useId, useMemo, useRef } from "react"
import type { RefObject } from "react"
import { useOptionalActiveWindow } from "./active-context"
import type { WindowCommand } from "./types"
import { useWindowRuntime } from "./provider"
import { fireAndForget } from "@/lib/utils"

type CommandRegistration = { token: string; scope: string; key: string; id: string }

export interface WindowCommandOptions {
  /** Identificador para um contexto aninhado dentro da janela ativa. */
  scope?: string
  /** Elemento que recebe os atalhos desse contexto. */
  target?: RefObject<HTMLElement | null>
}

const ALT_ENTER_KEY = hotkeyKey("Alt+Enter")
const registrations = new Map<string, CommandRegistration>()
const idRegistrations = new Map<string, CommandRegistration>()

function hotkeyKey(hotkey: WindowCommand["hotkey"]): string {
  try {
    return normalizeRegisterableHotkey(hotkey, "linux")
  } catch {
    return typeof hotkey === "string" ? hotkey.trim().toLowerCase() : JSON.stringify(hotkey)
  }
}

function reportConflict(
  _command: WindowCommand,
  _scope: string,
  _existing: CommandRegistration,
) {
  // O primeiro registro vence; não exponha detalhes internos no console do usuário.
  void _command
  void _scope
  void _existing
}

/** Registra atalhos no escopo da janela ativa; apenas o topo fica habilitado. */
export function useWindowCommands(
  commands: WindowCommand[],
  options: WindowCommandOptions = {},
): void {
  const runtime = useWindowRuntime()
  const activeWindow = useOptionalActiveWindow<unknown>()
  const token = useId()
  const allowedRef = useRef<Set<string>>(new Set())
  // Window-level commands listen on the document. The active window id below
  // is the ownership boundary; tying the listener to the content element
  // would make a shortcut disappear when focus is still on the opener while a
  // modal is being mounted. Callers may still provide a target for a genuinely
  // element-local command (for example, the SKU input's Alt+K).
  const scope = `${activeWindow?.id ?? "root"}:${options.scope ?? "window"}`
  const commandKeys = useMemo(() => commands.map((command) => hotkeyKey(command.hotkey)), [commands])
  const duplicateKeys = useMemo(
    () => new Set(commandKeys.filter((key, index) => commandKeys.indexOf(key) !== index)),
    [commandKeys],
  )
  const duplicateIds = useMemo(() => {
    const ids = commands.map((command) => command.id)
    return new Set(ids.filter((id, index) => ids.indexOf(id) !== index))
  }, [commands])

  useEffect(() => {
    const claimed: string[] = []
    const allowed = new Set<string>()

    commands.forEach((command, index) => {
      const key = commandKeys[index]
      if (!key || duplicateKeys.has(key) || duplicateIds.has(command.id)) {
        if (key && duplicateKeys.has(key) && commandKeys.indexOf(key) !== index) {
          reportConflict(command, scope, { token, scope, key, id: command.id })
        }
        if (duplicateIds.has(command.id) && commands.findIndex((item) => item.id === command.id) !== index) {
          reportConflict(command, scope, { token, scope, key, id: command.id })
        }
        return
      }

      const registrationKey = `${scope}:${key}`
      const existing = registrations.get(registrationKey)
      if (existing && existing.token !== token) {
        reportConflict(command, scope, existing)
        return
      }
      const idKey = `${scope}:id:${command.id}`
      const existingId = idRegistrations.get(idKey)
      if (existingId && existingId.token !== token) {
        reportConflict(command, scope, existingId)
        return
      }

      const registration = { token, scope, key, id: command.id }
      registrations.set(registrationKey, registration)
      idRegistrations.set(idKey, registration)
      claimed.push(registrationKey)
      claimed.push(idKey)
      allowed.add(key)
    })

    allowedRef.current = allowed

    return () => {
      claimed.forEach((registrationKey) => {
        if (registrations.get(registrationKey)?.token === token) {
          registrations.delete(registrationKey)
        }
        if (idRegistrations.get(registrationKey)?.token === token) {
          idRegistrations.delete(registrationKey)
        }
      })
      allowedRef.current = new Set()
    }
  }, [commands, commandKeys, duplicateIds, duplicateKeys, scope, token])

  const definitions = commands.flatMap((command, index) => {
    const key = commandKeys[index]
    const base = {
      hotkey: command.hotkey,
      callback: (event: KeyboardEvent) => {
        if (!allowedRef.current.has(key) || command.enabled === false) return
        fireAndForget(() => command.run(event))
      },
      options: {
        enabled: activeWindow
          ? runtime.activeWindowId === activeWindow.id
          : runtime.activeWindowId === null,
        ignoreInputs: false,
        preventDefault: command.preventDefault ?? true,
        stopPropagation: command.stopPropagation ?? true,
        // O registro do TanStack é global e não conhece o escopo da janela.
        // A arbitragem por contexto acontece no registro acima; permitir os
        // handlers aqui evita alertas falsos entre janelas diferentes.
        conflictBehavior: "allow" as const,
        meta: { name: command.id, description: command.label },
      },
    }
    // Alt+Enter também responde a Control+Enter. São combinações distintas
    // (um único keydown nunca casa as duas), então não há disparo duplicado.
    return key === ALT_ENTER_KEY
      ? [base, { ...base, hotkey: "Control+Enter" as const }]
      : [base]
  })

  useHotkeys(definitions, {
    target: options.target,
    conflictBehavior: "allow",
  })
}
