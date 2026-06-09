"use client";
import React from "react";
import { EntityInput } from "@/components/ui/entity-input";
import { TransportadorasFeature, Transportadora } from "@/features/logistica/transportadoras";
import { transportadorasApi } from "@/api/logistica";

interface TransportadoraInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: Transportadora | null;
  onSelectId: (id: number | null) => void;
}

export function TransportadoraInput({
  name,
  label = "Transportadora",
  error,
  initialItem,
  onSelectId,
}: TransportadoraInputProps) {
  return (
    <EntityInput<Transportadora, Transportadora>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
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
