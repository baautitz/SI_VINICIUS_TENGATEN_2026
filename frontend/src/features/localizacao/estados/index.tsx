"use client";

import React from "react";
import { EstadosClient } from "@/api/client";
import { API_URL } from "@/api/url";
import { EstadosList } from "./list";
import { EstadosUpsert } from "./upsert";
import { EstadoDto } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";

export type { EstadoDto };

interface EstadosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (estado: EstadoDto) => void;
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
  } = useFeatureOrchestrator<EstadoDto>({
    queryKey: "estados",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const client = new EstadosClient(API_URL);
      const res = await client.getEstados(
        searchTerm || undefined,
        page,
        pageSize,
      );
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens as unknown as EstadoDto[],
        totalPages: Math.ceil((res.totalDeItens ?? 0) / pageSize),
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (id) => {
      await new EstadosClient(API_URL).deleteEstado(id);
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
