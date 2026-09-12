"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { transportadorasApi } from "@/api/parceiros";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useUi } from "@/ui/imperative";
import { TransportadorasList } from "./list";
import { TransportadorasUpsert, TransportadorasUpsertProps } from "./upsert";
import { Transportadora } from "./types";

interface TransportadorasFeatureProps {
  selectionMode?: boolean;
  onSelect?: (transportadora: Transportadora) => void;
  initialSearchTerm?: string;
}

export * from "./types";

export function TransportadorasFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: TransportadorasFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Transportadora>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["transportadoras", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await transportadorasApi.list(
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
    await queryClient.invalidateQueries({ queryKey: ["transportadoras"] });
  };

  const openUpsert = async (
    editingItem: Transportadora | null,
    readOnly = false,
  ) => {
    const result = await ui.windows.open<true, TransportadorasUpsertProps>({
      component: TransportadorasUpsert,
      props: { editingItem, readOnly },
      title: readOnly
        ? "Visualizar Transportadora"
        : editingItem
          ? "Editar Transportadora"
          : "Nova Transportadora",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: editingItem
          ? "Transportadora atualizada com sucesso."
          : "Transportadora criada com sucesso.",
      });
    }
  };

  const deleteTransportadora = async (item: Transportadora) => {
    const result = await ui.windows.confirm({
      title: "Excluir Transportadora",
      description: `Deseja realmente excluir a transportadora ${item.nomeRazaosocial}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await transportadorasApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Transportadora excluída com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir a transportadora.",
      });
    }
  };

  const openCreate = async () => openUpsert(null);
  const openEdit = async (item: Transportadora) => openUpsert(item);
  const openView = async (item: Transportadora) => openUpsert(item, true);

  return (
    <TransportadorasList
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
      onDelete={deleteTransportadora}
      onPageChange={list.setPage}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
      selectionMode={selectionMode}
      onSelect={onSelect}
    />
  );
}
