"use client"

import { isValidElement, type ReactNode } from "react"
import { toast } from "sonner"
import {
  extractApiErrors,
  getApiErrorMessage,
} from "@/utils/api-error"
import type {
  FeedbackErrorOptions,
  NotificationOptions,
} from "@/ui/imperative/contracts"

const notifiedErrors = new WeakSet<object>()

function safeContent(value: unknown, fallback: string): ReactNode {
  if (typeof value === "string" || typeof value === "number") return value
  if (isValidElement(value)) return value
  if (Array.isArray(value)) {
    return value.map((item) => safeContent(item, fallback))
  }
  return getApiErrorMessage(value, fallback)
}

export function notifyWithSonner(options: NotificationOptions): void {
  const method = options.type ?? "info"
  toast[method](safeContent(options.title, "Notificação"), {
    description: options.description
      ? safeContent(options.description, "")
      : undefined,
    duration: options.duration,
    id: options.id,
  })
}

export function notifyErrorWithSonner(
  error: unknown,
  options: FeedbackErrorOptions = {},
): void {
  if (
    typeof error === "object" &&
    error !== null &&
    "suppressToast" in error &&
    error.suppressToast === true
  ) {
    return
  }

  if (
    typeof error === "object" &&
    error !== null &&
    notifiedErrors.has(error)
  ) {
    return
  }

  const parsed = extractApiErrors(error)
  const fieldOnly = !parsed.globalError && Object.keys(parsed.fieldErrors).length > 0
  const title = parsed.globalError ??
    (fieldOnly
      ? "Verifique os campos destacados."
      : getApiErrorMessage(error, options.fallbackTitle ?? "Ocorreu um erro inesperado."))

  notifyWithSonner({
    type: "error",
    title: safeContent(title, options.fallbackTitle ?? "Ocorreu um erro inesperado."),
    id: options.id,
  })

  if (typeof error === "object" && error !== null) notifiedErrors.add(error)
}
