"use client"

import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query"
import { useState, useEffect } from "react"
import { HotkeysProvider } from "@tanstack/react-hotkeys"
import "@/lib/zod-config"
import { notifyErrorWithSonner } from "@/ui/adapters/feedback-sonner"

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        queryCache: new QueryCache({
          onError: (error) => notifyErrorWithSonner(error),
        }),
        mutationCache: new MutationCache({
          onError: (error) => notifyErrorWithSonner(error),
        }),
        defaultOptions: {
          queries: {
            staleTime: 0,
            refetchOnWindowFocus: true,
            refetchOnMount: true,
          },
        },
      })
  )

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Alt") {
        e.preventDefault();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      <HotkeysProvider>
        {children}
      </HotkeysProvider>
    </QueryClientProvider>
  )
}
