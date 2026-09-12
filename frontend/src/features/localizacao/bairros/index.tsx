"use client";

import { BairrosList } from "./list";
import { BairrosUpsert, type BairrosUpsertProps } from "./upsert";
import { Bairro } from "./types";
import { useFeatureList } from "@/hooks/use-feature-list";
import { bairrosApi } from "@/api/localizacao";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";

export * from "./types";

interface BairrosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (bairro: Bairro) => void;
  initialSearchTerm?: string;
}

export function BairrosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: BairrosFeatureProps) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Bairro>({ initialSearchTerm });
  const { data, isLoading } = useQuery({
    queryKey: ["bairros", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await bairrosApi.list(list.deferredSearch.trim() || undefined, list.page, 50);
      return { itens: res?.itens ?? [], totalPages: res?.totalDePaginas ?? 1, totalItems: res?.totalDeItens ?? 0 };
    },
  });
  const invalidate = async () => queryClient.invalidateQueries({ queryKey: ["bairros"] });
  const openUpsert = async (editingItem: Bairro | null, readOnly = false) => {
    const result = await ui.windows.open<true, BairrosUpsertProps>({
      component: BairrosUpsert,
      props: { editingItem, readOnly },
      title: readOnly ? "Visualizar Bairro" : editingItem ? "Editar Bairro" : "Novo Bairro",
    });
    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({ type: "success", title: editingItem ? "Bairro atualizado com sucesso." : "Bairro criado com sucesso." });
    }
  };
  const deleteBairro = async (item: Bairro) => {
    const result = await ui.windows.confirm({
      title: "Excluir Bairro",
      description: <>Deseja realmente excluir o bairro <strong>{item.bairro}</strong> de <strong>{item.cidade.cidade}</strong> ({item.cidade.estado.uf})? Esta ação não poderá ser desfeita.</>,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await bairrosApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Bairro excluído com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir o bairro.",
      });
    }
  };

  return (
    <>
      <BairrosList
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
        onDelete={deleteBairro}
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
