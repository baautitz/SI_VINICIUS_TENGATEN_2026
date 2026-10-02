"use client";
import React from "react";
import { useUi } from "@/ui/imperative";
import { TransportadorasUpsert } from "@/features/parceiros/transportadoras/upsert";
import { EntityInput } from "@/ui/composites";
import { TransportadorasFeature, Transportadora } from "@/features/parceiros/transportadoras";
import { transportadorasApi } from "@/api/parceiros";

interface TransportadoraInputProps {
  name: string;
  label?: string;
  error?: string;
  disabled?: boolean;
  initialItem?: Transportadora | null;
  onSelectId: (id: number | null) => void;
}

export function TransportadoraInput({
  name,
  label = "Transportadora",
  error,
  disabled = false,
  initialItem,
  onSelectId,
}: TransportadoraInputProps) {
  const ui = useUi()
  return (
    <EntityInput<Transportadora, Transportadora>
      name={name}
      label={label}
      error={error}
      disabled={disabled}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onView={(item, readOnly) =>
        ui.windows.open({ component: TransportadorasUpsert, props: { editingItem: item, readOnly }, title: readOnly ? "Visualizar Transportadora" : "Editar Transportadora", size: "full" })}
      modalTitle="Selecionar Transportadora"
      getDisplayLabel={(item) => item.nomeRazaosocial}
      getSearchTerm={(item) => item.nomeRazaosocial}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await transportadorasApi.getById(id);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          return await transportadorasApi.list(term.trim() || undefined, 1, 10);
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <TransportadorasFeature {...props} />}
    />
  );
}
