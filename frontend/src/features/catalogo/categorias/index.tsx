"use client";

import React from "react";
import { CategoriasList } from "./list";
import { CategoriasUpsert } from "./upsert";
import { CategoriaResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { categoriasApi } from "@/api/catalogo";

export * from "./types";

interface CategoriasFeatureProps {
  selectionMode?: boolean;
  onSelect?: (categoria: CategoriaResumo) => void;
  initialSearchTerm?: string;
}

export function CategoriasFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: CategoriasFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<CategoriaResumo>({
    queryKey: "categorias",
    initialSearchTerm,
    additionalKeysToInvalidate: [["produtos"], ["skus"]],
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await categoriasApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (item) => {
      await categoriasApi.delete(item.id);
    },
  });

  return (
    <>
      <CategoriasList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <CategoriasUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Categoria"
        description={
          <p>
            Deseja realmente excluir a categoria{" "}
            <strong>{list.itemToDelete?.categoria}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
