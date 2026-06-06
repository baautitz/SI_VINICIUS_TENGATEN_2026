"use client";

import { useQuery } from "@tanstack/react-query";
import { estoqueApi } from "@/api/estoque";
import { MovimentacaoEstoque } from "./types";
import { MovimentacoesUpsertForm } from "./upsert-form";
import { UpsertDialog } from "@/components/ui/upsert-dialog";

export interface ItemLinha {
  sku: string;
  produtoNome: string;
  quantidade: number;
  custoUnitario: number;
  estoqueAtual?: number;
  precoSugerido?: number;
  custoMedio?: number;
  custoUltimaCompra?: number;
  unidadeMedidaSigla?: string;
  permiteDecimais?: boolean;
}

interface MovimentacoesUpsertProps {
  open: boolean;
  editingItem: MovimentacaoEstoque | null;
  onClose: () => void;
  onSuccess: () => void;
  initialItems?: ItemLinha[];
  fixedTipo?: "ENTRADA" | "SAIDA" | "BALANCO" | "VENDA";
}

export function MovimentacoesUpsert(props: MovimentacoesUpsertProps) {
  const { open, editingItem, onClose, initialItems, fixedTipo } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["movimentacoes", "detail", editingItem?.id],
    queryFn: () => estoqueApi.getById(editingItem!.id),
    enabled: isEditMode && open,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Carregando Movimentação..."
        loading={true}
      />
    );
  }

  const resolvedReadOnly = fullItem ? fullItem.status !== "RASCUNHO" : false;

  return (
    <MovimentacoesUpsertForm
      {...props}
      editingItem={isEditMode ? (fullItem ?? null) : null}
      readOnly={resolvedReadOnly}
      initialItems={initialItems}
      fixedTipo={fixedTipo}
    />
  );
}
