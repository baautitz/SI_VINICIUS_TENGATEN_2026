"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { PaisesFeature, Pais, PaisResumo, formatPaisLabel } from "@/features/localizacao/paises"
import { paisesApi } from "@/api/localizacao"

interface PaisInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: Pais | PaisResumo | null
  onSelectId: (id: number | null) => void
  inputSize?: "small" | "medium" | "large" | "full"
}

export function PaisInput({
  name,
  label = "País",
  error,
  initialItem,
  onSelectId,
  inputSize
}: PaisInputProps) {
  return (
    <EntityInput<Pais, PaisResumo>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      inputSize={inputSize}
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
