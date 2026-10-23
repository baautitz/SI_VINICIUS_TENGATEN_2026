"use client";

import { avisoTituloExterno } from "../origem";
import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ContasReceberList, TODOS } from "./list";
import { ContasReceberUpsertForm, type ContasReceberUpsertProps } from "./upsert";
import { ContasReceber } from "./types";
import {
  BaixaParcelaWindow,
  type BaixaParcelaWindowProps,
} from "../components/baixa-parcela-dialog";
import { FILTROS_AVANCADOS_VAZIOS, filtrosAvancadosParaApi, type FiltrosAvancados } from "../components/filtros-avancados";
import { useFeatureList } from "@/hooks/use-feature-list";
import { contasReceberApi } from "@/api/financeiro";
import { useUi } from "@/ui/imperative";

export * from "./types";

export function ContasReceberFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<ContasReceber>();
  const [status, setStatus] = React.useState(TODOS);
  const [origem, setOrigem] = React.useState(TODOS);
  const [numeroDocumento, setNumeroDocumento] = React.useState("");
  const [avancados, setAvancados] = React.useState<FiltrosAvancados>(FILTROS_AVANCADOS_VAZIOS);
  const { setPage } = list;
  const { data, isLoading } = useQuery({
    queryKey: ["contas-receber", list.deferredSearch, status, origem, numeroDocumento, avancados, list.page],
    queryFn: async () => {
      const res = await contasReceberApi.list({
        search: list.deferredSearch.trim() || undefined,
        status: status === TODOS ? undefined : status,
        origemTipo: origem === TODOS ? undefined : origem,
        numeroDocumento: numeroDocumento || undefined,
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
    await queryClient.invalidateQueries({ queryKey: ["contas-receber"] });
  };

  const openEditor = async (item: ContasReceber | null, readOnly = false) => {
    // Título gerado por outro documento só se visualiza; o aviso aparece apenas ao tentar cancelar (openDelete).
    if (item && item.origemTipo !== "MANUAL") readOnly = true;
    try {
      const editingItem = item ? await contasReceberApi.getById(item.id) : null;
      const result = await ui.windows.open<true, ContasReceberUpsertProps>({
        component: ContasReceberUpsertForm,
        props: {
          editingItem,
          readOnly,
        },
        title: readOnly
          ? "Detalhes da Conta a Receber"
          : editingItem
            ? "Editar Conta a Receber"
            : "Nova Conta a Receber",
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
          "Não foi possível carregar os detalhes da conta a receber.",
      });
    }
  };

  const openDelete = async (item: ContasReceber) => {
    if (item.origemTipo !== "MANUAL") {
      ui.feedback.notify({ type: "warning", title: avisoTituloExterno(item.origemTipo, item.origemId) });
      return;
    }
    const result = await ui.windows.confirm({
      title: "Excluir Conta a Receber",
      description: `Deseja realmente excluir a parcela ${item.numeroParcela}/${item.totalParcelas} do documento ${item.numeroDocumento} - ${item.descricao}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await contasReceberApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: "Conta excluída com sucesso.",
      });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir a conta a receber.",
      });
    }
  };

  async function openBaixa(item: ContasReceber, isEstorno = false) {
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
        tipo: "RECEBER",
        isEstorno,
      },
      title: isEstorno ? "Estornar Recebimento" : "Registrar Recebimento",
      description: `${isEstorno ? "Informe o valor a estornar" : "Informe o valor recebido"} da parcela ${item.numeroParcela}/${item.totalParcelas} do documento ${item.numeroDocumento}.`,
      size: "small",
    });
    if (result.status === "confirmed") await invalidate();
  }

  return (
    <ContasReceberList
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
      numeroDocumento={numeroDocumento}
      avancados={avancados}
      onAvancadosChange={(v) => { setAvancados(v); setPage(1); }}
      onStatusChange={(v) => { setStatus(v); setPage(1); }}
      onOrigemChange={(v) => { setOrigem(v); setPage(1); }}
      onDocumentoChange={(v) => { setNumeroDocumento(v); setPage(1); }}
      onBaixa={(item) => openBaixa(item)}
      onEstorno={(item) => openBaixa(item, true)}
    />
  );
}

export default ContasReceberFeature;
