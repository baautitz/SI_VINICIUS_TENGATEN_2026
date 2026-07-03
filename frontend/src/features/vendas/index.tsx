"use client";

import React from "react";
import { VendasList } from "./list";
import { VendasUpsertForm } from "./upsert";
import type { Venda } from "./types";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { vendasApi } from "@/api/vendas";
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
import { Textarea } from "@/components/ui/textarea";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import { cn } from "@/lib/utils";

export * from "./types";

export function VendasFeature() {
  const [cancelOpen, setCancelOpen] = React.useState(false);
  const [vendaToCancel, setVendaToCancel] = React.useState<Venda | null>(null);
  const [motivo, setMotivo] = React.useState("");
  const [motivoError, setMotivoError] = React.useState("");
  const [canceling, setCanceling] = React.useState(false);

  const {
    listProps,
    upsertProps,
    featureList: list,
  } = useFeatureOrchestrator<Venda>({
    queryKey: "vendas",
    initialSearchTerm: "",
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await vendasApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    fetchById: async (id) => {
      return await vendasApi.getById(id as number);
    },
    deleteItem: async () => {},
    additionalKeysToInvalidate: [
      ["skus"],
      ["produtos"],
      ["contas-receber"],
      ["movimentacoes"],
    ],
  });

  const handleAddWrapper = () => {
    listProps.onAdd();
  };

  const handleViewWrapper = (item: Venda) => {
    listProps.onView(item);
  };

  const handleConfirmCancel = async () => {
    if (!vendaToCancel) return;
    if (!motivo.trim()) {
      setMotivoError("Motivo do cancelamento é obrigatório.");
      return;
    }
    if (motivo.trim().length < 5) {
      setMotivoError("O motivo deve ter pelo menos 5 caracteres.");
      return;
    }

    try {
      setCanceling(true);
      await vendasApi.cancel(vendaToCancel.id, motivo);
      toast.success("Venda cancelada com sucesso!");
      setCancelOpen(false);
      setVendaToCancel(null);
      upsertProps.onSuccess();
    } catch {
    } finally {
      setCanceling(false);
    }
  };

  return (
    <>
      <VendasList
        {...listProps}
        onAdd={handleAddWrapper}
        onView={handleViewWrapper}
        onDelete={(item) => {
          setVendaToCancel(item);
          setMotivo("");
          setMotivoError("");
          setCancelOpen(true);
        }}
      />

      {list.isUpsertOpen && (
        <VendasUpsertForm
          key={list.editingItem?.id ?? "new"}
          open={list.isUpsertOpen}
          editingItem={upsertProps.editingItem}
          loading={upsertProps.loading}
          onClose={upsertProps.onClose}
          onSuccess={() => {
            upsertProps.onSuccess();
            toast.success(
              list.editingItem
                ? "Venda consultada com sucesso!"
                : "Venda registrada com sucesso!",
            );
          }}
          readOnly={list.readOnly}
        />
      )}

      <Dialog
        open={cancelOpen}
        onOpenChange={(open) => {
          if (!open) {
            setCancelOpen(false);
            setVendaToCancel(null);
          }
        }}
      >
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Cancelar Venda</DialogTitle>
            <DialogDescription>
              Tem certeza que deseja cancelar a venda{" "}
              <strong>#{vendaToCancel?.id}</strong>?
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-3 py-3">
            <p className="text-destructive text-xs font-semibold">
              Esta ação reverterá as movimentações de estoque físicas dos itens
              e excluirá a conta a receber gerada.
            </p>
            <Field data-invalid={!!motivoError}>
              <FieldLabel htmlFor="motivo-cancelamento">
                Motivo do Cancelamento
              </FieldLabel>
              <Textarea
                id="motivo-cancelamento"
                value={motivo}
                onChange={(e) => {
                  setMotivo(e.target.value);
                  if (e.target.value.trim().length >= 5) setMotivoError("");
                }}
                placeholder="Informe o motivo (mínimo de 5 caracteres)..."
                rows={3}
                maxLength={500}
                className={cn(
                  motivoError &&
                    "border-destructive focus-visible:ring-destructive",
                )}
              />
              {motivoError && <FieldError>{motivoError}</FieldError>}
            </Field>
          </div>

          <DialogFooter className="gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setCancelOpen(false);
                setVendaToCancel(null);
              }}
              disabled={canceling}
            >
              Ceancelar
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={handleConfirmCancel}
              disabled={canceling}
            >
              {canceling ? "Cancelando..." : "Confirmar Cancelamento"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
