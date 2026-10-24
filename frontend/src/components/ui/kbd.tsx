import { ArrowLeft, ArrowRight } from "lucide-react"

import { cn } from "@/lib/utils"

function Kbd({ className, ...props }: React.ComponentProps<"kbd">) {
  return (
    <kbd
      data-slot="kbd"
      className={cn(
        "pointer-events-none inline-flex h-5 w-fit min-w-5 items-center justify-center gap-1 rounded-sm bg-muted px-1 font-sans text-xs font-medium text-muted-foreground select-none in-data-[slot=tooltip-content]:bg-background/20 in-data-[slot=tooltip-content]:text-background dark:in-data-[slot=tooltip-content]:bg-background/10 [&_svg:not([class*='size-'])]:size-3",
        className
      )}
      {...props}
    />
  )
}

function KbdGroup({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <kbd
      data-slot="kbd-group"
      className={cn("inline-flex items-center gap-1", className)}
      {...props}
    />
  )
}

const KEY_ICONS: Record<string, React.ReactNode> = {
  ArrowLeft: <ArrowLeft className="size-3" />,
  ArrowRight: <ArrowRight className="size-3" />,
}

/** Exibe um atalho ("Alt+Enter") como um Kbd por tecla. */
function HotkeyKbd({ hotkey, className }: { hotkey: string; className?: string }) {
  return (
    <KbdGroup className={className}>
      {hotkey.split("+").map((key) => (
        <Kbd key={key}>{KEY_ICONS[key] ?? key}</Kbd>
      ))}
    </KbdGroup>
  )
}

export { Kbd, KbdGroup, HotkeyKbd }
