"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { atributosApi } from "@/api/catalogo";
import { useUi } from "@/ui/imperative";
import { useFeatureList } from "@/hooks/use-feature-list";
import { AtributosList } from "./list";
import { AtributosUpsert } from "./upsert";
import type { AtributosUpsertProps } from "./upsert";
import { SkuAtributoChave } from "./types";

export * from "./types";

interface AtributosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (atributo: SkuAtributoChave) => void;
  initialSearchTerm?: string;
}

export function AtributosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: AtributosFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<SkuAtributoChave>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["atributos", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await atributosApi.list(
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
      queryClient.invalidateQueries({ queryKey: ["atributos"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
    ]);
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, AtributosUpsertProps>({
      component: AtributosUpsert,
      props: { editingItem: null },
      title: "Novo Atributo",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Atributo criado com sucesso." });
    }
  };

  const openEdit = async (item: SkuAtributoChave) => {
    const result = await ui.windows.open<true, AtributosUpsertProps>({
      component: AtributosUpsert,
      props: { editingItem: item },
      title: "Editar Atributo",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Atributo atualizado com sucesso." });
    }
  };

  const deleteAtributo = async (item: SkuAtributoChave) => {
    const result = await ui.windows.confirm({
      title: "Excluir Atributo",
      description: `Deseja realmente excluir o atributo ${item.chave}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await atributosApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Atributo excluído com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir o atributo.",
      });
    }
  };

  return (
    <AtributosList
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
      onDelete={deleteAtributo}
      onSelect={onSelect}
      onPageChange={list.setPage}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
      selectionMode={selectionMode}
    />
  );
}
