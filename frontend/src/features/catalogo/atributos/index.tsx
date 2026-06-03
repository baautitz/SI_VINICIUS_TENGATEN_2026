"use client";

import React from "react";
import { AtributosList } from "./list";
import { AtributosUpsert } from "./upsert";
import { SkuAtributoChaveResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { atributosApi } from "@/api/catalogo";

export * from "./types";

interface AtributosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (attr: SkuAtributoChaveResumo) => void;
  initialSearchTerm?: string;
}

export function AtributosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: AtributosFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<SkuAtributoChaveResumo>({
    queryKey: "atributos",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await atributosApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (id) => {
      await atributosApi.delete(id);
    },
  });

  return (
    <>
      <AtributosList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <AtributosUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Atributo"
        description={
          <p>
            Deseja realmente excluir o atributo{" "}
            <strong>{list.itemToDelete?.chave}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
