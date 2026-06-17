"use client";
import React from "react";
import { EntityInput } from "@/components/ui/entity-input";
import { ClientesFeature } from "@/features/parceiros/clientes";
import { Cliente } from "@/features/parceiros/clientes/types";
import { clientesApi } from "@/api/parceiros";

interface ClienteInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: Cliente | null;
  onSelectId: (id: number | null) => void;
  onSelectItem?: (item: Cliente | null) => void;
  disabled?: boolean;
}

export function ClienteInput({
  name,
  label = "Cliente",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  disabled = false,
}: ClienteInputProps) {
  return (
    <EntityInput<Cliente, Cliente>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Cliente"
      getDisplayLabel={(item) => item?.nomeRazaoSocial ?? ""}
      getSearchTerm={(item) => item.nomeRazaoSocial}
      getId={(item) => item.id}
      disabled={disabled}
      fetchById={async (id) => {
        try {
          return await clientesApi.getById(id as number);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          const res = await clientesApi.list(term.trim() || undefined, 1, 10);
          return res ? { itens: res.itens } : null;
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <ClientesFeature {...props} />}
    />
  );
}
