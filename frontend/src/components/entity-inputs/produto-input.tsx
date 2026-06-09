"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { ProdutosFeature, Produto } from "@/features/catalogo/produtos"
import { produtosApi } from "@/api/catalogo"

interface ProdutoInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: Produto | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Produto | null) => void
}

export function ProdutoInput({
  name,
  label = "Produto",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
}: ProdutoInputProps) {
  return (
    <EntityInput<Produto, Produto>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Produto"
      getDisplayLabel={(item) => item?.produto ?? ""}
      getSearchTerm={(item) => item.produto}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await produtosApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          const res = await produtosApi.list(term.trim() || undefined, 1, 10)
          return res ? { itens: res.itens } : null
        } catch { return null }
      }}
      renderFeature={(props) => <ProdutosFeature {...props} />}
    />
  )
}
