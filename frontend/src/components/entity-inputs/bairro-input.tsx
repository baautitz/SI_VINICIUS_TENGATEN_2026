"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import {
  BairrosFeature,
  Bairro,
} from "@/features/localizacao/bairros"
import { bairrosApi } from "@/api/localizacao"

interface BairroInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: Bairro | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Bairro | null) => void
}

export function BairroInput({
  name,
  label = "Bairro",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
}: BairroInputProps) {
  return (
    <EntityInput<Bairro, Bairro>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
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
