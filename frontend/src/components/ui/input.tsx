import * as React from "react"

import { cn } from "@/lib/utils"

export interface InputProps extends React.ComponentProps<"input"> {
  inputSize?: "small" | "medium" | "large" | "full"
}

const Input = ({ className, type, inputSize = "full", autoComplete = "off", ref, ...props }: InputProps) => {
    return (
      <input
        type={type}
        autoComplete={autoComplete}
        data-slot="input"
        ref={ref}
        className={cn(
          "h-8 rounded-lg border border-input bg-transparent px-2.5 py-1 text-base transition-colors outline-none file:inline-flex file:h-6 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:cursor-not-allowed disabled:bg-input/50 disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 md:text-sm dark:bg-input/30 dark:disabled:bg-input/80 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40",
          inputSize === "full" && "w-full",
          inputSize === "large" && "w-full max-w-64",
          inputSize === "medium" && "w-full max-w-48",
          inputSize === "small" && "w-full max-w-32",
          className
        )}
        {...props}
      />
    )
}


export { Input }
