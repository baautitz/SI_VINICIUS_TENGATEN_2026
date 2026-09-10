"use client"
import React from "react"
import { EntityInput } from "@/ui/composites"
import {
  CidadesFeature,
  Cidade,
} from "@/features/localizacao/cidades"
import { cidadesApi } from "@/api/localizacao"

interface CidadeInputProps {
  name: string
  label?: string
  error?: string
  disabled?: boolean
  initialItem?: Cidade | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Cidade | null) => void
}

export function CidadeInput({
  name,
  label = "Cidade",
  error,
  disabled = false,
  initialItem,
  onSelectId,
  onSelectItem,
}: CidadeInputProps) {
  return (
    <EntityInput<Cidade, Cidade>
      name={name}
      label={label}
      error={error}
      disabled={disabled}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Cidade"
      getDisplayLabel={(item) => item?.cidade ?? ""}
      getSearchTerm={(item) => item.cidade}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await cidadesApi.getById(id)
        } catch {
          return null
        }
      }}
      fetchList={async (term) => {
        try {
          const res = await cidadesApi.list(term.trim() || undefined, 1, 10)
          return res ? { itens: res.itens } : null
        } catch {
          return null
        }
      }}
      renderFeature={(props) => <CidadesFeature {...props} />}
    />
  )
}
