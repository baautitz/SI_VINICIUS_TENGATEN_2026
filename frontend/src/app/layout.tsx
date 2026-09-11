import type { Metadata } from "next";
import "./globals.css";
import { Inter } from "next/font/google";
import { cn } from "@/lib/utils";
import { SidebarProvider } from "@/ui/primitives";
import { AppSidebar } from "@/components/app-sidebar";
import { TooltipProvider } from "@/ui/primitives";
import { Providers } from "@/providers/query-provider";
import { Toaster } from "@/ui/primitives";
import { WindowManagerHost } from "@/ui/imperative";
import { WindowManagerProvider } from "@/ui/imperative";
import {
  KeyboardNavigationProvider,
  NavigationScope,
} from "@/ui/keyboard-navigation";

const inter = Inter({ subsets: ["latin"], variable: "--font-sans" });

export const metadata: Metadata = {
  title: "Sistema de Gestão",
  description: "Módulos de Gestão e Localização",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="pt-BR"
      className={cn("font-sans", inter.variable)}
      suppressHydrationWarning
    >
      <body className="bg-background text-foreground isolate min-h-screen antialiased">
        <Providers>
          <KeyboardNavigationProvider>
            <WindowManagerProvider>
              <TooltipProvider>
                <SidebarProvider>
                  <NavigationScope id="application-shell">
                    <div className="flex h-screen w-full">
                      <AppSidebar />
                      <main className="flex flex-1 flex-col bg-slate-50/50 p-6 dark:bg-slate-900/10">
                        {children}
                      </main>
                    </div>
                  </NavigationScope>
                </SidebarProvider>
              </TooltipProvider>
              <Toaster position="top-right" />
              <WindowManagerHost />
            </WindowManagerProvider>
          </KeyboardNavigationProvider>
        </Providers>
      {/* impeccable-live-start */}
<script src="http://localhost:8400/live.js" async></script>
{/* impeccable-live-end */}
</body>
    </html>
  );
}
