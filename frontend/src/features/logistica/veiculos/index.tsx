"use client";

import React from "react";
import { VeiculosList } from "./list";
import { VeiculosUpsert } from "./upsert";
import { VeiculoResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { veiculosApi } from "@/api/logistica";

interface VeiculosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (veiculo: VeiculoResumo) => void;
  initialSearchTerm?: string;
}

export function VeiculosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: VeiculosFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<VeiculoResumo>({
    queryKey: "veiculos",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await veiculosApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (id) => {
      await veiculosApi.delete(id);
    },
  });

  return (
    <>
      <VeiculosList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <VeiculosUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Veículo"
        description={
          <p>
            Deseja realmente excluir o veículo{" "}
            <strong>{list.itemToDelete?.placa}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
