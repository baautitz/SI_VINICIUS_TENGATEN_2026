export const MAX_TOAST_TITLE_LENGTH = 200

type ToastType = "success" | "error" | "info" | "warning" | "loading"

const DURATION_BY_TYPE: Record<ToastType, number | undefined> = {
  success: 4000,
  info: 4000,
  warning: 6000,
  error: 8000,
  loading: undefined,
}

export function toastDuration(type: ToastType, explicit?: number): number | undefined {
  return explicit ?? DURATION_BY_TYPE[type]
}

/** Mantém o título legível: o excedente de mensagens longas vai para a descrição. */
export function splitLongMessage(message: string): { title: string; description?: string } {
  if (message.length <= MAX_TOAST_TITLE_LENGTH) return { title: message }

  const lastSpace = message.lastIndexOf(" ", MAX_TOAST_TITLE_LENGTH)
  const cut = lastSpace > 0 ? lastSpace : MAX_TOAST_TITLE_LENGTH
  return {
    title: `${message.slice(0, cut).trimEnd()}…`,
    description: message.slice(cut).trimStart(),
  }
}
