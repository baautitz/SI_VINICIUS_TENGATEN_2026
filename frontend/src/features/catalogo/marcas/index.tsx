"use client";

import React from "react";
import { MarcasList } from "./list";
import { MarcasUpsert } from "./upsert";
import { Marca } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { marcasApi } from "@/api/catalogo";

export * from "./types";

interface MarcasFeatureProps {
  selectionMode?: boolean;
  onSelect?: (marca: Marca) => void;
  initialSearchTerm?: string;
}

export function MarcasFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: MarcasFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<Marca>({
    queryKey: "marcas",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await marcasApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (item) => {
      await marcasApi.delete(item.id);
    },
  });

  return (
    <>
      <MarcasList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <MarcasUpsert 
          key={list.editingItem?.id ?? "new"} 
          {...upsertProps} 
          editingItem={list.editingItem}
        />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Marca"
        description={
          <p>
            Deseja realmente excluir a marca{" "}
            <strong>{list.itemToDelete?.marca}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
