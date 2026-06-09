"use client";

import React from "react";
import { CidadesList } from "./list";
import { CidadesUpsert } from "./upsert";
import { Cidade } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { cidadesApi } from "@/api/localizacao";

export * from "./types";

interface CidadesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (cidade: Cidade) => void;
  initialSearchTerm?: string;
}

export function CidadesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: CidadesFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<Cidade>({
    queryKey: "cidades",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await cidadesApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (item) => {
      await cidadesApi.delete(item.id);
    },
  });

  return (
    <>
      <CidadesList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <CidadesUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Cidade"
        description={
          <p>
            Deseja realmente excluir a cidade{" "}
            <strong>{list.itemToDelete?.cidade}</strong> (
            {list.itemToDelete?.estado.uf})? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
