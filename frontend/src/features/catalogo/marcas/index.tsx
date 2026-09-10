"use client";

import React from "react";
import { MarcasList } from "./list";
import { MarcasUpsert } from "./upsert";
import { Marca } from "./types";
import { marcasApi } from "@/api/catalogo";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

export * from "./types";

interface MarcasFeatureProps {
  selectionMode?: boolean;
  onSelect?: (marca: Marca) => void;
  initialSearchTerm?: string;
}

export function MarcasFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: MarcasFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Marca>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["marcas", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await marcasApi.list(list.deferredSearch.trim() || undefined, list.page, 50);
      return {
        itens: res?.itens ?? [],
        totalPages: res?.totalDePaginas ?? 1,
        totalItems: res?.totalDeItens ?? 0,
      };
    },
  });

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["marcas"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
    ]);
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, MarcasUpsertProps>({
      component: MarcasUpsert,
      props: { editingItem: null },
      title: "Nova Marca",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Marca criada com sucesso." });
    }
  };

  const openEdit = async (item: Marca) => {
    const result = await ui.windows.open<true, MarcasUpsertProps>({
      component: MarcasUpsert,
      props: { editingItem: item },
      title: "Editar Marca",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Marca atualizada com sucesso." });
    }
  };

  const deleteMarca = async (item: Marca) => {
    const result = await ui.windows.confirm({
      title: "Excluir Marca",
      description: `Deseja realmente excluir a marca ${item.marca}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    await marcasApi.delete(item.id);
    await invalidate();
    ui.feedback.notify({ type: "success", title: "Marca excluída com sucesso." });
  };

  return (
    <>
      <MarcasList
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
        onDelete={deleteMarca}
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

export interface MarcasUpsertProps {
  editingItem: Marca | null;
  readOnly?: boolean;
}
