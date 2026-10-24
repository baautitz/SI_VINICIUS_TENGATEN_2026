"use client";
import React from "react";
import { useUi } from "@/ui/imperative";
import { EntityInput } from "@/ui/composites";
import { VendasFeature } from "@/features/vendas";
import { VendasUpsertForm, type VendasUpsertProps } from "@/features/vendas/upsert";
import type { Venda } from "@/features/vendas/types";
import { vendasApi } from "@/api/vendas";
import { ShoppingBag } from "lucide-react";

interface VendaInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: Venda | null;
  onSelectId: (id: number | null) => void;
  onSelectItem?: (item: Venda | null) => void;
  disabled?: boolean;
}

export function VendaInput({
  name,
  label = "Venda",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  disabled = false,
}: VendaInputProps) {
  const ui = useUi();
  return (
    <EntityInput<Venda, Venda>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      modalTitle="Selecionar Venda"
      icon={<ShoppingBag />}
      getDisplayLabel={(item) => `#${item.id} - ${item.cliente?.nomeRazaoSocial ?? ""}`}
      getSearchTerm={(item) => item.cliente?.nomeRazaoSocial ?? String(item.id)}
      getId={(item) => item.id}
      disabled={disabled}
      onView={(item) =>
        ui.windows.open<true, VendasUpsertProps>({
          component: VendasUpsertForm,
          props: { editingItem: item, readOnly: true },
          title: "Detalhes da Venda",
          size: "full",
        })
      }
      fetchById={async (id) => {
        try {
          return await vendasApi.getById(id);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          const res = await vendasApi.list(term.trim() || undefined, 1, 10);
          return res ? { itens: res.itens } : null;
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <VendasFeature {...props} />}
    />
  );
}
