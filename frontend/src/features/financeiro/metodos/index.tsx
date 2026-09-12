"use client";

import React from "react";
import { MetodosList } from "./list";
import { MetodosUpsert } from "./upsert";
import { MetodoPagamento } from "./types";
import { metodosApi } from "@/api/financeiro";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

interface MetodosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (metodo: MetodoPagamento) => void;
  initialSearchTerm?: string;
}

export function MetodosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: MetodosFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<MetodoPagamento>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["metodosPagamento", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await metodosApi.list(
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
    await queryClient.invalidateQueries({ queryKey: ["metodosPagamento"] });
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, MetodosUpsertProps>({
      component: MetodosUpsert,
      props: { editingItem: null },
      title: "Novo Método de Pagamento",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Método de pagamento criado com sucesso." });
    }
  };

  const openEdit = async (item: MetodoPagamento) => {
    const result = await ui.windows.open<true, MetodosUpsertProps>({
      component: MetodosUpsert,
      props: { editingItem: item },
      title: "Editar Método de Pagamento",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Método de pagamento atualizado com sucesso." });
    }
  };

  const openView = async (item: MetodoPagamento) => {
    await ui.windows.open<true, MetodosUpsertProps>({
      component: MetodosUpsert,
      props: { editingItem: item, readOnly: true },
      title: "Visualizar Método de Pagamento",
    });
  };

  const deleteMetodo = async (item: MetodoPagamento) => {
    const result = await ui.windows.confirm({
      title: "Excluir Método de Pagamento",
      description: (
        <p>
          Deseja realmente excluir o método de pagamento{" "}
          <strong>{item.descricao}</strong>? Esta ação não poderá ser desfeita.
        </p>
      ),
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await metodosApi.delete(item.codigo);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Método de pagamento excluído com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir o método de pagamento.",
      });
    }
  };

  return (
    <>
      <MetodosList
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
        onDelete={deleteMetodo}
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

export interface MetodosUpsertProps {
  editingItem: MetodoPagamento | null;
  readOnly?: boolean;
}
