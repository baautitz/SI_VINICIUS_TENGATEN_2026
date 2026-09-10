"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useUi } from "@/ui/imperative";
import { veiculosApi } from "@/api/parceiros";
import { VeiculosList } from "./list";
import { VeiculosUpsert, VeiculosUpsertProps } from "./upsert";
import { Veiculo } from "./types";

interface VeiculosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (veiculo: Veiculo) => void;
  initialSearchTerm?: string;
}

export function VeiculosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: VeiculosFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Veiculo>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["veiculos", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await veiculosApi.list(
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
    await queryClient.invalidateQueries({ queryKey: ["veiculos"] });
  };

  const openUpsert = async (
    editingItem: Veiculo | null,
    readOnly = false,
  ) => {
    const result = await ui.windows.open<true, VeiculosUpsertProps>({
      component: VeiculosUpsert,
      props: { editingItem, readOnly },
      title: readOnly
        ? "Visualizar Veículo"
        : editingItem
          ? "Editar Veículo"
          : "Novo Veículo",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: editingItem
          ? "Veículo atualizado com sucesso."
          : "Veículo criado com sucesso.",
      });
    }
  };

  const deleteVeiculo = async (item: Veiculo) => {
    const result = await ui.windows.confirm({
      title: "Excluir Veículo",
      description: `Deseja realmente excluir o veículo ${item.placa}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    await veiculosApi.delete(item.id);
    await invalidate();
    ui.feedback.notify({ type: "success", title: "Veículo excluído com sucesso." });
  };

  return (
    <>
      <VeiculosList
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
        onDelete={deleteVeiculo}
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

export * from "./types";
