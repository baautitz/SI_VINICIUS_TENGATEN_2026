"use client";

import React from "react";
import { FornecedoresList } from "./list";
import { FornecedoresUpsert } from "./upsert";
import { FornecedorResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { fornecedoresApi } from "@/api/parceiros";

interface FornecedoresFeatureProps {
  selectionMode?: boolean;
  onSelect?: (fornecedor: FornecedorResumo) => void;
  initialSearchTerm?: string;
}

export function FornecedoresFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: FornecedoresFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<FornecedorResumo>({
    queryKey: "fornecedores",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await fornecedoresApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (item) => {
      await fornecedoresApi.delete(item.id);
    },
  });

  return (
    <>
      <FornecedoresList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <FornecedoresUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Fornecedor"
        description={
          <p>
            Deseja realmente excluir o fornecedor{" "}
            <strong>{list.itemToDelete?.nomeRazaosocial}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
