"use client";

import React from "react";
import { PaisesClient } from "@/api/client";
import { API_URL } from "@/api/url";
import { PaisesList } from "./list";
import { PaisesUpsert } from "./upsert";
import { PaisDto } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";

export type { PaisDto };

interface PaisesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (pais: PaisDto) => void;
  initialSearchTerm?: string;
}

export function PaisesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: PaisesFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<PaisDto>({
    queryKey: "paises",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const client = new PaisesClient(API_URL);
      const res = await client.getPaises(
        searchTerm || undefined,
        page,
        pageSize,
      );
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens as unknown as PaisDto[],
        totalPages: Math.ceil((res.totalDeItens ?? 0) / pageSize),
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (id) => {
      await new PaisesClient(API_URL).deletePais(id);
    },
  });

  return (
    <>
      <PaisesList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <PaisesUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir País"
        description={
          <p>
            Deseja realmente excluir o país{" "}
            <strong>{list.itemToDelete?.pais}</strong> (
            {list.itemToDelete?.siglaIso})? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
