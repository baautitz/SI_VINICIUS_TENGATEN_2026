"use client"
import React from "react"
import { EntityInput } from "@/components/ui/entity-input"
import { AtributosFeature, SkuAtributoChave } from "@/features/catalogo/atributos"
import { atributosApi } from "@/api/catalogo"

interface AtributoChaveInputProps {
  name: string
  label?: string
  error?: string
  initialItem?: SkuAtributoChave | null
  onSelectId: (id: number | null) => void
  onSelectItem?: (item: SkuAtributoChave | null) => void
}

export function AtributoChaveInput({
  name,
  label = "Atributo",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
}: AtributoChaveInputProps) {
  return (
    <EntityInput<SkuAtributoChave, SkuAtributoChave>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Atributo"
      getDisplayLabel={(item) => item?.chave ?? ""}
      getSearchTerm={(item) => item.chave}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await atributosApi.getById(id)
        } catch { return null }
      }}
      fetchList={async (term) => {
        try {
          const res = await atributosApi.list(term.trim() || undefined, 1, 10)
          return res ? { itens: res.itens } : null
        } catch { return null }
      }}
      renderFeature={(props) => <AtributosFeature {...props} />}
    />
  )
}
