"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ContasReceberList } from "./list";
import {
  ContasReceberUpsertForm,
  type ContasReceberUpsertProps,
} from "./upsert";
import { ContasReceber, ContasReceberParcela } from "./types";
import {
  BaixaParcelaWindow,
  type BaixaParcelaWindowProps,
} from "../components/baixa-parcela-dialog";
import { useFeatureList } from "@/hooks/use-feature-list";
import { contasReceberApi } from "@/api/financeiro";
import { useUi } from "@/ui/imperative";

export * from "./types";

export function ContasReceberFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<ContasReceber>();
  const { data, isLoading } = useQuery({
    queryKey: ["contas-receber", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await contasReceberApi.list(
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
    await queryClient.invalidateQueries({ queryKey: ["contas-receber"] });
  };

  const openEditor = async (item: ContasReceber | null, readOnly = false) => {
    try {
      const editingItem = item ? await contasReceberApi.getById(item.id) : null;
      const result = await ui.windows.open<true, ContasReceberUpsertProps>({
        component: ContasReceberUpsertForm,
        props: {
          editingItem,
          readOnly,
          onBaixa: (contaId, parcela) => openBaixa(contaId, parcela),
          onEstorno: (contaId, parcela) => openBaixa(contaId, parcela, true),
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
            ? "Conta atualizada com sucesso."
            : "Conta criada com sucesso.",
        });
      }
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível carregar os detalhes da conta a receber.",
      });
    }
  };

  const openDelete = async (item: ContasReceber) => {
    const result = await ui.windows.confirm({
      title: "Excluir Conta a Receber",
      description: `Deseja realmente excluir a conta a receber #${item.id} - ${item.descricao}? Esta ação não poderá ser desfeita.`,
      confirmLabel: "Excluir",
      confirmVariant: "destructive",
    });
    if (!result) return;
    try {
      await contasReceberApi.delete(item.id);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Conta excluída com sucesso." });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível excluir a conta a receber.",
      });
    }
  };

  async function openBaixa(
    contaId: number,
    parcela: ContasReceberParcela,
    isEstorno = false,
  ) {
    const result = await ui.windows.open<true, BaixaParcelaWindowProps>({
      component: BaixaParcelaWindow,
      props: {
        contaId,
        parcela: {
          numeroParcela: parcela.numeroParcela,
          valorParcela: parcela.valorParcela,
          valorPagoOuRecebido: parcela.valorRecebido,
          status: parcela.status,
        },
        tipo: "RECEBER",
        isEstorno,
      },
      title: isEstorno ? "Estornar Recebimento" : "Registrar Recebimento",
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
      onBaixa={(contaId, parcela) => openBaixa(contaId, parcela)}
    />
  );
}

export default ContasReceberFeature;
