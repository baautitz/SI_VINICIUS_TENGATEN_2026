"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { UnidadesMedidaFeature, UnidadeMedida } from "@/features/catalogo/unidades-medida"
import { unidadesMedidaApi } from "@/api/catalogo"

interface UnidadeMedidaInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: UnidadeMedida | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: UnidadeMedida | null) => void
}

export function UnidadeMedidaInput({
  name,
  label = "Unidade de Medida",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
}: UnidadeMedidaInputProps) {
  return (
    <EntityInput<UnidadeMedida, UnidadeMedida>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Unidade de Medida"
      getDisplayLabel={(item) => item?.descricao ?? ""}
      getSearchTerm={(item) => item.descricao}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await unidadesMedidaApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          const res = await unidadesMedidaApi.list(term.trim() || undefined, 1, 10)
          return res ? { itens: res.itens } : null
        } catch { return null }
      }}
      renderFeature={(props) => <UnidadesMedidaFeature {...props} />}
    />
  )
}
