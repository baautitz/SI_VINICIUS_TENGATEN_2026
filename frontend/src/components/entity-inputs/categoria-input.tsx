"use client"
import React from "react"
import { useUi } from "@/ui/imperative";
import { CategoriasUpsert } from "@/features/catalogo/categorias/upsert";
import { EntityInput } from "@/ui/composites"
import { CategoriasFeature, Categoria } from "@/features/catalogo/categorias"
import { categoriasApi } from "@/api/catalogo"

interface CategoriaInputProps {
  name: string
  label?: string
  error?: string
  disabled?: boolean
  initialItem?: Categoria | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Categoria | null) => void
}

export function CategoriaInput({
  name,
  label = "Categoria",
  error,
  disabled = false,
  initialItem,
  onSelectId,
  onSelectItem,
}: CategoriaInputProps) {
  const ui = useUi()
  return (
    <EntityInput<Categoria, Categoria>
      name={name}
      label={label}
      error={error}
      disabled={disabled}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      onView={(item, readOnly) =>
        ui.windows.open({ component: CategoriasUpsert, props: { editingItem: item, readOnly }, title: readOnly ? "Visualizar Categoria" : "Editar Categoria", size: "full" })}
      modalTitle="Selecionar Categoria"
      getDisplayLabel={(item) => item?.categoria ?? ""}
      getSearchTerm={(item) => item.categoria}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await categoriasApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          const res = await categoriasApi.list(term.trim() || undefined, 1, 10)
          return res ? { itens: res.itens } : null
        } catch { return null }
      }}
      renderFeature={(props) => <CategoriasFeature {...props} />}
    />
  )
}
