"use client";
import React from "react";
import { useUi } from "@/ui/imperative";
import { EmitentesUpsert } from "@/features/parceiros/emitentes/upsert";
import { EntityInput } from "@/ui/composites";
import { EmitentesFeature } from "@/features/parceiros/emitentes";
import { Emitente } from "@/features/parceiros/emitentes/types";
import { emitentesApi } from "@/api/parceiros";

interface EmitenteInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: Emitente | null;
  onSelectId: (id: number | null) => void;
  onSelectItem?: (item: Emitente | null) => void;
  disabled?: boolean;
}

export function EmitenteInput({
  name,
  label = "Emitente",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  disabled = false,
}: EmitenteInputProps) {
  const ui = useUi();
  return (
    <EntityInput<Emitente, Emitente>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Emitente"
      getDisplayLabel={(item) => item?.nomeRazaoSocial ?? ""}
      getSearchTerm={(item) => item.nomeRazaoSocial}
      getId={(item) => item.id}
      disabled={disabled}
      onView={(item) =>
        ui.windows.open({
          component: EmitentesUpsert,
          props: { editingItem: item, readOnly: true },
          title: "Visualizar Emitente",
          size: "full",
        })
      }
      fetchById={async (id) => {
        try {
          return await emitentesApi.getById(id as number);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          const res = await emitentesApi.list(term.trim() || undefined, 1, 10);
          return res ? { itens: res.itens } : null;
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <EmitentesFeature {...props} />}
    />
  );
}
