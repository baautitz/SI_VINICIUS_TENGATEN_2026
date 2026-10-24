"use client";

import { avisoTituloExterno } from "../origem";
import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ContasPagarList, TODOS } from "./list";
import { ContasPagarUpsertForm, type ContasPagarUpsertProps } from "./upsert";
import { ContasPagar } from "./types";
import {
  BaixaParcelaWindow,
  type BaixaParcelaWindowProps,
} from "../components/baixa-parcela-dialog";
import { FILTROS_AVANCADOS_VAZIOS, filtrosAvancadosParaApi, type FiltrosAvancados } from "../components/filtros-avancados";
import type { DocumentoFiltro } from "../components/documento-filtro";
import { useFeatureList } from "@/hooks/use-feature-list";
import { contasPagarApi } from "@/api/financeiro";
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
  const [status, setStatus] = React.useState(TODOS);
  const [origem, setOrigem] = React.useState(TODOS);
  const [documento, setDocumento] = React.useState<DocumentoFiltro | null>(null);
  const [avancados, setAvancados] = React.useState<FiltrosAvancados>(FILTROS_AVANCADOS_VAZIOS);
  const { setPage } = list;
  const { data, isLoading } = useQuery({
    queryKey: ["contas-pagar", list.deferredSearch, status, origem, documento, avancados, compraId, list.page],
    queryFn: async () => {
      const res = await contasPagarApi.list({
        search: list.deferredSearch.trim() || undefined,
        status: status === TODOS ? undefined : status,
        origemTipo: documento?.origemTipo ?? (origem === TODOS ? undefined : origem),
        origemId: documento?.origemId ?? undefined,
        compraId,
        numeroDocumento: documento?.numeroDocumento,
        ...filtrosAvancadosParaApi(avancados),
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
      description: `Deseja realmente excluir a parcela ${item.numeroParcela}/${item.totalParcelas} do documento ${item.numeroDocumento} - ${item.descricao}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await contasPagarApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: "Conta excluída com sucesso.",
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
      title: isEstorno ? "Estornar Pagamento" : "Registrar Pagamento",
      description: `${isEstorno ? "Informe o valor a estornar" : "Informe o valor pago"} da parcela ${item.numeroParcela}/${item.totalParcelas} do documento ${item.numeroDocumento}.`,
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
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
      status={status}
      origem={origem}
      documento={documento}
      compraTravada={compraId !== undefined}
      avancados={avancados}
      onAvancadosChange={(v) => { setAvancados(v); setPage(1); }}
      onStatusChange={(v) => { setStatus(v); setPage(1); }}
      onOrigemChange={(v) => { setOrigem(v); setPage(1); }}
      onDocumentoChange={(v) => { setDocumento(v); setPage(1); }}
      onBaixa={(item) => openBaixa(item)}
      onEstorno={(item) => openBaixa(item, true)}
    />
  );
}

export default ContasPagarFeature;
