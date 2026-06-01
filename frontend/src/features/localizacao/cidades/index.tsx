"use client";

import React from "react";
import { CidadesClient } from "@/api/client";
import { API_URL } from "@/api/url";
import { CidadesList } from "./list";
import { CidadesUpsert } from "./upsert";
import { CidadeDto } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";

export type { CidadeDto };

interface CidadesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (cidade: CidadeDto) => void;
  initialSearchTerm?: string;
}

export function CidadesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: CidadesFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<CidadeDto>({
    queryKey: "cidades",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const client = new CidadesClient(API_URL);
      const res = await client.getCidades(
        searchTerm || undefined,
        page,
        pageSize,
      );
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens as unknown as CidadeDto[],
        totalPages: Math.ceil((res.totalDeItens ?? 0) / pageSize),
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (id) => {
      await new CidadesClient(API_URL).deleteCidade(id);
    },
  });

  return (
    <>
      <CidadesList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <CidadesUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Cidade"
        description={
          <p>
            Deseja realmente excluir a cidade{" "}
            <strong>{list.itemToDelete?.cidade}</strong> (
            {list.itemToDelete?.uf})? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
