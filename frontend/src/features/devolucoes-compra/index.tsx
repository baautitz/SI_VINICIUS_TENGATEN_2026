"use client";
import { ITENS_POR_PAGINA } from "@/config/paginacao";

import { useQuery } from "@tanstack/react-query";
import { Undo2 } from "lucide-react";
import { DevolucoesCompraList } from "./list";
import { DevolucaoCompraDetalheWindow, type DevolucaoCompraDetalheWindowProps } from "./detalhe";
import { useEstornarDevolucaoCompra } from "./estornar";
import { useFeatureList } from "@/hooks/use-feature-list";
import type { DevolucaoCompraResumo } from "@/features/compras/types";
import { comprasApi } from "@/api/compras";
import { useUi } from "@/ui/imperative";

export interface DevolucoesCompraFeatureProps {
  compraId?: number;
}

/** A devolução de compra nasce na tela de Compras (precisa do estoque e dos itens da compra); aqui se consulta e se estorna. */
export function DevolucoesCompraFeature({ compraId }: DevolucoesCompraFeatureProps = {}) {
  const ui = useUi();
  const estornarDevolucao = useEstornarDevolucaoCompra();
  const list = useFeatureList<DevolucaoCompraResumo>();
  const { data, isLoading } = useQuery({
    queryKey: ["devolucoes-compra", list.deferredSearch, compraId, list.page, list.ordenacao],
    queryFn: async () => {
      const res = await comprasApi.searchDevolucoes({
        search: list.deferredSearch.trim() || undefined,
        compraId,
        page: list.page,
        pageSize: ITENS_POR_PAGINA,
        ordenacao: list.ordenacao,
      });
      return {
        itens: res?.itens ?? [],
        totalPages: res?.totalDePaginas ?? 1,
        totalItems: res?.totalDeItens ?? 0,
      };
    },
  });

  const view = async (item: DevolucaoCompraResumo) => {
    await ui.windows.open<true, DevolucaoCompraDetalheWindowProps>({
      component: DevolucaoCompraDetalheWindow,
      props: { devolucaoId: item.id },
      title: `Devolução de compra #${item.id}`,
      size: "medium",
    });
  };

  return (
    <DevolucoesCompraList
      icon={<Undo2 />}
      items={data?.itens ?? []}
      loading={isLoading}
      searchTerm={list.searchTerm}
      page={list.page}
      totalPages={data?.totalPages ?? 1}
      totalItems={data?.totalItems ?? 0}
      onSearchChange={list.handleSearchChange}
      onView={view}
      onEstornar={(item) => void estornarDevolucao(item.id)}
      onPageChange={list.setPage}
      ordenacao={list.ordenacao}
      onOrdenacaoChange={list.alterarOrdenacao}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
    />
  );
}
