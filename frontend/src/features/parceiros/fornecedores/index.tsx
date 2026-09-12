"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { fornecedoresApi } from "@/api/parceiros";
import { useUi } from "@/ui/imperative";
import { useFeatureList } from "@/hooks/use-feature-list";
import { FornecedoresList } from "./list";
import { FornecedoresUpsert, FornecedoresUpsertProps } from "./upsert";
import { Fornecedor } from "./types";

interface FornecedoresFeatureProps {
  selectionMode?: boolean;
  onSelect?: (fornecedor: Fornecedor) => void | Promise<void>;
  initialSearchTerm?: string;
}

export function FornecedoresFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: FornecedoresFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Fornecedor>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["fornecedores", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await fornecedoresApi.list(
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
    await queryClient.invalidateQueries({ queryKey: ["fornecedores"] });
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, FornecedoresUpsertProps>({
      component: FornecedoresUpsert,
      props: { editingItem: null },
      title: "Novo Fornecedor",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Fornecedor criado com sucesso." });
    }
  };

  const openEdit = async (item: Fornecedor, readOnly = false) => {
    const result = await ui.windows.open<true, FornecedoresUpsertProps>({
      component: FornecedoresUpsert,
      props: { editingItem: item, readOnly },
      title: readOnly ? "Visualizar Fornecedor" : "Editar Fornecedor",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Fornecedor atualizado com sucesso." });
    }
  };

  const openView = async (item: Fornecedor) => {
    await openEdit(item, true);
  };

  const selectFornecedor = async (item: Fornecedor) => {
    await onSelect?.(item);
  };

  const deleteFornecedor = async (item: Fornecedor) => {
    const result = await ui.windows.confirm({
      title: "Excluir Fornecedor",
      description: (
        <p>
          Deseja realmente excluir o fornecedor <strong>{item.nomeRazaosocial}</strong>? Esta ação não poderá ser desfeita.
        </p>
      ),
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await fornecedoresApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Fornecedor excluído com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir o fornecedor.",
      });
    }
  };

  return (
    <FornecedoresList
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
      onDelete={deleteFornecedor}
      onPageChange={list.setPage}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
      selectionMode={selectionMode}
      onSelect={selectFornecedor}
    />
  );
}
