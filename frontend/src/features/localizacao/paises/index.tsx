"use client";

import React from "react";
import { PaisesList } from "./list";
import { PaisesUpsert, type PaisesUpsertProps } from "./upsert";
import { Pais } from "./types";
import { useFeatureList } from "@/hooks/use-feature-list";
import { paisesApi } from "@/api/localizacao";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

export * from "./types";

export const formatPaisLabel = (item: Pais) => `${item.pais} (${item.codigoIsoPais})`;

interface PaisesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (pais: Pais) => void;
  initialSearchTerm?: string;
}

export function PaisesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: PaisesFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Pais>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["paises", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await paisesApi.list(list.deferredSearch.trim() || undefined, list.page, 50);
      return { itens: res?.itens ?? [], totalPages: res?.totalDePaginas ?? 1, totalItems: res?.totalDeItens ?? 0 };
    },
  });

  const invalidate = async () => queryClient.invalidateQueries({ queryKey: ["paises"] });
  const openUpsert = async (editingItem: Pais | null, readOnly = false) => {
    const result = await ui.windows.open<true, PaisesUpsertProps>({
      component: PaisesUpsert,
      props: { editingItem, readOnly },
      title: readOnly ? "Visualizar País" : editingItem ? "Editar País" : "Novo País",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: editingItem ? "País atualizado com sucesso." : "País criado com sucesso." });
    }
  };
  const deletePais = async (item: Pais) => {
    const result = await ui.windows.confirm({
      title: "Excluir País",
      description: <>Deseja realmente excluir o país <strong>{item.pais}</strong> ({item.codigoIsoPais})? Esta ação não poderá ser desfeita.</>,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    await paisesApi.delete(item.id);
    await invalidate();
    ui.feedback.notify({ type: "success", title: "País excluído com sucesso." });
  };

  return (
    <>
      <PaisesList
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
        onDelete={deletePais}
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
