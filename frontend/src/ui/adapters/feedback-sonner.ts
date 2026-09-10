"use client"

import { toast } from "sonner"
import type { NotificationOptions } from "@/ui/imperative/contracts"

export function notifyWithSonner(options: NotificationOptions): void {
  const method = options.type ?? "info"
  toast[method](options.title, {
    description: options.description,
    duration: options.duration,
    id: options.id,
  })
}
