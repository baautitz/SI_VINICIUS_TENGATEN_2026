"use client";

import React from "react";
import { MetodosList } from "./list";
import { MetodosUpsert } from "./upsert";
import { MetodoPagamento } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { metodosApi } from "@/api/pagamentos";

interface MetodosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (metodo: MetodoPagamento) => void;
  initialSearchTerm?: string;
}

export function MetodosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: MetodosFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<MetodoPagamento>({
    queryKey: "metodosPagamento",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await metodosApi.list(
        searchTerm || undefined,
        page,
        pageSize,
      );
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    fetchById: async (_id, item) => {
      return await metodosApi.getById(item!.codigo);
    },
    deleteItem: async (item) => {
      await metodosApi.delete(item.codigo);
    },
  });

  return (
    <>
      <MetodosList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <MetodosUpsert key={list.editingItem?.codigo ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Método de Pagamento"
        description={
          <p>
            Deseja realmente excluir o método de pagamento{" "}
            <strong>{list.itemToDelete?.descricao}</strong>? Esta ação não
            poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
