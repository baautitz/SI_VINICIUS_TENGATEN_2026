"use client";
import React from "react";
import { useUi } from "@/ui/imperative";
import { FornecedoresUpsert } from "@/features/parceiros/fornecedores/upsert";
import { EntityInput } from "@/ui/composites";
import { FornecedoresFeature } from "@/features/parceiros/fornecedores";
import { Fornecedor } from "@/features/parceiros/fornecedores/types";
import { fornecedoresApi } from "@/api/parceiros";

interface FornecedorInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: Fornecedor | null;
  onSelectId: (id: number | null) => void;
  onSelectItem?: (item: Fornecedor | null) => void;
  disabled?: boolean;
}

export function FornecedorInput({
  name,
  label = "Fornecedor",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  disabled = false,
}: FornecedorInputProps) {
  const ui = useUi()
  return (
    <EntityInput<Fornecedor, Fornecedor>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      onView={(item, readOnly) =>
        ui.windows.open({ component: FornecedoresUpsert, props: { editingItem: item, readOnly }, title: readOnly ? "Visualizar Fornecedor" : "Editar Fornecedor", size: "full" })}
      modalTitle="Selecionar Fornecedor"
      getDisplayLabel={(item) => item?.nomeRazaosocial ?? ""}
      getSearchTerm={(item) => item.nomeRazaosocial}
      getId={(item) => item.id}
      disabled={disabled}
      fetchById={async (id) => {
        try {
          return await fornecedoresApi.getById(id as number);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          const res = await fornecedoresApi.list(term.trim() || undefined, 1, 10);
          return res ? { itens: res.itens } : null;
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <FornecedoresFeature {...props} />}
    />
  );
}
