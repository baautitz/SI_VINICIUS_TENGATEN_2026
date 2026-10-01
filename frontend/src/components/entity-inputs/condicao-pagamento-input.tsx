"use client";
import React from "react";
import { EntityInput } from "@/ui/composites";
import { CondicoesFeature } from "@/features/financeiro/condicoes";
import { CondicaoPagamento } from "@/features/financeiro/condicoes/types";
import { condicoesApi } from "@/api/financeiro";

interface CondicaoPagamentoInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: CondicaoPagamento | null;
  onSelectId: (id: number | null) => void;
  onSelectItem?: (item: CondicaoPagamento | null) => void;
  disabled?: boolean;
}

export function CondicaoPagamentoInput({
  name,
  label = "Condição de Pagamento",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  disabled = false,
}: CondicaoPagamentoInputProps) {
  return (
    <EntityInput<CondicaoPagamento, CondicaoPagamento>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={async (item) => {
        if (!onSelectItem) return;
        if (item && !item.condicoesPagamentosParcelas?.length) {
          try {
            onSelectItem(await condicoesApi.getById(item.id));
            return;
          } catch {
            // mantém o item da lista
          }
        }
        onSelectItem(item);
      }}
      modalTitle="Selecionar Condição de Pagamento"
      getDisplayLabel={(item) => item?.descricao ?? ""}
      getSearchTerm={(item) => item.descricao}
      getId={(item) => item.id}
      disabled={disabled}
      fetchById={async (id) => {
        try {
          return await condicoesApi.getById(id as number);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          const res = await condicoesApi.list(term.trim() || undefined, 1, 10);
          return res ? { itens: res.itens.filter((c) => c.ativo) } : null;
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <CondicoesFeature {...props} />}
    />
  );
}

