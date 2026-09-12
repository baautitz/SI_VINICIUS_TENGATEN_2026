"use client";

import React from "react";
import { CondicoesList } from "./list";
import { CondicoesUpsert } from "./upsert";
import { CondicaoPagamento } from "./types";
import { condicoesApi } from "@/api/financeiro";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

interface CondicoesFeatureProps {
  selectionMode?: boolean;
  onSelect?: (condicao: CondicaoPagamento) => void;
  initialSearchTerm?: string;
}

export function CondicoesFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: CondicoesFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<CondicaoPagamento>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["condicoesPagamento", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await condicoesApi.list(
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
      queryClient.invalidateQueries({ queryKey: ["condicoesPagamento"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
    ]);
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, CondicoesUpsertProps>({
      component: CondicoesUpsert,
      props: { editingItem: null },
      title: "Nova Condição de Pagamento",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Condição criada com sucesso." });
    }
  };

  const openEdit = async (item: CondicaoPagamento) => {
    const result = await ui.windows.open<true, CondicoesUpsertProps>({
      component: CondicoesUpsert,
      props: { editingItem: item },
      title: "Editar Condição de Pagamento",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Condição atualizada com sucesso." });
    }
  };

  const openView = async (item: CondicaoPagamento) => {
    await ui.windows.open<true, CondicoesUpsertProps>({
      component: CondicoesUpsert,
      props: { editingItem: item, readOnly: true },
      title: "Visualizar Condição de Pagamento",
    });
  };

  const deleteCondicao = async (item: CondicaoPagamento) => {
    const result = await ui.windows.confirm({
      title: "Excluir Condição de Pagamento",
      description: (
        <p>
          Deseja realmente excluir a condição de pagamento{" "}
          <strong>{item.descricao}</strong>? Esta ação não poderá ser desfeita.
        </p>
      ),
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await condicoesApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Condição excluída com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir a condição de pagamento.",
      });
    }
  };

  return (
    <CondicoesList
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
      onDelete={deleteCondicao}
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

export interface CondicoesUpsertProps {
  editingItem: CondicaoPagamento | null;
  readOnly?: boolean;
}
