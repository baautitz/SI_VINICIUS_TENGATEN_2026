"use client";

import React from "react";
import { CidadesList } from "./list";
import { CidadesUpsert, type CidadesUpsertProps } from "./upsert";
import { Cidade } from "./types";
import { useFeatureList } from "@/hooks/use-feature-list";
import { cidadesApi } from "@/api/localizacao";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

export * from "./types";

interface CidadesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (cidade: Cidade) => void;
  initialSearchTerm?: string;
}

export function CidadesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: CidadesFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Cidade>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["cidades", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await cidadesApi.list(list.deferredSearch.trim() || undefined, list.page, 50);
      return { itens: res?.itens ?? [], totalPages: res?.totalDePaginas ?? 1, totalItems: res?.totalDeItens ?? 0 };
    },
  });
  const invalidate = async () => queryClient.invalidateQueries({ queryKey: ["cidades"] });
  const openUpsert = async (editingItem: Cidade | null, readOnly = false) => {
    const result = await ui.windows.open<true, CidadesUpsertProps>({
      component: CidadesUpsert,
      props: { editingItem, readOnly },
      title: readOnly ? "Visualizar Cidade" : editingItem ? "Editar Cidade" : "Nova Cidade",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: editingItem ? "Cidade atualizada com sucesso." : "Cidade criada com sucesso." });
    }
  };
  const deleteCidade = async (item: Cidade) => {
    const result = await ui.windows.confirm({
      title: "Excluir Cidade",
      description: <>Deseja realmente excluir a cidade <strong>{item.cidade}</strong> ({item.estado.uf})? Esta ação não poderá ser desfeita.</>,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    await cidadesApi.delete(item.id);
    await invalidate();
    ui.feedback.notify({ type: "success", title: "Cidade excluída com sucesso." });
  };

  return (
    <>
      <CidadesList
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
        onDelete={deleteCidade}
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
