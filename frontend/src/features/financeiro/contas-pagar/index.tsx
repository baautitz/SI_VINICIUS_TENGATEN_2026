"use client";

import React, { useState } from "react";
import { ContasPagarList } from "./list";
import { ContasPagarUpsertForm } from "./upsert-form";
import { ContasPagar, ContasPagarParcela } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { BaixaParcelaDialog } from "../components/baixa-parcela-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { contasPagarApi } from "@/api/financeiro";

export * from "./types";

export function ContasPagarFeature() {
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
  } = useFeatureOrchestrator<ContasPagar>({
    queryKey: "contas-pagar",
    initialSearchTerm: "",
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await contasPagarApi.list(
        searchTerm || undefined,
        page,
        pageSize,
      );
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };
      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    fetchById: async (id) => {
      return await contasPagarApi.getById(id as number);
    },
    deleteItem: async (item) => {
      await contasPagarApi.delete(item.id);
    },
  });

  const handleBaixa = (contaId: number, parcela: ContasPagarParcela) => {
    setBaixaContaId(contaId);
    setBaixaParcela({
      numeroParcela: parcela.numeroParcela,
      valorParcela: parcela.valorParcela,
      valorPagoOuRecebido: parcela.valorPago,
      status: parcela.status,
    });
    setBaixaOpen(true);
  };

  const handleViewAction = (item: ContasPagar) => {
    listProps.onView(item);
  };

  const handleEditWrapper = (item: ContasPagar) => {
    if (item.status === "PAGO" || item.status === "CANCELADO") {
      listProps.onView(item);
    } else {
      listProps.onEdit(item);
    }
  };

  return (
    <>
      <ContasPagarList
        {...listProps}
        onAdd={listProps.onAdd}
        onEdit={handleEditWrapper}
        onView={handleViewAction}
        onBaixa={handleBaixa}
      />

      {list.isUpsertOpen && (
        <ContasPagarUpsertForm
          key={list.editingItem?.id ?? "new"}
          {...upsertProps}
        />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Conta a Pagar"
        description={
          <p>
            Deseja realmente excluir a conta a pagar{" "}
            <strong>
              #{list.itemToDelete?.id} - {list.itemToDelete?.descricao}
            </strong>
            ? Esta ação não poderá ser desfeita.
          </p>
        }
      />

      {baixaOpen && baixaContaId && baixaParcela && (
        <BaixaParcelaDialog
          key={`${baixaContaId}-${baixaParcela.numeroParcela}`}
          open={baixaOpen}
          onOpenChange={setBaixaOpen}
          contaId={baixaContaId}
          parcela={baixaParcela}
          tipo="PAGAR"
          onSuccess={() => {
            // Recarrega listagens e detalhes ativos invalidando queryKey
          }}
        />
      )}
    </>
  );
}
export default ContasPagarFeature;
