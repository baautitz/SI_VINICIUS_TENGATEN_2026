import React from "react"
import { Plus } from "lucide-react"
import { Button } from "@/components/ui/button"

interface FeatureHeaderProps {
  title: string
  icon: React.ReactNode
  onAdd: () => void
  addButtonLabel: string
}

export function FeatureHeader({
  title,
  icon,
  onAdd,
  addButtonLabel,
}: FeatureHeaderProps) {
  return (
    <div className="flex items-center justify-between pr-2">
      <div className="flex items-center gap-2">
        <div className="bg-primary/10 p-2 rounded-lg text-primary">
          <div className="[&_svg]:size-5">
            {icon}
          </div>
        </div>
        <h1 className="text-xl font-bold tracking-tight text-foreground">{title}</h1>
      </div>
      
        <Button onClick={onAdd} className="rounded-lg shadow-sm">
          <Plus className="h-4 w-4" />
          {addButtonLabel}
        </Button>
      
    </div>
  )
}
