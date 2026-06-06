"use client";

import React from "react";
import { ProdutosList } from "./list";
import { ProdutosUpsert } from "./upsert";
import { ProdutoResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { produtosApi } from "@/api/catalogo";
import { MovimentacoesUpsert, ItemLinha } from "../../estoque/movimentacoes/upsert";

export * from "./types";

interface ProdutosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (attr: ProdutoResumo) => void;
  initialSearchTerm?: string;
}

export function ProdutosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: ProdutosFeatureProps) {
  const [adjustmentItems, setAdjustmentItems] = React.useState<ItemLinha[] | null>(null);

  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<ProdutoResumo>({
    queryKey: "produtos",
    initialSearchTerm,
    additionalKeysToInvalidate: [["skus"]],
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await produtosApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (item) => {
      await produtosApi.delete(item.id);
    },
  });

  return (
    <>
      <ProdutosList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <ProdutosUpsert 
          key={list.editingItem?.id ?? "new"} 
          {...upsertProps} 
          onSuccessWithAdjustment={(items) => {
            setAdjustmentItems(items);
            upsertProps.onSuccess();
          }}
        />
      )}

      {adjustmentItems && (
        <MovimentacoesUpsert
          open={!!adjustmentItems}
          editingItem={null}
          initialItems={adjustmentItems}
          fixedTipo="BALANCO"
          onClose={() => setAdjustmentItems(null)}
          onSuccess={() => {
            setAdjustmentItems(null);
            listProps.onPageChange(1);
          }}
        />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Produto"
        description={
          <p>
            Deseja realmente excluir o produto{" "}
            <strong>{list.itemToDelete?.produto}</strong>? Esta ação excluirá todos os seus SKUs associados e não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
