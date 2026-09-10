"use client"
import React from "react"
import { EntityInput } from "@/ui/composites"
import { PaisesFeature, Pais, formatPaisLabel } from "@/features/localizacao/paises"
import { paisesApi } from "@/api/localizacao"

interface PaisInputProps {
  name: string
  label?: string
  error?: string
  disabled?: boolean
  initialItem?: Pais | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Pais | null) => void
}

export function PaisInput({
  name,
  label = "País",
  error,
  disabled = false,
  initialItem,
  onSelectId,
  onSelectItem,
}: PaisInputProps) {
  return (
    <EntityInput<Pais, Pais>
      name={name}
      label={label}
      error={error}
      disabled={disabled}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar País"
      getDisplayLabel={formatPaisLabel}
      getSearchTerm={(item) => item.pais}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await paisesApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          return await paisesApi.list(term.trim() || undefined, 1, 10)
        } catch { return null }
      }}
      renderFeature={(props) => <PaisesFeature {...props} />}
    />
  )
}
