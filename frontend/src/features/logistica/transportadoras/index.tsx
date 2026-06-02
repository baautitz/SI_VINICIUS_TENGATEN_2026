"use client";

import React from "react";
import { TransportadorasList } from "./list";
import { TransportadorasUpsert } from "./upsert";
import { TransportadoraResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { transportadorasApi } from "@/api/logistica";

interface TransportadorasFeatureProps {
  selectionMode?: boolean;
  onSelect?: (transportadora: TransportadoraResumo) => void;
  initialSearchTerm?: string;
}

export function TransportadorasFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: TransportadorasFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<TransportadoraResumo>({
    queryKey: "transportadoras",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await transportadorasApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (id) => {
      await transportadorasApi.delete(id);
    },
  });

  return (
    <>
      <TransportadorasList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <TransportadorasUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Transportadora"
        description={
          <p>
            Deseja realmente excluir a transportadora{" "}
            <strong>{list.itemToDelete?.nomeRazaosocial}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
