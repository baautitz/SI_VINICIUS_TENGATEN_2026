"use client"
import React from "react"
import { EntityInput } from "@/ui/composites"
import {
  EstadosFeature,
  Estado,
} from "@/features/localizacao/estados"
import { estadosApi } from "@/api/localizacao"

interface EstadoInputProps {
  name: string
  label?: string
  error?: string
  disabled?: boolean
  initialItem?: Estado | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: Estado | null) => void
}

export function EstadoInput({
  name,
  label = "Estado",
  error,
  disabled = false,
  initialItem,
  onSelectId,
  onSelectItem,
}: EstadoInputProps) {
  return (
    <EntityInput<Estado, Estado>
      name={name}
      label={label}
      error={error}
      disabled={disabled}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Estado"
      getDisplayLabel={(item) => item?.estado ?? ""}
      getSearchTerm={(item) => item.estado}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await estadosApi.getById(id)
        } catch {
          return null
        }
      }}
      fetchList={async (term) => {
        try {
          const res = await estadosApi.list(term.trim() || undefined, 1, 10)
          return res ? { itens: res.itens } : null
        } catch {
          return null
        }
      }}
      renderFeature={(props) => <EstadosFeature {...props} />}
    />
  )
}
