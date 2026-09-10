"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";
import { useFeatureList } from "@/hooks/use-feature-list";
import { ProdutosList } from "./list";
import { ProdutosUpsert } from "./upsert";
import type { ProdutosUpsertProps } from "./upsert";
import { Produto } from "./types";
import { produtosApi } from "@/api/catalogo";

export * from "./types";

interface ProdutosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (produto: Produto) => void;
  initialSearchTerm?: string;
}

export function ProdutosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: ProdutosFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Produto>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["produtos", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await produtosApi.list(
        list.deferredSearch.trim() || undefined,
        list.page,
        50,
      );
      return {
        itens: res?.itens ?? [],
        totalPages: res?.totalDePaginas ?? 1,
        totalItems: res?.totalDeItens ?? 0,
      };
    },
  });

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
      queryClient.invalidateQueries({ queryKey: ["skus"] }),
    ]);
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, ProdutosUpsertProps>({
      component: ProdutosUpsert,
      props: { editingItem: null },
      title: "Novo Produto",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Produto criado com sucesso." });
    }
  };

  const openEdit = async (item: Produto) => {
    const result = await ui.windows.open<true, ProdutosUpsertProps>({
      component: ProdutosUpsert,
      props: { editingItem: item },
      title: "Editar Produto",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Produto atualizado com sucesso." });
    }
  };

  const openView = async (item: Produto) => {
    await ui.windows.open<true, ProdutosUpsertProps>({
      component: ProdutosUpsert,
      props: { editingItem: item, readOnly: true },
      title: "Visualizar Produto",
    });
  };

  const deleteProduto = async (item: Produto) => {
    const result = await ui.windows.confirm({
      title: "Excluir Produto",
      description: `Deseja realmente excluir o produto ${item.produto}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    await produtosApi.delete(item.id);
    await invalidate();
    ui.feedback.notify({ type: "success", title: "Produto excluído com sucesso." });
  };

  return (
    <>
      <ProdutosList
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
        onDelete={deleteProduto}
        onPageChange={list.setPage}
        rowSelection={list.rowSelection}
        onRowSelectionChange={list.setRowSelection}
        selectAllAcrossPages={list.selectAllAcrossPages}
        onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />
    </>
  );
}

export type { ProdutosUpsertProps } from "./upsert";
