"use client";

import React, { useState } from "react";
import { MovimentacoesList } from "./list";
import { MovimentacoesUpsert } from "./upsert";
import { MovimentacaoEstoqueResumo, MovimentacaoEstoque } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { estoqueApi } from "@/api/estoque";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

export * from "./types";

export function MovimentacoesFeature() {
  const queryClient = useQueryClient();
  const [actionItem, setActionItem] =
    useState<MovimentacaoEstoqueResumo | null>(null);
  const [actionType, setActionType] = useState<"CONFIRM" | "CANCEL" | null>(
    null,
  );
  const [viewOnly, setViewOnly] = useState(false);

  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<MovimentacaoEstoqueResumo>({
    queryKey: "movimentacoes",
    initialSearchTerm: "",
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await estoqueApi.list(
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
    deleteItem: async (id) => {
      await estoqueApi.delete(id);
    },
  });

  // Confirm (Efetivar) Mutation
  const confirmMutation = useMutation({
    mutationFn: (id: number) => estoqueApi.confirmar(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["movimentacoes"] });
      toast.success("Movimentação de estoque efetivada com sucesso!");
      setActionItem(null);
      setActionType(null);
    },
    onError: () => {
      setActionItem(null);
      setActionType(null);
    },
  });

  // Cancel (Estornar) Mutation
  const cancelMutation = useMutation({
    mutationFn: (id: number) => estoqueApi.cancelar(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["movimentacoes"] });
      toast.success("Movimentação de estoque estornada com sucesso!");
      setActionItem(null);
      setActionType(null);
    },
    onError: () => {
      setActionItem(null);
      setActionType(null);
    },
  });

  const handleConfirmAction = (item: MovimentacaoEstoqueResumo) => {
    setActionItem(item);
    setActionType("CONFIRM");
  };

  const handleCancelAction = (item: MovimentacaoEstoqueResumo) => {
    setActionItem(item);
    setActionType("CANCEL");
  };

  const handleViewAction = (item: MovimentacaoEstoqueResumo) => {
    setViewOnly(true);
    listProps.onEdit(item);
  };

  const handleAddWrapper = () => {
    setViewOnly(false);
    listProps.onAdd();
  };

  const handleEditWrapper = (item: MovimentacaoEstoqueResumo) => {
    setViewOnly(false);
    listProps.onEdit(item);
  };

  return (
    <>
      <MovimentacoesList
        {...listProps}
        onAdd={handleAddWrapper}
        onEdit={handleEditWrapper}
        onConfirm={handleConfirmAction}
        onCancel={handleCancelAction}
        onView={handleViewAction}
      />

      {list.isUpsertOpen && (
        <MovimentacoesUpsert
          key={list.editingItem?.id ?? "new"}
          {...upsertProps}
          editingItem={
            list.editingItem as unknown as MovimentacaoEstoque | null
          }
          readOnly={viewOnly}
        />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Rascunho de Movimentação"
        description={
          <p>
            Deseja realmente excluir o rascunho de movimentação{" "}
            <strong>#{list.itemToDelete?.id}</strong>? Esta ação não poderá ser
            desfeita.
          </p>
        }
      />

      <Dialog
        open={actionType !== null}
        onOpenChange={(open) => {
          if (!open) {
            setActionItem(null);
            setActionType(null);
          }
        }}
      >
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>
              {actionType === "CONFIRM"
                ? "Efetivar Movimentação?"
                : "Estornar Movimentação?"}
            </DialogTitle>
            <DialogDescription>
              {actionType === "CONFIRM"
                ? `Deseja realmente efetivar a movimentação de estoque #${actionItem?.id}? Isso alterará de forma definitiva o saldo físico dos produtos no catálogo.`
                : `Deseja realmente estornar/cancelar a movimentação #${actionItem?.id}? Isso reverterá o impacto das quantidades no saldo físico dos produtos.`}
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="mt-4 flex gap-2 justify-end">
            <Button
              type="button"
              variant="outline"
              disabled={confirmMutation.isPending || cancelMutation.isPending}
              onClick={() => {
                setActionItem(null);
                setActionType(null);
              }}
            >
              Cancelar
            </Button>
            <Button
              type="button"
              variant={actionType === "CONFIRM" ? "default" : "destructive"}
              disabled={confirmMutation.isPending || cancelMutation.isPending}
              onClick={() => {
                if (actionItem) {
                  if (actionType === "CONFIRM") {
                    confirmMutation.mutate(actionItem.id);
                  } else {
                    cancelMutation.mutate(actionItem.id);
                  }
                }
              }}
            >
              {confirmMutation.isPending || cancelMutation.isPending
                ? "Processando..."
                : actionType === "CONFIRM"
                  ? "Efetivar"
                  : "Confirmar Estorno"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
