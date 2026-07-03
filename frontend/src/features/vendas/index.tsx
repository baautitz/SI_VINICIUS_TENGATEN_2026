"use client";

import React from "react";
import { VendasList } from "./list";
import { VendasUpsertForm } from "./upsert";
import type { VendasResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { vendasApi } from "@/api/vendas";
import { toast } from "sonner";

export * from "./types";

export function VendasFeature() {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  } = useFeatureOrchestrator<any>({
    queryKey: "vendas",
    initialSearchTerm: "",
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await vendasApi.list(
        searchTerm || undefined,
        page,
        pageSize
      );
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    fetchById: async (id) => {
      return await vendasApi.getById(id as number);
    },
    deleteItem: async (item) => {
      await vendasApi.delete(item.id);
    },
    additionalKeysToInvalidate: [
      ["skus"],
      ["produtos"],
      ["contas-receber"],
      ["movimentacoes"],
    ],
  });

  const handleAddWrapper = () => {
    listProps.onAdd();
  };

  const handleViewWrapper = (item: VendasResumo) => {
    listProps.onView(item);
  };

  return (
    <>
      <VendasList
        {...listProps}
        onAdd={handleAddWrapper}
        onView={handleViewWrapper}
      />

      {list.isUpsertOpen && (
        <VendasUpsertForm
          key={list.editingItem?.id ?? "new"}
          open={list.isUpsertOpen}
          editingItem={upsertProps.editingItem}
          loading={upsertProps.loading}
          onClose={upsertProps.onClose}
          onSuccess={() => {
            upsertProps.onSuccess();
            toast.success(
              list.editingItem
                ? "Venda consultada com sucesso!"
                : "Venda registrada com sucesso!"
            );
          }}
          readOnly={list.readOnly}
        />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Cancelar / Excluir Venda"
        description={
          <div className="flex flex-col gap-2">
            <p>
              Deseja realmente cancelar/excluir a venda{" "}
              <strong>#{list.itemToDelete?.id}</strong>?
            </p>
            <p className="text-xs text-red-500 font-semibold">
              Esta ação irá reverter a movimentação de estoque física dos produtos e
              excluir a conta a receber financeira gerada, caso nenhuma parcela tenha sido baixada.
            </p>
          </div>
        }
      />
    </>
  );
}
