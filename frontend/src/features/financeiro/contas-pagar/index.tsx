"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ContasPagarList } from "./list";
import { ContasPagarUpsertForm, type ContasPagarUpsertProps } from "./upsert";
import { ContasPagar, ContasPagarParcela } from "./types";
import {
  BaixaParcelaWindow,
  type BaixaParcelaWindowProps,
} from "../components/baixa-parcela-dialog";
import { useFeatureList } from "@/hooks/use-feature-list";
import { contasPagarApi } from "@/api/financeiro";
import { useUi } from "@/ui/imperative";

export * from "./types";

export function ContasPagarFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<ContasPagar>();
  const { data, isLoading } = useQuery({
    queryKey: ["contas-pagar", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await contasPagarApi.list(
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
    await queryClient.invalidateQueries({ queryKey: ["contas-pagar"] });
  };

  const openEditor = async (item: ContasPagar | null, readOnly = false) => {
    try {
      const editingItem = item ? await contasPagarApi.getById(item.id) : null;
      const result = await ui.windows.open<true, ContasPagarUpsertProps>({
        component: ContasPagarUpsertForm,
        props: {
          editingItem,
          readOnly,
          onBaixa: (contaId, parcela) => openBaixa(contaId, parcela),
          onEstorno: (contaId, parcela) => openBaixa(contaId, parcela, true),
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
            ? "Conta atualizada com sucesso."
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
    const result = await ui.windows.confirm({
      title: "Excluir Conta a Pagar",
      description: `Deseja realmente excluir a conta a pagar #${item.id} - ${item.descricao}? Esta ação não poderá ser desfeita.`,
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

  async function openBaixa(
    contaId: number,
    parcela: ContasPagarParcela,
    isEstorno = false,
  ) {
    const result = await ui.windows.open<true, BaixaParcelaWindowProps>({
      component: BaixaParcelaWindow,
      props: {
        contaId,
        parcela: {
          numeroParcela: parcela.numeroParcela,
          valorParcela: parcela.valorParcela,
          valorPagoOuRecebido: parcela.valorPago,
          status: parcela.status,
        },
        tipo: "PAGAR",
        isEstorno,
      },
      title: isEstorno ? "Estornar Pagamento" : "Registrar Pagamento",
      description: isEstorno
        ? `Informe o valor a estornar para a parcela #${parcela.numeroParcela} da conta #${contaId}.`
        : `Informe o valor pago para a parcela #${parcela.numeroParcela} da conta #${contaId}.`,
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
      onBaixa={(contaId, parcela) => openBaixa(contaId, parcela)}
    />
  );
}

export default ContasPagarFeature;
