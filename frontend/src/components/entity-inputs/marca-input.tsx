"use client"
import React from "react"
import { useUi } from "@/ui/imperative";
import { MarcasUpsert } from "@/features/catalogo/marcas/upsert";
import { EntityInput } from "@/ui/composites"
import { MarcasFeature, Marca } from "@/features/catalogo/marcas"
import { marcasApi } from "@/api/catalogo"

interface MarcaInputProps {
  name: string
  label?: string
  error?: string
  disabled?: boolean
  initialItem?: Marca | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Marca | null) => void
}

export function MarcaInput({
  name,
  label = "Marca",
  error,
  disabled = false,
  initialItem,
  onSelectId,
  onSelectItem,
}: MarcaInputProps) {
  const ui = useUi()
  return (
    <EntityInput<Marca, Marca>
      name={name}
      label={label}
      error={error}
      disabled={disabled}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      onView={(item, readOnly) =>
        ui.windows.open({ component: MarcasUpsert, props: { editingItem: item, readOnly }, title: readOnly ? "Visualizar Marca" : "Editar Marca", size: "full" })}
      modalTitle="Selecionar Marca"
      getDisplayLabel={(item) => item?.marca ?? ""}
      getSearchTerm={(item) => item.marca}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await marcasApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          const res = await marcasApi.list(term.trim() || undefined, 1, 10)
          return res ? { itens: res.itens } : null
        } catch { return null }
      }}
      renderFeature={(props) => <MarcasFeature {...props} />}
    />
  )
}
