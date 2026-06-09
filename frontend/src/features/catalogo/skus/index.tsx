"use client";

import React from "react";
import { SkusList } from "./list";
import { Sku } from "./types";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { skusApi, produtosApi } from "@/api/catalogo";
import { ProdutosUpsert } from "@/features/catalogo/produtos/upsert";
import { Produto } from "@/features/catalogo/produtos/types";

export * from "./types";

interface SkusFeatureProps {
  selectionMode?: boolean;
  onSelect?: (sku: Sku) => void;
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
  } = useFeatureOrchestrator<Sku>({
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
    fetchById: async (id) => {
      return await skusApi.getBySku(id as string);
    },
  });

  const [editingProduct, setEditingProduct] = React.useState<Produto | null>(null);

  const handleEdit = async (sku: Sku) => {
    try {
      const produto = await produtosApi.getBySku(sku.sku);
      setEditingProduct(produto);
    } catch (e) {
      console.error("Erro ao buscar produto pelo SKU", e);
    }
  };

  return (
    <>
      <SkusList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
        onEdit={handleEdit}
        onAdd={() => list.handleCreate()}
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
