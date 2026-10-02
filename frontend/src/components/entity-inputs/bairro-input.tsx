"use client"
import React from "react"
import { useUi } from "@/ui/imperative";
import { BairrosUpsert } from "@/features/localizacao/bairros/upsert";
import { EntityInput } from "@/ui/composites"
import {
  BairrosFeature,
  Bairro,
} from "@/features/localizacao/bairros"
import { bairrosApi } from "@/api/localizacao"

interface BairroInputProps {
  name: string
  label?: string
  error?: string
  disabled?: boolean
  initialItem?: Bairro | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Bairro | null) => void
}

export function BairroInput({
  name,
  label = "Bairro",
  error,
  disabled = false,
  initialItem,
  onSelectId,
  onSelectItem,
}: BairroInputProps) {
  const ui = useUi()
  return (
    <EntityInput<Bairro, Bairro>
      name={name}
      label={label}
      error={error}
      disabled={disabled}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      onView={(item) =>
        ui.windows.open({ component: BairrosUpsert, props: { editingItem: item, readOnly: true }, title: "Visualizar Bairro", size: "full" })}
      modalTitle="Selecionar Bairro"
      getDisplayLabel={(item) => item?.bairro ?? ""}
      getSearchTerm={(item) => item.bairro}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await bairrosApi.getById(id)
        } catch {
          return null
        }
      }}
      fetchList={async (term) => {
        try {
          const res = await bairrosApi.list(term.trim() || undefined, 1, 10)
          return res ? { itens: res.itens } : null
        } catch {
          return null
        }
      }}
      renderFeature={(props) => <BairrosFeature {...props} />}
    />
  )
}
