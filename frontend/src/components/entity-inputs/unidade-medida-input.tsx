"use client"
import React from "react"
import { useUi } from "@/ui/imperative";
import { UnidadesMedidaUpsert } from "@/features/catalogo/unidades-medida/upsert";
import { EntityInput } from "@/ui/composites"
import { UnidadesMedidaFeature, UnidadeMedida } from "@/features/catalogo/unidades-medida"
import { unidadesMedidaApi } from "@/api/catalogo"

interface UnidadeMedidaInputProps {
  name: string
  label?: string
  error?: string
  disabled?: boolean
  initialItem?: UnidadeMedida | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: UnidadeMedida | null) => void
}

export function UnidadeMedidaInput({
  name,
  label = "Unidade de Medida",
  error,
  disabled = false,
  initialItem,
  onSelectId,
  onSelectItem,
}: UnidadeMedidaInputProps) {
  const ui = useUi()
  return (
    <EntityInput<UnidadeMedida, UnidadeMedida>
      name={name}
      label={label}
      error={error}
      disabled={disabled}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      onView={(item, readOnly) =>
        ui.windows.open({ component: UnidadesMedidaUpsert, props: { editingItem: item, readOnly }, title: readOnly ? "Visualizar Unidade de Medida" : "Editar Unidade de Medida", size: "full" })}
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
