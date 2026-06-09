import type { Metadata } from "next";
import "./globals.css";
import { Inter } from "next/font/google";
import { cn } from "@/lib/utils";
import { SidebarProvider } from "@/components/ui/sidebar";
import { AppSidebar } from "@/components/app-sidebar";
import { TooltipProvider } from "@/components/ui/tooltip";
import { Providers } from "@/providers/query-provider";
import { Toaster } from "@/components/ui/sonner";

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
      <body className="bg-background text-foreground min-h-screen antialiased">
        <Providers>
          <TooltipProvider>
            <SidebarProvider>
              <div className="flex h-screen w-full overflow-hidden">
                <AppSidebar />
                <div className="flex min-h-screen flex-1 flex-col overflow-y-auto">
                  <main className="flex flex-1 flex-col bg-slate-50/50 p-6 dark:bg-slate-900/10">
                    {children}
                  </main>
                </div>
              </div>
            </SidebarProvider>
          </TooltipProvider>
          <Toaster position="top-right" />
        </Providers>
      </body>
    </html>
  );
}
