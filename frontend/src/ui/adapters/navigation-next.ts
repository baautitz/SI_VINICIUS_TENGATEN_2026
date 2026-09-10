"use client"

/** Adapter isolando o roteador do shell/produto. */
export { usePathname as useNavigationPathname } from "next/navigation"

import { useRouter } from "next/navigation"
import { useCallback } from "react"
import type { NavigationOptions } from "@/ui/imperative/contracts"

export function useNextNavigation(beforeNavigate?: () => void) {
  const router = useRouter()
  return useCallback(
    (href: string, options: NavigationOptions = {}) => {
      beforeNavigate?.()
      if (options.replace) router.replace(href, { scroll: options.scroll })
      else router.push(href, { scroll: options.scroll })
    },
    [beforeNavigate, router],
  )
}
