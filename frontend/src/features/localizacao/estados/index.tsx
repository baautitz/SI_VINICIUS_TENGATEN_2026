"use client";

import React from "react";
import { EstadosList } from "./list";
import { EstadosUpsert, type EstadosUpsertProps } from "./upsert";
import { Estado } from "./types";
import { useFeatureList } from "@/hooks/use-feature-list";
import { estadosApi } from "@/api/localizacao";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

export * from "./types";

interface EstadosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (estado: Estado) => void;
  initialSearchTerm?: string;
}

export function EstadosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: EstadosFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Estado>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["estados", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await estadosApi.list(list.deferredSearch.trim() || undefined, list.page, 50);
      return { itens: res?.itens ?? [], totalPages: res?.totalDePaginas ?? 1, totalItems: res?.totalDeItens ?? 0 };
    },
  });
  const invalidate = async () => queryClient.invalidateQueries({ queryKey: ["estados"] });
  const openUpsert = async (editingItem: Estado | null, readOnly = false) => {
    const result = await ui.windows.open<true, EstadosUpsertProps>({
      component: EstadosUpsert,
      props: { editingItem, readOnly },
      title: readOnly ? "Visualizar Estado" : editingItem ? "Editar Estado" : "Novo Estado",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: editingItem ? "Estado atualizado com sucesso." : "Estado criado com sucesso." });
    }
  };
  const deleteEstado = async (item: Estado) => {
    const result = await ui.windows.confirm({
      title: "Excluir Estado",
      description: <>Deseja realmente excluir o estado <strong>{item.estado}</strong> ({item.uf})? Esta ação não poderá ser desfeita.</>,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await estadosApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Estado excluído com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir o estado.",
      });
    }
  };

  return (
    <>
      <EstadosList
        items={data?.itens ?? []}
        loading={isLoading}
        searchTerm={list.searchTerm}
        page={list.page}
        totalPages={data?.totalPages ?? 1}
        totalItems={data?.totalItems ?? 0}
        onSearchChange={list.handleSearchChange}
        onAdd={() => openUpsert(null)}
        onEdit={(item) => openUpsert(item)}
        onView={(item) => openUpsert(item, true)}
        onDelete={deleteEstado}
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
