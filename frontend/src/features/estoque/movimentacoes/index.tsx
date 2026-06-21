"use client";

import React, { useState } from "react";
import { MovimentacoesList } from "./list";
import { MovimentacoesUpsertForm } from "./upsert-form";
import { MovimentacaoEstoque } from "./types";
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
import { Kbd, KbdGroup } from "@/components/ui/kbd";

export * from "./types";

export function MovimentacoesFeature() {
  const queryClient = useQueryClient();
  const [actionItem, setActionItem] = useState<MovimentacaoEstoque | null>(
    null,
  );
  const [actionType, setActionType] = useState<"CONFIRM" | "CANCEL" | null>(
    null,
  );

  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<MovimentacaoEstoque>({
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
    fetchById: async (id) => {
      return await estoqueApi.getById(id as number);
    },
    deleteItem: async (item) => {
      await estoqueApi.delete(item.id);
    },
  });

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

  const handleConfirmAction = (item: MovimentacaoEstoque) => {
    setActionItem(item);
    setActionType("CONFIRM");
  };

  const handleCancelAction = (item: MovimentacaoEstoque) => {
    setActionItem(item);
    setActionType("CANCEL");
  };

  const handleViewAction = (item: MovimentacaoEstoque) => {
    listProps.onView(item);
  };

  const handleAddWrapper = () => {
    listProps.onAdd();
  };

  const handleEditWrapper = (item: MovimentacaoEstoque) => {
    if (item.status === "RASCUNHO") {
      listProps.onEdit(item);
    } else {
      listProps.onView(item);
    }
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
        <MovimentacoesUpsertForm
          key={list.editingItem?.id ?? "new"}
          {...upsertProps}
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
        <DialogContent
          className="max-w-md"
          onKeyDown={(e) => {
            if (e.altKey && e.key === "Enter") {
              e.preventDefault();
              e.stopPropagation();
              if (actionItem) {
                if (actionType === "CONFIRM") {
                  confirmMutation.mutate(actionItem.id);
                } else {
                  cancelMutation.mutate(actionItem.id);
                }
              }
              setActionItem(null);
              setActionType(null);
            }
          }}
        >
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

          <DialogFooter className="mt-4 flex justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={confirmMutation.isPending || cancelMutation.isPending}
              onClick={() => {
                setActionItem(null);
                setActionType(null);
              }}
            >
              Cancelar <Kbd>Esc</Kbd>
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
                setActionItem(null);
                setActionType(null);
              }}
            >
              {confirmMutation.isPending || cancelMutation.isPending ? (
                "Processando..."
              ) : actionType === "CONFIRM" ? (
                <span className="flex items-center gap-2">
                  Efetivar
                  <KbdGroup>
                    <Kbd>Alt</Kbd>
                    <Kbd>Enter</Kbd>
                  </KbdGroup>
                </span>
              ) : (
                <span className="flex items-center gap-2">
                  Confirmar Estorno
                  <KbdGroup>
                    <Kbd>Alt</Kbd>
                    <Kbd>Enter</Kbd>
                  </KbdGroup>
                </span>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
