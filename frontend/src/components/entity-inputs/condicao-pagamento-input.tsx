"use client";
import React from "react";
import { EntityInput } from "@/components/ui/entity-input";
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
      onSelectItem={onSelectItem}
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
          return res ? { itens: res.itens } : null;
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <CondicoesFeature {...props} />}
    />
  );
}

