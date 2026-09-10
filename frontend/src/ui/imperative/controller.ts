import type { ComponentType } from "react"
import type {
  CloseReason,
  DiscardRequest,
  FocusTarget,
  OpenWindowOptions,
  WindowController as WindowControllerContract,
  WindowId,
  WindowOptions,
  WindowPorts,
  WindowResult,
  WindowSnapshot,
  WindowSnapshotEntry,
} from "./contracts"

interface PendingWindow extends WindowSnapshotEntry {
  resolve: (result: WindowResult<unknown>) => void
}

const defaultPorts: WindowPorts = {
  confirmDiscard: async () => false,
  restoreFocus: () => undefined,
}

/** Máquina de estados sem React, DOM ou dependência de biblioteca visual. */
export class WindowController implements WindowControllerContract {
  private readonly listeners = new Set<() => void>()
  private readonly pendingClose = new Set<WindowId>()
  private readonly ports: WindowPorts
  private windows: PendingWindow[] = []
  private sequence = 0
  private disposed = false
  private currentSnapshot: WindowSnapshot = { windows: [], activeId: null }

  constructor(ports: Partial<WindowPorts> = {}) {
    this.ports = { ...defaultPorts, ...ports }
  }

  snapshot = (): WindowSnapshot => this.currentSnapshot
  getSnapshot = this.snapshot

  subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener)
    return () => this.listeners.delete(listener)
  }

  open<TResult, TProps>(
    view: ComponentType<TProps> | OpenWindowOptions<TProps, TResult>,
    props?: TProps,
    options: WindowOptions = {},
    focusTarget: FocusTarget | null = null,
  ): Promise<WindowResult<TResult>> {
    if (this.disposed) {
      return Promise.resolve({ status: "cancelled", reason: "unmounted" })
    }

    const normalized = typeof view === "function"
      ? { component: view, props: props as TProps, ...options }
      : view

    return new Promise<WindowResult<TResult>>((resolve) => {
      const id = `window-${++this.sequence}` as WindowId
      this.windows.push({
        id,
        options: normalized as OpenWindowOptions<unknown>,
        focusTarget,
        dirty: false,
        resolve: resolve as (result: WindowResult<unknown>) => void,
      })
      this.publish()
    })
  }

  resolve<TResult>(id: WindowId, value: TResult): void {
    if (!this.isTop(id)) return
    this.settle(id, { status: "confirmed", value })
  }

  dismiss(id: WindowId, reason: CloseReason = "cancel"): void {
    if (reason === "unmounted") {
      if (this.isTop(id)) this.settle(id, { status: "cancelled", reason })
      return
    }
    const requestReason = reason === "discarded" ? "cancel" : reason
    this.requestDismiss(id, requestReason)
  }

  requestDismiss(
    id: WindowId,
    reason: Extract<CloseReason, "cancel" | "escape" | "outside"> = "cancel",
  ): void {
    const current = this.top()
    if (!current || current.id !== id || this.pendingClose.has(id)) return

    if (!current.dirty) {
      this.settle(id, { status: "cancelled", reason })
      return
    }

    // Repetições enquanto a confirmação está aberta são deliberadamente ignoradas.
    this.pendingClose.add(id)
    const request: DiscardRequest = { windowId: id, reason }
    let confirmation: Promise<boolean>
    try {
      confirmation = this.ports.confirmDiscard(request)
    } catch {
      this.pendingClose.delete(id)
      return
    }
    void confirmation.then(
      (discard) => {
        this.pendingClose.delete(id)
        if (discard && this.isTop(id)) {
          this.settle(id, { status: "cancelled", reason: "discarded" })
        }
      },
      () => {
        this.pendingClose.delete(id)
      },
    )
  }

  markDirty(id: WindowId, dirty: boolean): void {
    const current = this.windows.find((window) => window.id === id)
    if (!current || current.dirty === dirty) return
    current.dirty = dirty
    this.publish()
  }

  dismissAll(): void {
    if (!this.windows.length) return
    const current = this.windows
    this.windows = []
    this.pendingClose.clear()
    this.publish()
    current.forEach((window) => window.resolve({ status: "cancelled", reason: "unmounted" }))
  }

  dispose(): void {
    if (this.disposed) return
    this.disposed = true
    this.dismissAll()
    this.listeners.clear()
  }

  private top(): PendingWindow | undefined {
    return this.windows[this.windows.length - 1]
  }

  private isTop(id: WindowId): boolean {
    return this.top()?.id === id
  }

  private settle(id: WindowId, result: WindowResult<unknown>): void {
    const index = this.windows.findIndex((window) => window.id === id)
    if (index === -1 || !this.isTop(id)) return

    const [window] = this.windows.splice(index, 1)
    this.pendingClose.delete(id)
    this.publish()
    window.resolve(result)

    if (!(result.status === "cancelled" && result.reason === "unmounted")) {
      try {
        this.ports.restoreFocus(window.focusTarget)
      } catch {
        // Efeitos de foco não podem reabrir uma Promise já liquidada.
      }
    }
  }

  private publish(): void {
    const windows: WindowSnapshotEntry[] = this.windows.map((window) => ({
      id: window.id,
      options: window.options,
      focusTarget: window.focusTarget,
      dirty: window.dirty,
    }))
    this.currentSnapshot = { windows, activeId: windows.at(-1)?.id ?? null }
    this.listeners.forEach((listener) => listener())
  }
}
