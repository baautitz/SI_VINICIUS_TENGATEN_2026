"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { CidadesFeature, Cidade, CidadeResumo, formatCidadeLabel } from "@/features/localizacao/cidades"
import { cidadesApi } from "@/api/localizacao"

interface CidadeInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: Cidade | CidadeResumo | null
  onSelectId: (id: number | null) => void
}

export function CidadeInput({
  name,
  label = "Cidade",
  error,
  initialItem,
  onSelectId,
}: CidadeInputProps) {
  return (
    <EntityInput<Cidade, CidadeResumo>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      modalTitle="Selecionar Cidade"
      getDisplayLabel={formatCidadeLabel}
      getSearchTerm={(item) => item.cidade}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await cidadesApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          return await cidadesApi.list(term.trim() || undefined, 1, 10)
        } catch { return null }
      }}
      renderFeature={(props) => <CidadesFeature {...props} />}
    />
  )
}
