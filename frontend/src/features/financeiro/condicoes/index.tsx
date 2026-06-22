"use client";

import React from "react";
import { CondicoesList } from "./list";
import { CondicoesUpsert } from "./upsert";
import { CondicaoPagamento } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { condicoesApi } from "@/api/financeiro";

interface CondicoesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (condicao: CondicaoPagamento) => void;
  initialSearchTerm?: string;
}

export function CondicoesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: CondicoesFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<CondicaoPagamento>({
    queryKey: "condicoesPagamento",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await condicoesApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    fetchById: async (id) => {
      return await condicoesApi.getById(id as number);
    },
    deleteItem: async (item) => {
      await condicoesApi.delete(item.id);
    },
  });

  return (
    <>
      <CondicoesList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <CondicoesUpsert
          key={list.editingItem?.id ?? "new"}
          {...upsertProps}
        />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Condição de Pagamento"
        description={
          <p>
            Deseja realmente excluir a condição de pagamento{" "}
            <strong>{list.itemToDelete?.descricao}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}

