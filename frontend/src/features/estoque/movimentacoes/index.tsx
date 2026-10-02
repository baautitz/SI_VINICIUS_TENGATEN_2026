"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { estoqueApi } from "@/api/estoque";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useUi } from "@/ui/imperative";
import { MovimentacoesList } from "./list";
import { MovimentacoesUpsert } from "./upsert";
import { MovimentacaoEstoque } from "./types";

export * from "./types";

export function MovimentacoesFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<MovimentacaoEstoque>();
  const { data, isLoading } = useQuery({
    queryKey: ["movimentacoes", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await estoqueApi.list(
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
      queryClient.invalidateQueries({ queryKey: ["movimentacoes"] }),
      queryClient.invalidateQueries({ queryKey: ["skus"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
    ]);
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, MovimentacoesUpsertProps>({
      component: MovimentacoesUpsert,
      props: { editingItem: null },
      title: "Nova Movimentação de Estoque",
      size: "full",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Movimentação salva com sucesso." });
    }
  };

  const openEdit = async (item: MovimentacaoEstoque) => {
    if (item.status !== "RASCUNHO") return openView(item);
    const result = await ui.windows.open<true, MovimentacoesUpsertProps>({
      component: MovimentacoesUpsert,
      props: { editingItem: item },
      title: `Editar Movimentação #${item.id}`,
      size: "full",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Movimentação atualizada com sucesso." });
    }
  };

  const openView = async (item: MovimentacaoEstoque) => {
    await ui.windows.open<true, MovimentacoesUpsertProps>({
      component: MovimentacoesUpsert,
      props: { editingItem: item, readOnly: true },
      title: `Visualizar Movimentação #${item.id}`,
      size: "full",
    });
  };

  const deleteItem = async (item: MovimentacaoEstoque) => {
    const result = await ui.windows.confirm({
      title: "Excluir Rascunho de Movimentação",
      description: `Deseja realmente excluir o rascunho de movimentação #${item.id}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await estoqueApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Rascunho excluído com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir o rascunho de movimentação.",
      });
    }
  };

  const confirmAction = async (item: MovimentacaoEstoque) => {
    const result = await ui.windows.confirm({
      title: "Efetivar Movimentação?",
      description: `Deseja realmente efetivar a movimentação de estoque #${item.id}? Isso alterará de forma definitiva o saldo físico dos produtos no catálogo.`,
      confirmLabel: "Efetivar",
    });
    if (!result) return;
    try {
      const response = await estoqueApi.confirmar(item.id);
      if (response.success === false) {
        ui.feedback.notifyError(response, {
          fallbackTitle: "Não foi possível efetivar a movimentação.",
        });
        return;
      }
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Movimentação efetivada com sucesso!" });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível efetivar a movimentação.",
      });
    }
  };

  const cancelAction = async (item: MovimentacaoEstoque) => {
    const result = await ui.windows.confirm({
      title: "Estornar Movimentação?",
      description: `Deseja realmente estornar/cancelar a movimentação #${item.id}? Isso reverterá o impacto das quantidades no saldo físico dos produtos.`,
      confirmLabel: "Confirmar Estorno",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      const response = await estoqueApi.cancelar(item.id);
      if (response.success === false) {
        ui.feedback.notifyError(response, {
          fallbackTitle: "Não foi possível estornar a movimentação.",
        });
        return;
      }
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Movimentação estornada com sucesso!" });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível estornar a movimentação.",
      });
    }
  };

  return (
    <MovimentacoesList
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
      onDelete={deleteItem}
      onConfirm={confirmAction}
      onCancel={cancelAction}
      onPageChange={list.setPage}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
    />
  );
}

export interface MovimentacoesUpsertProps {
  editingItem: MovimentacaoEstoque | null;
  readOnly?: boolean;
  initialItems?: import("./upsert").ItemLinha[];
  fixedTipo?: "ENTRADA" | "SAIDA" | "BALANCO" | "VENDA";
}
