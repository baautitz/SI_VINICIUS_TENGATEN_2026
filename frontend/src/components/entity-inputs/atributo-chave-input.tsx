"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { AtributosFeature, SkuAtributoChave, SkuAtributoChaveResumo, formatSkuAtributoChaveLabel } from "@/features/catalogo/atributos"
import { atributosApi } from "@/api/catalogo"

interface AtributoChaveInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: SkuAtributoChave | SkuAtributoChaveResumo | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: SkuAtributoChave | null) => void
  inputSize?: "small" | "medium" | "large" | "full"
}

export function AtributoChaveInput({
  name,
  label = "Atributo",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  inputSize
}: AtributoChaveInputProps) {
  return (
    <EntityInput<SkuAtributoChave, SkuAtributoChaveResumo>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      inputSize={inputSize}
      modalTitle="Selecionar Atributo"
      getDisplayLabel={formatSkuAtributoChaveLabel}
      getSearchTerm={(item) => item.chave}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await atributosApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          return await atributosApi.list(term.trim() || undefined, 1, 10)
        } catch { return null }
      }}
      renderFeature={(props) => <AtributosFeature {...props} />}
    />
  )
}
