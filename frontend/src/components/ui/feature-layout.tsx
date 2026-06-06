import React from "react";
import { cn } from "@/lib/utils";

type FeatureLayoutProps = React.ComponentPropsWithRef<"div">;

export const FeatureLayout = ({ children, className, ref, ...props }: FeatureLayoutProps) => {
  return (
    <div ref={ref} className={cn("flex flex-col gap-4 flex-1 min-h-0", className)} {...props}>
      {children}
    </div>
  );
};

