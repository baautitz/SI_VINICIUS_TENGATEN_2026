"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { MarcasFeature, Marca, MarcaResumo, formatMarcaLabel } from "@/features/catalogo/marcas"
import { marcasApi } from "@/api/catalogo"

interface MarcaInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: Marca | MarcaResumo | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Marca | null) => void
}

export function MarcaInput({
  name,
  label = "Marca",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
}: MarcaInputProps) {
  return (
    <EntityInput<Marca, MarcaResumo>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Marca"
      getDisplayLabel={formatMarcaLabel}
      getSearchTerm={(item) => item.marca}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await marcasApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          return await marcasApi.list(term.trim() || undefined, 1, 10)
        } catch { return null }
      }}
      renderFeature={(props) => <MarcasFeature {...props} />}
    />
  )
}
