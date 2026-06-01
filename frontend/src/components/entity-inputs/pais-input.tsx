"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { PaisesClient } from "@/api/client"
import { API_URL } from "@/api/url"
import { PaisesFeature, PaisDto } from "@/features/localizacao/paises"

interface PaisInputProps {
  name: string
  label?: string
  error?: string
  initialDisplayValue?: string
  onSelectId: (id: number | undefined) => void
  inputSize?: "small" | "medium" | "large" | "full"
}

export function PaisInput({
  name,
  label = "País",
  error,
  initialDisplayValue,
  onSelectId,
  inputSize
}: PaisInputProps) {
  const client = React.useMemo(() => new PaisesClient(API_URL), [])
  
  return (
    <EntityInput<PaisDto>
      name={name}
      label={label}
      error={error}
      initialDisplayValue={initialDisplayValue}
      onSelectId={onSelectId}
      inputSize={inputSize}
      modalTitle="Selecionar País"
      getDisplayLabel={(item) => `${item.pais} (${item.siglaIso})`}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          const res = await client.getPais(id)
          return res ? ({ 
            id: res.id ?? 0, 
            pais: res.pais ?? "", 
            siglaIso: res.siglaIso ?? "",
            ddi: res.ddi ?? "",
            moeda: res.moeda ?? "",
            simboloMoeda: res.simboloMoeda ?? ""
          } as PaisDto) : null
        } catch { return null }
      }}
      fetchList={async (term) => {
        return await client.getPaises(term.trim() || undefined, 1, 10)
      }}
      renderFeature={(props) => <PaisesFeature {...props} />}
    />
  )
}
