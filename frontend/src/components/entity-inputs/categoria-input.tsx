"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { CategoriasFeature, Categoria } from "@/features/catalogo/categorias"
import { categoriasApi } from "@/api/catalogo"

interface CategoriaInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: Categoria | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Categoria | null) => void
}

export function CategoriaInput({
  name,
  label = "Categoria",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
}: CategoriaInputProps) {
  return (
    <EntityInput<Categoria, Categoria>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
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
