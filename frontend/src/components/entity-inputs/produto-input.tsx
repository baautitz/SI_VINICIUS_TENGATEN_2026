"use client";

import React from "react";
import { EntityInput } from "@/components/ui/entity-input";
import { ProdutosFeature, Produto, ProdutoResumo, formatProdutoLabel } from "@/features/catalogo/produtos";
import { produtosApi } from "@/api/catalogo";

interface ProdutoInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: Produto | ProdutoResumo | null;
  onSelectId: (id: number | null) => void;
  onSelectItem?: (item: Produto | null) => void;
}

export function ProdutoInput({
  name,
  label = "Produto",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
}: ProdutoInputProps) {
  return (
    <EntityInput<Produto, ProdutoResumo>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Produto"
      getDisplayLabel={formatProdutoLabel}
      getSearchTerm={(item) => item.produto}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await produtosApi.getById(id);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          return await produtosApi.list(term.trim() || undefined, 1, 10);
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <ProdutosFeature {...props} />}
    />
  );
}
