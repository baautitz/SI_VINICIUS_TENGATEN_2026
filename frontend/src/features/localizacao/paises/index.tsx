"use client";

import React from "react";
import { PaisesList } from "./list";
import { PaisesUpsert } from "./upsert";
import { Pais } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { paisesApi } from "@/api/localizacao";

export * from "./types";

export const formatPaisLabel = (item: Pais) => `${item.pais} (${item.siglaIso})`;

interface PaisesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (pais: Pais) => void;
  initialSearchTerm?: string;
}

export function PaisesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: PaisesFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<Pais>({
    queryKey: "paises",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await paisesApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (item) => {
      await paisesApi.delete(item.id);
    },
  });

  return (
    <>
      <PaisesList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <PaisesUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir País"
        description={
          <p>
            Deseja realmente excluir o país{" "}
            <strong>{list.itemToDelete?.pais}</strong> (
            {list.itemToDelete?.siglaIso})? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
