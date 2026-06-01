import type { Metadata } from "next";
import "./globals.css";
import { Inter } from "next/font/google";
import { cn } from "@/lib/utils";
import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import { AppSidebar } from "@/components/app-sidebar";
import { TooltipProvider } from "@/components/ui/tooltip";
import { Providers } from "@/providers/query-provider";

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
    <html lang="pt-BR" className={cn("font-sans", inter.variable)}>
      <body className="antialiased min-h-screen bg-background text-foreground">
        <Providers>
          <TooltipProvider>
          <SidebarProvider>
            <div className="flex min-h-screen w-full">
              <AppSidebar />
              <div className="flex-1 flex flex-col min-h-screen">
                <header className="flex h-12 shrink-0 items-center gap-2 border-b border-sidebar-border px-4 bg-card">
                  <SidebarTrigger className="-ml-1 text-muted-foreground hover:text-foreground transition-colors duration-200" />
                  <div className="h-4 w-px bg-border mx-2" />
                  <h1 className="text-sm font-semibold text-foreground/80 tracking-wide">
                    Painel de Administração
                  </h1>
                </header>
                <main className="flex-1 p-6 bg-slate-50/50 dark:bg-slate-900/10">
                  {children}
                </main>
              </div>
            </div>
          </SidebarProvider>
          </TooltipProvider>
        </Providers>
      </body>
    </html>
  );
}
