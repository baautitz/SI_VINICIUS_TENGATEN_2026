"use client";

import React from "react";
import { ClientesList } from "./list";
import { ClientesUpsert } from "./upsert";
import { Cliente } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { clientesApi } from "@/api/parceiros";

interface ClientesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (cliente: Cliente) => void;
  initialSearchTerm?: string;
}

export function ClientesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: ClientesFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<Cliente>({
    queryKey: "clientes",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await clientesApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (item) => {
      await clientesApi.delete(item.id);
    },
  });

  return (
    <>
      <ClientesList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <ClientesUpsert 
          key={list.editingItem?.id ?? "new"} 
          {...upsertProps} 
          editingItem={list.editingItem}
        />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Cliente"
        description={
          <p>
            Deseja realmente excluir o cliente{" "}
            <strong>{list.itemToDelete?.nomeRazaoSocial}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
