"use client";
import { ITENS_POR_PAGINA } from "@/config/paginacao";

import { avisoTituloExterno } from "../origem";
import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ContasPagarList } from "./list";
import { ContasPagarUpsertForm, type ContasPagarUpsertProps } from "./upsert";
import { ContasPagar } from "./types";
import {
  BaixaParcelaWindow,
  type BaixaParcelaWindowProps,
} from "../components/baixa-parcela-dialog";
import { filtrosAvancadosParaApi } from "../components/filtros-avancados";
import { FILTROS_TITULOS_VAZIOS, TODOS, type FiltrosTitulos } from "../components/filtros-titulos";
import { useFeatureList } from "@/hooks/use-feature-list";
import { contasPagarApi } from "@/api/financeiro";
import { contraparteParaApi } from "../components/documento-filtro";
import { useUi } from "@/ui/imperative";

export * from "./types";

export interface ContasPagarFeatureProps {
  // Trava a listagem nas parcelas de uma Compra (mercadoria e frete).
  compraId?: number;
}

export function ContasPagarFeature({ compraId }: ContasPagarFeatureProps = {}) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<ContasPagar>();
  const [filtros, setFiltros] = React.useState<FiltrosTitulos>(FILTROS_TITULOS_VAZIOS);
  const { status, origem, documento, avancados } = filtros;
  const { setPage } = list;
  const { data, isLoading } = useQuery({
    queryKey: ["contas-pagar", list.deferredSearch, status, origem, documento, avancados, compraId, list.page, list.ordenacao],
    queryFn: async () => {
      const res = await contasPagarApi.list({
        search: list.deferredSearch.trim() || undefined,
        ordenarPor: list.ordenacao?.campo,
        direcao: list.ordenacao?.direcao,
        status: status === TODOS ? undefined : status,
        origemTipo: documento?.origemTipo ?? (origem === TODOS ? undefined : origem),
        origemId: documento?.origemId ?? undefined,
        compraId,
        numeroDocumento: documento?.numeroDocumento,
        ...contraparteParaApi(documento),
        ...filtrosAvancadosParaApi(avancados),
        page: list.page,
        pageSize: ITENS_POR_PAGINA,
      });
      return {
        itens: res?.itens ?? [],
        totalPages: res?.totalDePaginas ?? 1,
        totalItems: res?.totalDeItens ?? 0,
      };
    },
  });

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["contas-pagar"] });
  };

  const openEditor = async (item: ContasPagar | null, readOnly = false) => {
    // Título gerado por outro documento só se visualiza; o aviso aparece apenas ao tentar cancelar (openDelete).
    if (item && item.origemTipo !== "MANUAL") readOnly = true;
    try {
      const editingItem = item ? await contasPagarApi.getById(item.id) : null;
      const result = await ui.windows.open<true, ContasPagarUpsertProps>({
        component: ContasPagarUpsertForm,
        props: {
          editingItem,
          readOnly,
        },
        title: readOnly
          ? "Detalhes da Conta a Pagar"
          : editingItem
            ? "Editar Conta a Pagar"
            : "Nova Conta a Pagar",
        size: "large",
      });
      if (result.status === "confirmed") {
        await invalidate();
        ui.feedback.notify({
          type: "success",
          title: editingItem
            ? "Parcela atualizada com sucesso."
            : "Conta criada com sucesso.",
        });
      }
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle:
          "Não foi possível carregar os detalhes da conta a pagar.",
      });
    }
  };

  const openDelete = async (item: ContasPagar) => {
    if (item.origemTipo !== "MANUAL") {
      ui.feedback.notify({ type: "warning", title: avisoTituloExterno(item.origemTipo, item.origemId) });
      return;
    }
    const result = await ui.windows.confirm({
      title: "Excluir Conta a Pagar",
      description: item.totalParcelas > 1
        ? `Excluir o documento ${item.numeroDocumento} - ${item.descricao}? As ${item.totalParcelas} parcelas serão excluídas. Não dá para desfazer.`
        : `Excluir o documento ${item.numeroDocumento} - ${item.descricao}? Não dá para desfazer.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await contasPagarApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: item.totalParcelas > 1 ? "Contas excluídas com sucesso." : "Conta excluída com sucesso.",
      });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir a conta a pagar.",
      });
    }
  };

  async function openBaixa(item: ContasPagar, isEstorno = false) {
    const result = await ui.windows.open<true, BaixaParcelaWindowProps>({
      component: BaixaParcelaWindow,
      props: {
        contaId: item.id,
        parcela: {
          numeroParcela: item.numeroParcela,
          valorParcela: item.valor,
          valorPagoOuRecebido: item.valorPago,
          status: item.status,
        },
        tipo: "PAGAR",
        isEstorno,
      },
      title: isEstorno ? "Estornar Baixa" : "Baixar Parcela",
      description: `${isEstorno ? "Informe o valor a estornar" : "Informe o valor da baixa"} da parcela ${item.numeroParcela}/${item.totalParcelas} do documento ${item.numeroDocumento}.`,
      size: "small",
    });
    if (result.status === "confirmed") await invalidate();
  }

  return (
    <ContasPagarList
      items={data?.itens ?? []}
      loading={isLoading}
      searchTerm={list.searchTerm}
      page={list.page}
      totalPages={data?.totalPages ?? 1}
      totalItems={data?.totalItems ?? 0}
      onSearchChange={list.handleSearchChange}
      onAdd={() => openEditor(null)}
      onEdit={(item) => openEditor(item)}
      onView={(item) => openEditor(item, true)}
      onDelete={openDelete}
      onPageChange={list.setPage}
        ordenacao={list.ordenacao}
        onOrdenacaoChange={list.alterarOrdenacao}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
      filtros={filtros}
      compraId={compraId}
      onFiltrosChange={(parcial) => { setFiltros((f) => ({ ...f, ...parcial })); setPage(1); }}
      onBaixa={(item) => openBaixa(item)}
      onEstorno={(item) => openBaixa(item, true)}
    />
  );
}

export default ContasPagarFeature;
