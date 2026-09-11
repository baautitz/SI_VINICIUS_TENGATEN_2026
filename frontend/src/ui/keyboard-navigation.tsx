"use client"

import * as React from "react"
import {
  chooseSpatialDestination,
  type NavigationCell,
  type NavigationDirection,
  type SpatialNavigationNode,
} from "./keyboard-navigation-core"

export type { NavigationDirection } from "./keyboard-navigation-core"

type InputFocusModality = "pointer" | "keyboard"

// Focused text fields behave like spreadsheet cells: keyboard navigation
// selects the value for replacement, while a mouse click keeps the native
// caret. This is shared by the base Input and the document-level navigator.
let inputFocusModality: InputFocusModality = "keyboard"

/** Marks a programmatic focus (for example, the first field of a dialog) as keyboard focus. */
export function markKeyboardFocus(): void {
  inputFocusModality = "keyboard"
}

export interface NavigationCellOptions {
  grid: string
  row: number
  column: number
}

/** Metadata used by the spatial navigator for a table/grid cell. */
export function navigationCell({
  grid,
  row,
  column,
}: NavigationCellOptions): Record<string, string> {
  return {
    "data-navigation-grid": grid,
    "data-navigation-row": String(row),
    "data-navigation-column": String(column),
  }
}

/** Selects an input's complete value after focus has settled. */
export function selectInputTextOnFocus(input: HTMLInputElement): void {
  if (input.disabled || input.readOnly) return

  requestAnimationFrame(() => {
    if (
      document.activeElement === input &&
      (inputFocusModality === "keyboard" || input.autofocus) &&
      input.dataset.navigationEditing !== "true"
    ) {
      input.select()
    }
  })
}

interface NavigationScopeRegistration {
  id: string
  root: HTMLElement
  active: boolean
  priority: number
}

interface FocusedNavigationSnapshot {
  scopeId: string
  element: HTMLElement
  rect: DOMRect
  cell: NavigationCell | null
}

interface NavigationContextValue {
  registerScope: (scope: NavigationScopeRegistration) => () => void
}

const NavigationContext = React.createContext<NavigationContextValue | null>(null)

interface NavigationScopeProps {
  id: string
  active?: boolean
  priority?: number
  children: React.ReactNode
  className?: string
}

/** A zero-layout scope for the application shell or another independent surface. */
export function NavigationScope({
  id,
  active = true,
  priority = 0,
  children,
  className,
}: NavigationScopeProps) {
  const ref = React.useRef<HTMLDivElement>(null)
  useNavigationScope(ref, { id, active, priority })

  return (
    <div ref={ref} data-navigation-scope={id} className={className ?? "contents"}>
      {children}
    </div>
  )
}

/** Registers an existing element (for example DialogContent) as a scope. */
export function useNavigationScope(
  ref: React.RefObject<HTMLElement | null>,
  options: Omit<NavigationScopeRegistration, "root">,
): void {
  const context = React.useContext(NavigationContext)
  const { id, active, priority } = options

  React.useLayoutEffect(() => {
    if (!context) return

    let disposed = false
    let attempts = 0
    let frame: number | null = null
    let unregister: (() => void) | null = null

    // Dialogs and sheets render through a Radix portal. Their ref can be null
    // during the first layout pass even though the component is already
    // mounted. Keep the registration alive until the real DOM root exists so
    // keyboard navigation cannot silently fall back to the background shell.
    const registerWhenReady = () => {
      if (disposed) return
      const root = ref.current
      if (root) {
        unregister?.()
        unregister = context.registerScope({ id, active, priority, root })
        observer.disconnect()
        if (frame !== null) cancelAnimationFrame(frame)
        frame = null
        return
      }

      attempts += 1
      if (attempts < 120) {
        frame = requestAnimationFrame(registerWhenReady)
      }
    }

    const observer = new MutationObserver(registerWhenReady)
    if (document.body) {
      observer.observe(document.body, { childList: true, subtree: true })
    }
    registerWhenReady()

    return () => {
      disposed = true
      observer.disconnect()
      if (frame !== null) cancelAnimationFrame(frame)
      unregister?.()
    }
  }, [context, id, active, priority, ref])
}

export function KeyboardNavigationProvider({ children }: { children: React.ReactNode }) {
  const scopesRef = React.useRef<NavigationScopeRegistration[]>([])
  const lastFocusedRef = React.useRef<FocusedNavigationSnapshot | null>(null)

  const registerScope = React.useCallback((scope: NavigationScopeRegistration) => {
    scopesRef.current = scopesRef.current.filter((item) => item.id !== scope.id)
    scopesRef.current.push(scope)

    const observer = new MutationObserver(() => {
      const snapshot = lastFocusedRef.current
      if (
        !snapshot ||
        snapshot.scopeId !== scope.id ||
        snapshot.element.isConnected ||
        !scope.active
      ) {
        return
      }

      requestAnimationFrame(() => {
        const current = lastFocusedRef.current
        if (!current || current !== snapshot || current.element.isConnected) return

        const candidates = collectCandidates(scope.root)
        const source: SpatialNavigationNode = {
          id: "removed-source",
          rect: snapshot.rect,
          cell: snapshot.cell,
        }
        const geometryCandidates = candidates.map((candidate, index) => ({
          id: String(index),
          rect: candidate.rect,
          index,
          cell: readCell(candidate.element),
        }))
        const destinationId =
          chooseSpatialDestination(source, geometryCandidates, "down") ??
          chooseSpatialDestination(source, geometryCandidates, "up")
        const destination =
          destinationId === null
            ? null
            : candidates[Number(destinationId)]?.element ?? null
        if (!destination) return

        destination.scrollIntoView({ block: "nearest", inline: "nearest" })
        destination.focus({ preventScroll: true })
      })
    })
    observer.observe(scope.root, { childList: true, subtree: true })

    return () => {
      observer.disconnect()
      scopesRef.current = scopesRef.current.filter((item) => item !== scope)
    }
  }, [])

  React.useEffect(() => {
    const handlePointerDown = (event: PointerEvent) => {
      inputFocusModality = "pointer"
      const target = event.target
      if (!(target instanceof HTMLElement)) return
      const input = target.closest<HTMLInputElement | HTMLTextAreaElement>(
        "input:not([type='hidden']), textarea",
      )
      if (input && !input.hasAttribute("readonly") && !input.hasAttribute("disabled")) {
        input.dataset.navigationEditing = "true"
      }
    }

    const handleFocusIn = (event: FocusEvent) => {
      const target = event.target
      if (!(target instanceof HTMLElement)) return

      // Keyboard focus starts in insertion mode. A pointer focus deliberately
      // keeps the caret at the clicked position.
      if (
        inputFocusModality === "keyboard" &&
        target.matches("input:not([type='hidden']), textarea")
      ) {
        target.removeAttribute("data-navigation-editing")
      }

      const scope = findScopeForTarget(target, scopesRef.current)
      if (!scope || !isEligible(target)) return
      lastFocusedRef.current = {
        scopeId: scope.id,
        element: target,
        rect: target.getBoundingClientRect(),
        cell: readCell(target),
      }
    }

    document.addEventListener("pointerdown", handlePointerDown, true)
    document.addEventListener("focusin", handleFocusIn, true)
    return () => {
      document.removeEventListener("pointerdown", handlePointerDown, true)
      document.removeEventListener("focusin", handleFocusIn, true)
    }
  }, [])

  React.useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (!event.isComposing) {
        inputFocusModality = "keyboard"
      }
      if (event.defaultPrevented || event.isComposing) return
      if (event.altKey || event.ctrlKey || event.metaKey) return

      const target = event.target
      if (!(target instanceof HTMLElement)) return

      if (event.key === "F2" && isEditableTextEntry(target)) {
        event.preventDefault()
        event.stopPropagation()
        target.dataset.navigationEditing = "true"
        const position = target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement
          ? target.value.length
          : 0
        if (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement) {
          target.setSelectionRange(position, position)
        }
        return
      }

      const direction = getNavigationDirection(event)
      if (!direction) return
      if (
        isPopupInteraction(target) &&
        direction !== "forward" &&
        direction !== "backward"
      ) {
        return
      }

      // A clicked/F2 field is in cursor mode. Keep horizontal arrows native
      // so the caret can move; vertical arrows and Tab still navigate fields.
      if (
        isEditableTextEntry(target) &&
        target.dataset.navigationEditing === "true" &&
        (direction === "left" || direction === "right")
      ) {
        return
      }

      const scope = findScopeForTarget(target, scopesRef.current)
      if (!scope) return

      const source = findNavigationElement(target)
      if (!source || !isEligible(source)) return

      const destination = findDestination(source, direction, scope.root)
      if (!destination || destination === source) {
        if (direction === "forward" || direction === "backward") {
          event.preventDefault()
        }
        return
      }

      event.preventDefault()
      event.stopPropagation()
      destination.scrollIntoView({ block: "nearest", inline: "nearest" })
      destination.focus({ preventScroll: true })
    }

    document.addEventListener("keydown", handleKeyDown, true)
    return () => document.removeEventListener("keydown", handleKeyDown, true)
  }, [])

  return (
    <NavigationContext.Provider value={{ registerScope }}>
      {children}
    </NavigationContext.Provider>
  )
}

const NAVIGABLE_SELECTOR = [
  "input:not([type='hidden'])",
  "textarea",
  "select",
  "button",
  "a[href]",
  "[role='button']",
  "[role='checkbox']",
  "[role='radio']",
  "[role='switch']",
  "[role='tab']",
  "[tabindex]",
].join(",")

function getNavigationDirection(event: KeyboardEvent): NavigationDirection | null {
  if (event.key === "ArrowUp") return "up"
  if (event.key === "ArrowDown") return "down"
  if (event.key === "ArrowLeft") return "left"
  if (event.key === "ArrowRight") return "right"
  if (event.key === "Tab") return event.shiftKey ? "backward" : "forward"
  return null
}

function isEditableTextEntry(element: HTMLElement): element is HTMLInputElement | HTMLTextAreaElement {
  if (element instanceof HTMLTextAreaElement) {
    return !element.disabled && !element.readOnly
  }
  if (!(element instanceof HTMLInputElement)) return false
  return (
    !element.disabled &&
    !element.readOnly &&
    !["hidden", "button", "checkbox", "file", "image", "radio", "range", "reset", "submit"].includes(
      element.type,
    )
  )
}

function findScopeForTarget(
  target: HTMLElement,
  scopes: NavigationScopeRegistration[],
): NavigationScopeRegistration | null {
  const activeScopes = scopes
    .filter((scope) => scope.active && scope.root.isConnected)
    .sort((a, b) => b.priority - a.priority)
  const topScope = activeScopes[0]

  // A modal scope owns the entire keyboard surface while it is open, even if
  // a stale focus event originates from the inert page behind it.
  if (topScope?.priority > 0) {
    const containingScope = activeScopes.find((scope) => scope.root.contains(target))
    if (!containingScope || target.closest("[aria-hidden='true'], [inert]")) {
      return null
    }
    return containingScope
  }

  return activeScopes.find((scope) => scope.root.contains(target)) ?? null
}

function findNavigationElement(target: HTMLElement): HTMLElement | null {
  if (isEligible(target)) return target
  return target.closest<HTMLElement>(NAVIGABLE_SELECTOR)
}

function isEligible(element: HTMLElement): boolean {
  if (!element.isConnected || element.tabIndex < 0) return false
  if (element.closest("[aria-hidden='true'], [inert]")) return false
  if (element.getAttribute("aria-disabled") === "true") return false
  if (element instanceof HTMLButtonElement ||
      element instanceof HTMLInputElement ||
      element instanceof HTMLSelectElement ||
      element instanceof HTMLTextAreaElement) {
    if (element.disabled) return false
  }

  const style = window.getComputedStyle(element)
  if (style.display === "none" || style.visibility === "hidden") return false
  const rect = element.getBoundingClientRect()
  return rect.width > 0 && rect.height > 0
}

function isPopupInteraction(target: HTMLElement): boolean {
  if (target.closest("[data-navigation-popup-open='true']")) return true

  // A DataTable has its own row navigation. It must not inherit popup
  // semantics from a surrounding trigger while rendered in a dialog portal.
  if (target.closest("[data-navigation-list='true']")) return false

  // Accordion triggers expose aria-expanded as part of their disclosure
  // state, but their children are navigated with ↑/↓ rather than popup keys.
  if (target.closest("[data-navigation-sidebar='true']")) return false

  return Boolean(target.closest("[aria-expanded='true']"))
}

interface RectWithElement {
  element: HTMLElement
  rect: DOMRect
  index: number
}

function readCell(element: HTMLElement): NavigationCell | null {
  const host = element.closest<HTMLElement>(
    "[data-navigation-grid][data-navigation-row][data-navigation-column]",
  )
  if (!host) return null

  const row = Number(host.dataset.navigationRow)
  const column = Number(host.dataset.navigationColumn)
  if (!Number.isFinite(row) || !Number.isFinite(column)) return null

  return {
    grid: host.dataset.navigationGrid ?? "",
    row,
    column,
  }
}

function findDestination(
  source: HTMLElement,
  direction: NavigationDirection,
  root: HTMLElement,
): HTMLElement | null {
  const allCandidates = collectCandidates(root)
  const horizontalCandidates = allCandidates.filter(
    (candidate) =>
      !candidate.element.closest("[data-navigation-sidebar-subitem='true']"),
  )
  const sidebar = source.closest<HTMLElement>("[data-navigation-sidebar='true']")
  const candidates = sidebar
    ? direction === "up" || direction === "down"
      ? allCandidates.filter((candidate) => sidebar.contains(candidate.element))
      : horizontalCandidates.filter((candidate) => !sidebar.contains(candidate.element))
    : direction === "forward" || direction === "backward"
      ? horizontalCandidates
      : allCandidates
  const listSearch = source.matches("[data-navigation-list-search='true']")
  if (listSearch && direction === "down") {
    return candidates.find((candidate) =>
      candidate.element.matches("[data-navigation-list-item='true']"),
    )?.element ?? null
  }

  const listItem = source.closest<HTMLElement>("[data-navigation-list-item='true']")
  if (listItem && (direction === "up" || direction === "down")) {
    const items = candidates
      .filter((candidate) =>
        candidate.element.matches("[data-navigation-list-item='true']"),
      )
      .map((candidate) => candidate.element)
    const index = items.indexOf(listItem)
    if (direction === "down") {
      return items[index + 1] ?? null
    }

    if (index <= 0) {
      return root.querySelector<HTMLElement>("[data-navigation-list-search='true']")
    }

    return items[index - 1] ?? null
  }

  const sourceCandidate: SpatialNavigationNode = {
    id: "source",
    rect: source.getBoundingClientRect(),
    cell: readCell(source),
  }
  const geometryCandidates: SpatialNavigationNode[] = candidates.map(
    (candidate, index) => ({
      id: String(index),
      rect: candidate.rect,
      index,
      cell: readCell(candidate.element),
    }),
  )
  const destinationId = chooseSpatialDestination(
    sourceCandidate,
    geometryCandidates,
    direction,
  )
  const destination =
    destinationId === null
      ? null
      : candidates[Number(destinationId)]?.element ?? null

  // Returning from the content should restore the current route in the
  // sidebar, instead of landing on its unrelated collapse button or another
  // group trigger. Internal sidebar navigation keeps its normal geometry.
  if (
    direction === "backward" &&
    !sidebar &&
    destination?.closest("[data-navigation-sidebar='true']")
  ) {
    const currentPage = Array.from(
      root.querySelectorAll<HTMLElement>(
        "[data-navigation-sidebar-current='true']",
      ),
    ).find(isEligible)
    if (currentPage && isEligible(currentPage)) return currentPage
  }

  return destination === null
    ? null
    : destination
}

function collectCandidates(root: HTMLElement): RectWithElement[] {
  return Array.from(root.querySelectorAll<HTMLElement>(NAVIGABLE_SELECTOR))
    .filter((element) => !element.matches("[data-navigation-chip-remove='true']"))
    .filter(isEligible)
    .map((element, index) => ({ element, rect: element.getBoundingClientRect(), index }))
}
