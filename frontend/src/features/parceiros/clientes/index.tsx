"use client";

import React from "react";
import { ClientesList } from "./list";
import { ClientesUpsert } from "./upsert";
import { Cliente } from "./types";
import { clientesApi } from "@/api/parceiros";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";
import type { ReactNode } from "react";
import { Users } from "lucide-react";

interface ClientesFeatureProps {
  /** O mesmo ícone é usado no cabeçalho da página, seleção modal e titlebar. */
  icon?: ReactNode;
  selectionMode?: boolean;
  onSelect?: (cliente: Cliente) => void;
  initialSearchTerm?: string;
}

export function ClientesFeature({
  icon,
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: ClientesFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Cliente>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["clientes", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await clientesApi.list(
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
    await queryClient.invalidateQueries({ queryKey: ["clientes"] });
  };

  const openCreate = async () => {
    const result = await ui.windows.open<true, ClientesUpsertProps>({
      component: ClientesUpsert,
      props: { editingItem: null },
      title: "Novo Cliente",
      icon: icon ?? <Users />,
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Cliente criado com sucesso." });
    }
  };

  const openEdit = async (item: Cliente, readOnly = false) => {
    const result = await ui.windows.open<true, ClientesUpsertProps>({
      component: ClientesUpsert,
      props: { editingItem: item, readOnly },
      title: readOnly ? "Visualizar Cliente" : "Editar Cliente",
      icon: icon ?? <Users />,
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: "Cliente atualizado com sucesso.",
      });
    }
  };

  const openView = async (item: Cliente) => {
    await openEdit(item, true);
  };

  const selectCliente = async (item: Cliente) => {
    await onSelect?.(item);
  };

  const deleteCliente = async (item: Cliente) => {
    const result = await ui.windows.confirm({
      title: "Excluir Cliente",
      description: (
        <p>
          Deseja realmente excluir o cliente <strong>{item.nomeRazaoSocial}</strong>? Esta ação não poderá ser desfeita.
        </p>
      ),
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    await clientesApi.delete(item.id);
    await invalidate();
    ui.feedback.notify({ type: "success", title: "Cliente excluído com sucesso." });
  };

  return (
    <>
      <ClientesList
        icon={icon}
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
        onDelete={deleteCliente}
        onPageChange={list.setPage}
        rowSelection={list.rowSelection}
        onRowSelectionChange={list.setRowSelection}
        selectAllAcrossPages={list.selectAllAcrossPages}
        onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
        selectionMode={selectionMode}
        onSelect={selectCliente}
      />
    </>
  );
}

export interface ClientesUpsertProps {
  editingItem: Cliente | null;
  readOnly?: boolean;
}
