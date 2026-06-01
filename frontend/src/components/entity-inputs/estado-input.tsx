"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { EstadosClient } from "@/api/client"
import { API_URL } from "@/api/url"
import { EstadosFeature, EstadoDto } from "@/features/localizacao/estados"

interface EstadoInputProps {
  name: string
  label?: string
  error?: string
  initialDisplayValue?: string
  onSelectId: (id: number | undefined) => void
  inputSize?: "small" | "medium" | "large" | "full"
}

export function EstadoInput({
  name,
  label = "Estado",
  error,
  initialDisplayValue,
  onSelectId,
  inputSize
}: EstadoInputProps) {
  const client = React.useMemo(() => new EstadosClient(API_URL), [])
  
  return (
    <EntityInput<EstadoDto>
      name={name}
      label={label}
      error={error}
      initialDisplayValue={initialDisplayValue}
      onSelectId={onSelectId}
      inputSize={inputSize}
      modalTitle="Selecionar Estado"
      getDisplayLabel={(item) => `${item.estado} (${item.uf})`}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          const res = await client.getEstado(id)
          return res ? ({ 
            id: res.id ?? 0, 
            estado: res.estado ?? "", 
            uf: res.uf ?? "",
            paisId: res.pais?.id ?? 0,
            paisNome: res.pais?.pais ?? ""
          } as EstadoDto) : null
        } catch { return null }
      }}
      fetchList={async (term) => {
        return await client.getEstados(term.trim() || undefined, 1, 10)
      }}
      renderFeature={(props) => <EstadosFeature {...props} />}
    />
  )
}
