"use client";

import React, { useState, useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Kbd, KbdGroup } from "@/components/ui/kbd";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import { NumberInput } from "@/components/ui/number-input";
import { contasPagarApi, contasReceberApi } from "@/api/financeiro";

interface BaixaParcelaDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  contaId: number;
  parcela: {
    numeroParcela: number;
    valorParcela: number;
    valorPagoOuRecebido: number;
    status: string;
  } | null;
  tipo: "PAGAR" | "RECEBER";
  onSuccess: () => void;
}

export function BaixaParcelaDialog({
  open,
  onOpenChange,
  contaId,
  parcela,
  tipo,
  onSuccess,
}: BaixaParcelaDialogProps) {
  const queryClient = useQueryClient();
  const [valorBaixa, setValorBaixa] = useState<number>(0);
  const [error, setError] = useState<string | null>(null);

  const saldoRestante = parcela
    ? Math.max(0, parcela.valorParcela - parcela.valorPagoOuRecebido)
    : 0;

  useEffect(() => {
    if (parcela) {
      setValorBaixa(saldoRestante);
      setError(null);
    }
  }, [parcela, saldoRestante]);

  const mutation = useMutation({
    mutationFn: async () => {
      if (!parcela) return;
      if (tipo === "PAGAR") {
        await contasPagarApi.registrarPagamento(
          contaId,
          parcela.numeroParcela,
          valorBaixa
        );
      } else {
        await contasReceberApi.registrarRecebimento(
          contaId,
          parcela.numeroParcela,
          valorBaixa
        );
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [tipo === "PAGAR" ? "contas-pagar" : "contas-receber"] });
      onSuccess();
      onOpenChange(false);
    },
  });

  const handleConfirm = () => {
    setError(null);
    if (valorBaixa <= 0) {
      setError("O valor a baixar deve ser maior que zero.");
      return;
    }
    if (valorBaixa > saldoRestante + 0.01) {
      setError(`O valor não pode exceder o saldo restante (R$ ${saldoRestante.toFixed(2)}).`);
      return;
    }
    mutation.mutate();
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-w-md"
        onKeyDown={(e) => {
          if (e.altKey && e.key === "Enter") {
            e.preventDefault();
            e.stopPropagation();
            handleConfirm();
          }
        }}
      >
        <DialogHeader>
          <DialogTitle>
            {tipo === "PAGAR" ? "Registrar Pagamento" : "Registrar Recebimento"}
          </DialogTitle>
          <DialogDescription>
            Informe o valor pago para a parcela #{parcela?.numeroParcela} da conta #{contaId}.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex justify-between items-center text-sm border-b pb-2 text-muted-foreground">
            <span>Valor Total da Parcela:</span>
            <span className="font-semibold text-foreground">
              {new Intl.NumberFormat("pt-BR", {
                style: "currency",
                currency: "BRL",
              }).format(parcela?.valorParcela ?? 0)}
            </span>
          </div>

          <div className="flex justify-between items-center text-sm border-b pb-2 text-muted-foreground">
            <span>Valor Já Quitado:</span>
            <span className="font-semibold text-foreground">
              {new Intl.NumberFormat("pt-BR", {
                style: "currency",
                currency: "BRL",
              }).format(parcela?.valorPagoOuRecebido ?? 0)}
            </span>
          </div>

          <div className="flex justify-between items-center text-sm border-b pb-2 text-muted-foreground">
            <span>Saldo Devedor Restante:</span>
            <span className="font-semibold text-foreground">
              {new Intl.NumberFormat("pt-BR", {
                style: "currency",
                currency: "BRL",
              }).format(saldoRestante)}
            </span>
          </div>

          <Field data-invalid={!!error}>
            <div className="flex justify-between items-center">
              <FieldLabel className="text-right font-semibold">Valor a Baixar (R$)</FieldLabel>
              <NumberInput
                inputSize="full"
                value={valorBaixa}
                decimals={2}
                inputMode="decimal"
                onNumberChange={(num) => setValorBaixa(num)}
                className="h-9 w-48 text-right font-semibold"
                aria-invalid={!!error}
                disabled={mutation.isPending}
              />
            </div>
            {error && (
              <FieldError className="mt-1 block text-right">
                {error}
              </FieldError>
            )}
          </Field>
        </div>

        <DialogFooter className="flex gap-2 justify-end">
          <Button
            type="button"
            variant="outline"
            disabled={mutation.isPending}
            onClick={() => onOpenChange(false)}
          >
            Cancelar <Kbd>Esc</Kbd>
          </Button>
          <Button
            type="button"
            disabled={mutation.isPending}
            onClick={handleConfirm}
          >
            {mutation.isPending ? (
              "Processando..."
            ) : (
              <span className="flex items-center gap-1.5">
                Confirmar
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
  );
}
