"use client"

import React from "react"
import { Plus, Search } from "lucide-react"
import { Button } from "@/components/ui/button"
import { InputGroup, InputGroupAddon, InputGroupInput } from "@/components/ui/input-group"

interface FeatureHeaderProps {
  title: string
  icon: React.ReactNode
  searchPlaceholder?: string
  searchTerm: string
  onSearchChange: (value: string) => void
  addButtonLabel?: string
  onAdd?: () => void
  selectionMode?: boolean
}

export function FeatureHeader({
  title,
  icon,
  searchPlaceholder = "Pesquisar...",
  searchTerm,
  onSearchChange,
  addButtonLabel,
  onAdd,
  selectionMode = false,
}: FeatureHeaderProps) {
  return (
    <div className="flex flex-col gap-4">
      {!selectionMode && (
        <div className="flex flex-col gap-1">
          <h2 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            {React.isValidElement(icon)
              ? React.cloneElement(icon as React.ReactElement<{ className?: string }>, {
                  className: "text-muted-foreground",
                })
              : icon}
            {title}
          </h2>
        </div>
      )}

      <div className="flex items-center justify-between gap-4">
        <InputGroup className="max-w-sm">
          <InputGroupAddon align="inline-start">
            <Search />
          </InputGroupAddon>
          <InputGroupInput
            placeholder={searchPlaceholder}
            value={searchTerm}
            onChange={(e) => onSearchChange(e.target.value)}
          />
        </InputGroup>

        {onAdd && addButtonLabel && (
          <Button variant="default" onClick={onAdd}>
            <Plus />
            {addButtonLabel}
          </Button>
        )}
      </div>
    </div>
  )
}
