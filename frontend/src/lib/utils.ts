import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export const isBrowser = typeof document !== 'undefined';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
