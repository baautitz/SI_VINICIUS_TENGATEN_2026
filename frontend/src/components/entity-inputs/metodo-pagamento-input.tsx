"use client";

import { useUi } from "@/ui/imperative";
import { MetodosUpsert } from "@/features/financeiro/metodos/upsert";
import { EntityInput } from "@/ui/composites";
import { MetodosFeature } from "@/features/financeiro/metodos";
import { metodosApi } from "@/api/financeiro";
import { MetodoPagamento } from "@/features/financeiro/metodos/types";

interface MetodoPagamentoInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: MetodoPagamento | null;
  onSelectCodigo: (codigo: string | null) => void;
  onSelectItem?: (item: MetodoPagamento | null) => void;
  disabled?: boolean;
}

export function MetodoPagamentoInput({
  name,
  label = "Método de Pagamento",
  error,
  initialItem,
  onSelectCodigo,
  onSelectItem,
  disabled,
}: MetodoPagamentoInputProps) {
  const ui = useUi()
  return (
    <EntityInput<MetodoPagamento, MetodoPagamento, string>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectCodigo}
      onSelectItem={onSelectItem}
      disabled={disabled}
      onView={(item) =>
        ui.windows.open({ component: MetodosUpsert, props: { editingItem: item, readOnly: true }, title: "Visualizar Método de Pagamento", size: "full" })}
      modalTitle="Selecionar Método de Pagamento"
      getDisplayLabel={(item) =>
        item ? `${item.codigo} - ${item.descricao}` : ""
      }
      getSearchTerm={(item) => item.descricao}
      getId={(item) => item.codigo}
      fetchById={async (codigo) => {
        try {
          return await metodosApi.getById(String(codigo));
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

