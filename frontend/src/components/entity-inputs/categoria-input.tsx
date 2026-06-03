"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { CategoriasFeature, Categoria, CategoriaResumo, formatCategoriaLabel } from "@/features/catalogo/categorias"
import { categoriasApi } from "@/api/catalogo"

interface CategoriaInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: Categoria | CategoriaResumo | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Categoria | null) => void
  inputSize?: "small" | "medium" | "large" | "full"
}

export function CategoriaInput({
  name,
  label = "Categoria",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  inputSize
}: CategoriaInputProps) {
  return (
    <EntityInput<Categoria, CategoriaResumo>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      inputSize={inputSize}
      modalTitle="Selecionar Categoria"
      getDisplayLabel={formatCategoriaLabel}
      getSearchTerm={(item) => item.categoria}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await categoriasApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          return await categoriasApi.list(term.trim() || undefined, 1, 10)
        } catch { return null }
      }}
      renderFeature={(props) => <CategoriasFeature {...props} />}
    />
  )
}
