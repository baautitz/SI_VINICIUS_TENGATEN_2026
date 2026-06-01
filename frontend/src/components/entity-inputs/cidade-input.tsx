"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { CidadesClient } from "@/api/client"
import { API_URL } from "@/api/url"
import { CidadesFeature, CidadeDto } from "@/features/localizacao/cidades"

interface CidadeInputProps {
  name: string
  label?: string
  error?: string
  initialDisplayValue?: string
  onSelectId: (id: number | undefined) => void
  inputSize?: "small" | "medium" | "large" | "full"
}

export function CidadeInput({
  name,
  label = "Cidade",
  error,
  initialDisplayValue,
  onSelectId,
  inputSize
}: CidadeInputProps) {
  const client = React.useMemo(() => new CidadesClient(API_URL), [])
  
  return (
    <EntityInput<CidadeDto>
      name={name}
      label={label}
      error={error}
      initialDisplayValue={initialDisplayValue}
      onSelectId={onSelectId}
      inputSize={inputSize}
      modalTitle="Selecionar Cidade"
      getDisplayLabel={(item) => `${item.cidade} (${item.uf})`}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          const res = await client.getCidade(id)
          return res ? ({ 
            id: res.id ?? 0, 
            cidade: res.cidade ?? "",
            ddd: res.ddd ?? 0,
            estadoId: res.estado?.id ?? 0,
            estadoNome: res.estado?.estado ?? "",
            uf: res.estado?.uf ?? ""
          } as CidadeDto) : null
        } catch { return null }
      }}
      fetchList={async (term) => {
        return await client.getCidades(term.trim() || undefined, 1, 10)
      }}
      renderFeature={(props) => <CidadesFeature {...props} />}
    />
  )
}
