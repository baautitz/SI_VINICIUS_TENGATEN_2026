"use client";

import React from "react";
import { ProdutosList } from "./list";
import { ProdutosUpsert } from "./upsert";
import { Produto } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { produtosApi } from "@/api/catalogo";

export * from "./types";

interface ProdutosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (produto: Produto) => void;
  initialSearchTerm?: string;
}

export function ProdutosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: ProdutosFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<Produto>({
    queryKey: "produtos",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await produtosApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    fetchById: async (id) => {
      const res = await produtosApi.getById(id);
      return res;
    },
    deleteItem: async (item) => {
      await produtosApi.delete(item.id);
    },
  });

  return (
    <>
      <ProdutosList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <ProdutosUpsert 
          key={list.editingItem?.id ?? "new"} 
          {...upsertProps} 
        />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Produto"
        description={
          <p>
            Deseja realmente excluir o produto{" "}
            <strong>{list.itemToDelete?.produto}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
