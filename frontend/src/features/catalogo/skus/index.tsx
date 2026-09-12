"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";
import { useFeatureList } from "@/hooks/use-feature-list";
import { skusApi, produtosApi } from "@/api/catalogo";
import {
  ProdutosUpsert,
  type ProdutosUpsertProps,
} from "@/features/catalogo/produtos/upsert";
import type { Produto } from "@/features/catalogo/produtos/types";
import { SkusList } from "./list";
import { Sku } from "./types";

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
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Sku>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["skus", list.deferredSearch, list.page],
    queryFn: async () => {
      const result = await skusApi.list(
        list.deferredSearch.trim() || undefined,
        list.page,
        50,
      );
      return {
        itens: result?.itens ?? [],
        totalPages: result?.totalDePaginas ?? 1,
        totalItems: result?.totalDeItens ?? 0,
      };
    },
  });

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["skus"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
    ]);
  };

  const openProductWindow = async (
    editingItem: Produto | null,
    readOnly = false,
  ) => {
    const result = await ui.windows.open<true, ProdutosUpsertProps>({
      component: ProdutosUpsert,
      props: { editingItem, readOnly },
      title: readOnly
        ? "Visualizar Produto"
        : editingItem
          ? "Editar Produto"
          : "Novo Produto",
    });

    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: editingItem
          ? "Produto atualizado com sucesso."
          : "Produto criado com sucesso.",
      });
    }
  };

  const openCreate = async () => {
    await openProductWindow(null);
  };

  const loadProduct = async (sku: Sku) => {
    if (!sku.produto?.id) {
      ui.feedback.notify({
        type: "error",
        title: "Produto não encontrado.",
        description: "Não foi possível abrir o produto deste SKU.",
      });
      return null;
    }

    try {
      return await produtosApi.getById(sku.produto.id);
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível carregar o produto.",
      });
      return null;
    }
  };

  const openEdit = async (sku: Sku) => {
    const product = await loadProduct(sku);
    if (product) await openProductWindow(product);
  };

  const openView = async (sku: Sku) => {
    const product = await loadProduct(sku);
    if (product) await openProductWindow(product, true);
  };

  const deleteSku = async (sku: Sku) => {
    const result = await ui.windows.confirm({
      title: "Excluir SKU",
      description:
        `O SKU ${sku.sku} é gerenciado pelo formulário do produto. ` +
        "Deseja abrir o produto para revisar suas variações?",
      confirmLabel: "Abrir produto",
    });
    if (!result) return;

    const product = await loadProduct(sku);
    if (product) await openProductWindow(product);
  };

  return (
    <SkusList
      items={data?.itens ?? []}
      loading={isLoading}
      searchTerm={list.searchTerm}
      page={list.page}
      totalPages={data?.totalPages ?? 1}
      totalItems={data?.totalItems ?? 0}
      onSearchChange={list.handleSearchChange}
      onAdd={openCreate}
      onEdit={openEdit}
      onView={openView}
      onDelete={deleteSku}
      onPageChange={list.setPage}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
      selectionMode={selectionMode}
      onSelect={onSelect}
      searchInputRef={searchInputRef}
    />
  );
}
