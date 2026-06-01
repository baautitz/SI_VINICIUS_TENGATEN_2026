import React from "react";
import { cn } from "@/lib/utils";

interface FeatureLayoutProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
}

export function FeatureLayout({
  children,
  className,
  ...props
}: FeatureLayoutProps) {
  return (
    <div className={cn("flex flex-col gap-4 h-full", className)} {...props}>
      {children}
    </div>
  );
}
