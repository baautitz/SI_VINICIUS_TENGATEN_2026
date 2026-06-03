"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { UnidadesMedidaFeature, UnidadeMedida, UnidadeMedidaResumo, formatUnidadeMedidaLabel } from "@/features/catalogo/unidades-medida"
import { unidadesMedidaApi } from "@/api/catalogo"

interface UnidadeMedidaInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: UnidadeMedida | UnidadeMedidaResumo | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: UnidadeMedida | null) => void
  inputSize?: "small" | "medium" | "large" | "full"
}

export function UnidadeMedidaInput({
  name,
  label = "Unidade de Medida",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  inputSize
}: UnidadeMedidaInputProps) {
  return (
    <EntityInput<UnidadeMedida, UnidadeMedidaResumo>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      inputSize={inputSize}
      modalTitle="Selecionar Unidade de Medida"
      getDisplayLabel={formatUnidadeMedidaLabel}
      getSearchTerm={(item) => item.descricao}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await unidadesMedidaApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          return await unidadesMedidaApi.list(term.trim() || undefined, 1, 10)
        } catch { return null }
      }}
      renderFeature={(props) => <UnidadesMedidaFeature {...props} />}
    />
  )
}
