"use client";

import React from "react";
import { EmitentesList } from "./list";
import { EmitentesUpsert } from "./upsert";
import { EmitenteResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { emitentesApi } from "@/api/parceiros";

interface EmitentesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (emitente: EmitenteResumo) => void;
  initialSearchTerm?: string;
}

export function EmitentesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: EmitentesFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<EmitenteResumo>({
    queryKey: "emitentes",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await emitentesApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (item) => {
      await emitentesApi.delete(item.id);
    },
  });

  return (
    <>
      <EmitentesList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <EmitentesUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Emitente"
        description={
          <p>
            Deseja realmente excluir o emitente{" "}
            <strong>{list.itemToDelete?.nomeRazaoSocial}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
