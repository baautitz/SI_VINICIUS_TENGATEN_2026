"use client";

import React from "react";
import { SkusList } from "./list";
import { SkuResumo } from "./types";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { skusApi } from "@/api/catalogo";
import { ProdutosUpsert } from "@/features/catalogo/produtos/upsert";
import { ProdutoResumo } from "@/features/catalogo/produtos/types";

export * from "./types";

interface SkusFeatureProps {
  selectionMode?: boolean;
  onSelect?: (sku: SkuResumo) => void;
  initialSearchTerm?: string;
  searchInputRef?: React.RefObject<HTMLInputElement | null>;
}

export function SkusFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
  searchInputRef,
}: SkusFeatureProps) {
  const {
    listProps,
    featureList: list,
  } = useFeatureOrchestrator<SkuResumo>({
    queryKey: "skus",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await skusApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
  });

  const [editingProduct, setEditingProduct] = React.useState<ProdutoResumo | null>(null);

  const handleEdit = (sku: SkuResumo) => {
    setEditingProduct({
      id: sku.produtoId,
      produto: sku.produtoNome,
      ativo: true,
      estoqueTotal: sku.estoque,
    });
  };

  const handleAdd = () => {
    list.handleCreate();
  };

  return (
    <>
      <SkusList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
        onEdit={handleEdit}
        onAdd={handleAdd}
        searchInputRef={searchInputRef}
      />

      {(list.isUpsertOpen || !!editingProduct) && (
        <ProdutosUpsert
          open={list.isUpsertOpen || !!editingProduct}
          editingItem={editingProduct}
          onClose={() => {
            list.setIsUpsertOpen(false);
            setEditingProduct(null);
          }}
          onSuccess={() => {
            list.setIsUpsertOpen(false);
            setEditingProduct(null);
            listProps.onSearchChange(listProps.searchTerm);
          }}
        />
      )}
    </>
  );
}
