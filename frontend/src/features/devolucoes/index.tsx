"use client";

import { useQuery } from "@tanstack/react-query";
import { Undo2 } from "lucide-react";
import { DevolucoesList } from "./list";
import { DevolucaoDetalheWindow, type DevolucaoDetalheWindowProps } from "./detalhe";
import { useEstornarDevolucao } from "./estornar";
import { NovaDevolucaoWindow } from "./nova";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useDevolucaoFlow } from "@/features/vendas/devolucao-flow";
import type { DevolucaoResumo, Venda } from "@/features/vendas/types";
import { vendasApi } from "@/api/vendas";
import { useUi } from "@/ui/imperative";

export interface DevolucoesFeatureProps {
  vendaId?: number;
}

export function DevolucoesFeature({ vendaId }: DevolucoesFeatureProps = {}) {
  const ui = useUi();
  const { realizarDevolucao } = useDevolucaoFlow();
  const estornarDevolucao = useEstornarDevolucao();
  const list = useFeatureList<DevolucaoResumo>();
  const { data, isLoading } = useQuery({
    queryKey: ["devolucoes", list.deferredSearch, vendaId, list.page],
    queryFn: async () => {
      const res = await vendasApi.searchDevolucoes({
        search: list.deferredSearch.trim() || undefined,
        vendaId,
        page: list.page,
        pageSize: 50,
      });
      return {
        itens: res?.itens ?? [],
        totalPages: res?.totalDePaginas ?? 1,
        totalItems: res?.totalDeItens ?? 0,
      };
    },
  });

  const view = async (item: DevolucaoResumo) => {
    await ui.windows.open<true, DevolucaoDetalheWindowProps>({
      component: DevolucaoDetalheWindow,
      props: { devolucaoId: item.id },
      title: `Devolução #${item.id}`,
      size: "medium",
    });
  };

  const add = async () => {
    if (vendaId) {
      const venda = await vendasApi.getById(vendaId);
      if (venda) await realizarDevolucao(venda);
      return;
    }
    const result = await ui.windows.open<Venda, Record<string, never>>({
      component: NovaDevolucaoWindow,
      props: {},
      title: "Nova devolução",
      size: "medium",
    });
    if (result.status === "confirmed") await realizarDevolucao(result.value);
  };

  return (
    <DevolucoesList
      icon={<Undo2 />}
      items={data?.itens ?? []}
      loading={isLoading}
      searchTerm={list.searchTerm}
      page={list.page}
      totalPages={data?.totalPages ?? 1}
      totalItems={data?.totalItems ?? 0}
      onSearchChange={list.handleSearchChange}
      onAdd={add}
      onEdit={view}
      onView={view}
      onEstornar={(item) => void estornarDevolucao(item.id)}
      onDelete={() => {}}
      onPageChange={list.setPage}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
    />
  );
}
