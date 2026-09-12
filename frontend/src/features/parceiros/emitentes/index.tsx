"use client";

import React from "react";
import { EmitentesList } from "./list";
import { EmitentesUpsert, type EmitentesUpsertProps } from "./upsert";
import { Emitente } from "./types";
import { emitentesApi } from "@/api/parceiros";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

interface EmitentesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (emitente: Emitente) => void;
  initialSearchTerm?: string;
}

export function EmitentesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: EmitentesFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Emitente>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["emitentes", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await emitentesApi.list(
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
      queryClient.invalidateQueries({ queryKey: ["emitentes"] }),
      queryClient.invalidateQueries({ queryKey: ["emitente"] }),
    ]);
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, EmitentesUpsertProps>({
      component: EmitentesUpsert,
      props: { editingItem: null },
      title: "Novo Emitente",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Emitente criado com sucesso." });
    }
  };

  const openEdit = async (item: Emitente) => {
    const result = await ui.windows.open<true, EmitentesUpsertProps>({
      component: EmitentesUpsert,
      props: { editingItem: item },
      title: "Editar Emitente",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Emitente atualizado com sucesso." });
    }
  };

  const openView = async (item: Emitente) => {
    await ui.windows.open<true, EmitentesUpsertProps>({
      component: EmitentesUpsert,
      props: { editingItem: item, readOnly: true },
      title: "Visualizar Emitente",
    });
  };

  const deleteEmitente = async (item: Emitente) => {
    const result = await ui.windows.confirm({
      title: "Excluir Emitente",
      description: (
        <p>
          Deseja realmente excluir o emitente <strong>{item.nomeRazaoSocial}</strong>? Esta ação não poderá ser desfeita.
        </p>
      ),
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await emitentesApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Emitente excluído com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir o emitente.",
      });
    }
  };

  return (
    <EmitentesList
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
      onDelete={deleteEmitente}
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
