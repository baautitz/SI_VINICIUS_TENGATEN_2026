"use client";

import React from "react";
import { EstadosList } from "./list";
import { EstadosUpsert } from "./upsert";
import { EstadoResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { estadosApi } from "@/api/localizacao";

export * from "./types";

interface EstadosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (estado: EstadoResumo) => void;
  initialSearchTerm?: string;
}

export function EstadosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: EstadosFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<EstadoResumo>({
    queryKey: "estados",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await estadosApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (item) => {
      await estadosApi.delete(item.id);
    },
  });

  return (
    <>
      <EstadosList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <EstadosUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Estado"
        description={
          <p>
            Deseja realmente excluir o estado{" "}
            <strong>{list.itemToDelete?.estado}</strong> (
            {list.itemToDelete?.uf})? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
