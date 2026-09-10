"use client";

import { useQuery } from "@tanstack/react-query";
import { estoqueApi } from "@/api/estoque";
import { Spinner } from "@/ui/primitives";
import { MovimentacaoEstoque } from "./types";
import { MovimentacoesUpsertForm } from "./upsert-form";

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

export interface MovimentacoesUpsertProps {
  editingItem: MovimentacaoEstoque | null;
  readOnly?: boolean;
  initialItems?: ItemLinha[];
  fixedTipo?: "ENTRADA" | "SAIDA" | "BALANCO" | "VENDA";
}

export function MovimentacoesUpsert(props: MovimentacoesUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["movimentacoes", "detail", editingItem?.id],
    queryFn: () => estoqueApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );
  }

  return (
    <MovimentacoesUpsertForm
      {...props}
      editingItem={isEditMode ? (fullItem ?? editingItem) : null}
      readOnly={readOnly}
    />
  );
}
