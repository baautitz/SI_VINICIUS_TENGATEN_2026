"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { EstadosFeature, Estado, EstadoResumo, formatEstadoLabel } from "@/features/localizacao/estados"
import { estadosApi } from "@/api/localizacao"

interface EstadoInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: Estado | EstadoResumo | null
  onSelectId: (id: number | null) => void
  inputSize?: "small" | "medium" | "large" | "full"
}

export function EstadoInput({
  name,
  label = "Estado",
  error,
  initialItem,
  onSelectId,
  inputSize
}: EstadoInputProps) {
  return (
    <EntityInput<Estado, EstadoResumo>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      inputSize={inputSize}
      modalTitle="Selecionar Estado"
      getDisplayLabel={formatEstadoLabel}
      getSearchTerm={(item) => item.estado}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await estadosApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          return await estadosApi.list(term.trim() || undefined, 1, 10)
        } catch { return null }
      }}
      renderFeature={(props) => <EstadosFeature {...props} />}
    />
  )
}
