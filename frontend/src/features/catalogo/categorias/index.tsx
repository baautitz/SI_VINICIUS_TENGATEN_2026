"use client";

import React from "react";
import { CategoriasList } from "./list";
import { CategoriasUpsert } from "./upsert";
import { Categoria } from "./types";
import { categoriasApi } from "@/api/catalogo";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

export * from "./types";

interface CategoriasFeatureProps {
  selectionMode?: boolean;
  onSelect?: (categoria: Categoria) => void;
  initialSearchTerm?: string;
}

export function CategoriasFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: CategoriasFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Categoria>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["categorias", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await categoriasApi.list(
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
      queryClient.invalidateQueries({ queryKey: ["categorias"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
    ]);
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, CategoriasUpsertProps>({
      component: CategoriasUpsert,
      props: { editingItem: null },
      title: "Nova Categoria",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Categoria criada com sucesso." });
    }
  };

  const openEdit = async (item: Categoria) => {
    const result = await ui.windows.open<true, CategoriasUpsertProps>({
      component: CategoriasUpsert,
      props: { editingItem: item },
      title: "Editar Categoria",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Categoria atualizada com sucesso." });
    }
  };

  const deleteCategoria = async (item: Categoria) => {
    const result = await ui.windows.confirm({
      title: "Excluir Categoria",
      description: `Deseja realmente excluir a categoria ${item.categoria}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await categoriasApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Categoria excluída com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir a categoria.",
      });
    }
  };

  return (
    <>
      <CategoriasList
        items={data?.itens ?? []}
        loading={isLoading}
        searchTerm={list.searchTerm}
        page={list.page}
        totalPages={data?.totalPages ?? 1}
        totalItems={data?.totalItems ?? 0}
        onSearchChange={list.handleSearchChange}
        onAdd={openCreate}
        onEdit={openEdit}
        onView={openEdit}
        onDelete={deleteCategoria}
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

export interface CategoriasUpsertProps {
  editingItem: Categoria | null;
  readOnly?: boolean;
}
