"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { VendasList } from "./list";
import {
  DevolucaoTipoWindow,
  DevolucaoWindow,
  type DevolucaoWindowProps,
  type TipoDevolucao,
} from "./devolucao";
import { VendasUpsertForm, type VendasUpsertProps } from "./upsert";
import type { CriarDevolucaoValues, Venda } from "./types";
import { useFeatureList } from "@/hooks/use-feature-list";
import { vendasApi } from "@/api/vendas";
import { useUi } from "@/ui/imperative";

export * from "./types";

export function VendasFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Venda>();
  const { data, isLoading } = useQuery({
    queryKey: ["vendas", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await vendasApi.list(
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
      queryClient.invalidateQueries({ queryKey: ["vendas"] }),
      queryClient.invalidateQueries({ queryKey: ["skus"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
      queryClient.invalidateQueries({ queryKey: ["contas-receber"] }),
      queryClient.invalidateQueries({ queryKey: ["contas-pagar"] }),
      queryClient.invalidateQueries({ queryKey: ["movimentacoes"] }),
    ]);
  };

  // Backend nao possui edicao de venda: venda existente abre sempre em leitura.
  const openVenda = async (item: Venda | null, readOnly = !!item) => {
    const result = await ui.windows.open<true, VendasUpsertProps>({
      component: VendasUpsertForm,
      props: { editingItem: item, readOnly },
      title: item ? "Detalhes da Venda" : "Nova venda",
      size: "full",
    });

    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: "Venda registrada com sucesso.",
      });
    }
  };

  // Também chamada pela tecla Delete na linha: ignora venda já totalmente devolvida.
  const returnVenda = async (item: Venda) => {
    if (item.statusDevolucao === "TOTAL") {
      ui.feedback.notify({ type: "info", title: "Venda já foi totalmente devolvida." });
      return;
    }
    const tipo = await ui.windows.open<TipoDevolucao, Record<string, never>>({
      component: DevolucaoTipoWindow,
      props: {},
      title: `Devolução da Venda #${item.id}`,
      size: "small",
    });
    if (tipo.status !== "confirmed") return;

    const result = await ui.windows.open<CriarDevolucaoValues, DevolucaoWindowProps>({
      component: DevolucaoWindow,
      props: { venda: item, total: tipo.value === "total" },
      title: `Devolver Itens da Venda #${item.id}`,
      size: "large",
    });
    if (result.status !== "confirmed") return;

    try {
      await vendasApi.createDevolucao(item.id, result.value);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Devolução registrada com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível registrar a devolução.",
      });
    }
  };

  return (
    <VendasList
      items={data?.itens ?? []}
      loading={isLoading}
      searchTerm={list.searchTerm}
      page={list.page}
      totalPages={data?.totalPages ?? 1}
      totalItems={data?.totalItems ?? 0}
      onSearchChange={list.handleSearchChange}
      onAdd={() => openVenda(null)}
      onEdit={(item) => openVenda(item, true)}
      onView={(item) => openVenda(item, true)}
      onDelete={returnVenda}
      onPageChange={list.setPage}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
    />
  );
}
