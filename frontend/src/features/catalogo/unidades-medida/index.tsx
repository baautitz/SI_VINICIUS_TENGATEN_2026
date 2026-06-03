"use client";

import React from "react";
import { UnidadesMedidaList } from "./list";
import { UnidadesMedidaUpsert } from "./upsert";
import { UnidadeMedida, UnidadeMedidaResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { unidadesMedidaApi } from "@/api/catalogo";

export * from "./types";

interface UnidadesMedidaFeatureProps {
  selectionMode?: boolean;
  onSelect?: (um: UnidadeMedida) => void;
  initialSearchTerm?: string;
}

export function UnidadesMedidaFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: UnidadesMedidaFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<UnidadeMedidaResumo>({
    queryKey: "unidades-medida",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await unidadesMedidaApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (id) => {
      await unidadesMedidaApi.delete(id);
    },
  });

  return (
    <>
      <UnidadesMedidaList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <UnidadesMedidaUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Unidade de Medida"
        description={
          <p>
            Deseja realmente excluir a unidade de medida{" "}
            <strong>{list.itemToDelete?.descricao}</strong> (
            {list.itemToDelete?.sigla})? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
