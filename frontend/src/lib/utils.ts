import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export const isBrowser = typeof document !== 'undefined';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/**
 * Runs an event callback without leaving rejected async handlers attached to
 * browser events. API helpers already surface the error to the user; this
 * boundary only prevents a duplicate unhandledRejection event.
 */
export function fireAndForget(action: () => unknown): void {
  void Promise.resolve().then(action).catch(() => undefined);
}
