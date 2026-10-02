"use client"
import React from "react"
import { useUi } from "@/ui/imperative";
import { ProdutosUpsert } from "@/features/catalogo/produtos/upsert";
import { EntityInput } from "@/ui/composites"
import { ProdutosFeature, Produto } from "@/features/catalogo/produtos"
import { produtosApi } from "@/api/catalogo"

interface ProdutoInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: Produto | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Produto | null) => void
  disabled?: boolean
}

export function ProdutoInput({
  name,
  label = "Produto",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  disabled = false,
}: ProdutoInputProps) {
  const ui = useUi()
  return (
    <EntityInput<Produto, Produto>
      name={name}
      label={label}
      error={error}
      disabled={disabled}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      onView={(item) =>
        ui.windows.open({ component: ProdutosUpsert, props: { editingItem: item, readOnly: true }, title: "Visualizar Produto", size: "full" })}
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
