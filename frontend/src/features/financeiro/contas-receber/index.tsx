"use client";

import React, { useState } from "react";
import { ContasReceberList } from "./list";
import { ContasReceberUpsertForm } from "./upsert-form";
import { ContasReceber, ContasReceberParcela } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { BaixaParcelaDialog } from "../components/baixa-parcela-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { contasReceberApi } from "@/api/financeiro";

export * from "./types";

export function ContasReceberFeature() {
  const [baixaOpen, setBaixaOpen] = useState(false);
  const [baixaContaId, setBaixaContaId] = useState<number | null>(null);
  const [baixaParcela, setBaixaParcela] = useState<{
    numeroParcela: number;
    valorParcela: number;
    valorPagoOuRecebido: number;
    status: string;
  } | null>(null);

  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<any>({
    queryKey: "contas-receber",
    initialSearchTerm: "",
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await contasReceberApi.list(
        searchTerm || undefined,
        page,
        pageSize
      );
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };
      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    fetchById: async (id) => {
      return await contasReceberApi.getById(id as number);
    },
    deleteItem: async (item) => {
      await contasReceberApi.delete(item.id);
    },
  });

  const handleBaixa = (contaId: number, parcela: ContasReceberParcela) => {
    setBaixaContaId(contaId);
    setBaixaParcela({
      numeroParcela: parcela.numeroParcela,
      valorParcela: parcela.valorParcela,
      valorPagoOuRecebido: parcela.valorRecebido,
      status: parcela.status,
    });
    setBaixaOpen(true);
  };

  const handleViewAction = (item: ContasReceber) => {
    listProps.onView(item);
  };

  const handleEditWrapper = (item: ContasReceber) => {
    if (item.status === "PAGO" || item.status === "CANCELADO") {
      listProps.onView(item);
    } else {
      listProps.onEdit(item);
    }
  };

  return (
    <>
      <ContasReceberList
        {...listProps}
        onAdd={listProps.onAdd}
        onEdit={handleEditWrapper}
        onView={handleViewAction}
        onBaixa={handleBaixa}
      />

      {list.isUpsertOpen && (
        <ContasReceberUpsertForm
          key={list.editingItem?.id ?? "new"}
          {...upsertProps}
        />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Conta a Receber"
        description={
          <p>
            Deseja realmente excluir a conta a receber{" "}
            <strong>#{list.itemToDelete?.id} - {list.itemToDelete?.descricao}</strong>? Esta ação não poderá ser desfeita.
          </p>
        }
      />

      {baixaOpen && baixaContaId && baixaParcela && (
        <BaixaParcelaDialog
          open={baixaOpen}
          onOpenChange={setBaixaOpen}
          contaId={baixaContaId}
          parcela={baixaParcela}
          tipo="RECEBER"
          onSuccess={() => {
            // Recarrega listagens e detalhes ativos invalidando queryKey
          }}
        />
      )}
    </>
  );
}
export default ContasReceberFeature;
