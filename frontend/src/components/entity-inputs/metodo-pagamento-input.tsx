"use client";

import { EntityInput } from "@/components/ui/entity-input";
import { MetodosFeature } from "@/features/pagamentos/metodos";
import { metodosApi } from "@/api/pagamentos";
import { MetodoPagamento } from "@/features/pagamentos/metodos/types";

interface MetodoPagamentoInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: MetodoPagamento | null;
  onSelectId: (id: number | null) => void;
  onSelectItem?: (item: MetodoPagamento | null) => void;
  disabled?: boolean;
}

export function MetodoPagamentoInput({
  name,
  label = "Método de Pagamento",
  error,
  initialItem,
  onSelectId,
  onSelectItem,
  disabled,
}: MetodoPagamentoInputProps) {
  return (
    <EntityInput<MetodoPagamento, MetodoPagamento>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      onSelectItem={onSelectItem}
      disabled={disabled}
      modalTitle="Selecionar Método de Pagamento"
      getDisplayLabel={(item) =>
        item ? `${item.codigo} - ${item.descricao}` : ""
      }
      getSearchTerm={(item) => item.descricao}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await metodosApi.getById(id);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          const res = await metodosApi.list(term.trim() || undefined, 1, 10);
          return res ? { itens: res.itens } : null;
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <MetodosFeature {...props} />}
    />
  );
}
