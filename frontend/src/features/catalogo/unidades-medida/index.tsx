"use client";

import React from "react";
import { UnidadesMedidaList } from "./list";
import { UnidadesMedidaUpsert } from "./upsert";
import { UnidadeMedida } from "./types";
import { unidadesMedidaApi } from "@/api/catalogo";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

export * from "./types";

interface UnidadesMedidaFeatureProps {
  selectionMode?: boolean;
  onSelect?: (unidade: UnidadeMedida) => void;
  initialSearchTerm?: string;
}

export function UnidadesMedidaFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: UnidadesMedidaFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<UnidadeMedida>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["unidadesMedida", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await unidadesMedidaApi.list(
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
      queryClient.invalidateQueries({ queryKey: ["unidadesMedida"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
    ]);
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, UnidadesMedidaUpsertProps>({
      component: UnidadesMedidaUpsert,
      props: { editingItem: null },
      title: "Nova Unidade de Medida",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: "Unidade de medida criada com sucesso.",
      });
    }
  };

  const openEdit = async (item: UnidadeMedida) => {
    const result = await ui.windows.open<true, UnidadesMedidaUpsertProps>({
      component: UnidadesMedidaUpsert,
      props: { editingItem: item },
      title: "Editar Unidade de Medida",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: "Unidade de medida atualizada com sucesso.",
      });
    }
  };

  const deleteUnidade = async (item: UnidadeMedida) => {
    const result = await ui.windows.confirm({
      title: "Excluir Unidade de Medida",
      description: `Deseja realmente excluir a unidade de medida ${item.descricao}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    await unidadesMedidaApi.delete(item.id);
    await invalidate();
    ui.feedback.notify({
      type: "success",
      title: "Unidade de medida excluída com sucesso.",
    });
  };

  return (
    <>
      <UnidadesMedidaList
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
        onDelete={deleteUnidade}
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

export interface UnidadesMedidaUpsertProps {
  editingItem: UnidadeMedida | null;
  readOnly?: boolean;
}
