"use client";

import React, { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { useHotkeys } from "@tanstack/react-hotkeys";
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
  isEstorno?: boolean;
}

export function BaixaParcelaDialog({
  open,
  onOpenChange,
  contaId,
  parcela,
  tipo,
  onSuccess,
  isEstorno = false,
}: BaixaParcelaDialogProps) {
  const contentRef = React.useRef<HTMLDivElement>(null);
  const queryClient = useQueryClient();
  const saldoRestante = parcela
    ? Math.max(0, parcela.valorParcela - parcela.valorPagoOuRecebido)
    : 0;

  // Set default value based on whether we are performing a write-off (baixa) or a reversal (estorno)
  const initialValue = isEstorno
    ? (parcela?.valorPagoOuRecebido ?? 0)
    : saldoRestante;
  const [valorBaixa, setValorBaixa] = useState<number>(initialValue);
  const [error, setError] = useState<string | null>(null);

  // Sync state if parcela or isEstorno changes
  React.useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setValorBaixa(
      isEstorno ? (parcela?.valorPagoOuRecebido ?? 0) : saldoRestante,
    );
    setError(null);
  }, [parcela, isEstorno, saldoRestante]);

  const mutation = useMutation({
    mutationFn: async () => {
      if (!parcela) return;
      if (tipo === "PAGAR") {
        if (isEstorno) {
          await contasPagarApi.estornarPagamento(
            contaId,
            parcela.numeroParcela,
            valorBaixa,
          );
        } else {
          await contasPagarApi.registrarPagamento(
            contaId,
            parcela.numeroParcela,
            valorBaixa,
          );
        }
      } else {
        if (isEstorno) {
          await contasReceberApi.estornarRecebimento(
            contaId,
            parcela.numeroParcela,
            valorBaixa,
          );
        } else {
          await contasReceberApi.registrarRecebimento(
            contaId,
            parcela.numeroParcela,
            valorBaixa,
          );
        }
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: [tipo === "PAGAR" ? "contas-pagar" : "contas-receber"],
      });
      onSuccess();
      onOpenChange(false);
    },
  });

  const handleConfirm = () => {
    setError(null);
    if (valorBaixa <= 0) {
      setError(
        isEstorno
          ? "O valor a estornar deve ser maior que zero."
          : "O valor a baixar deve ser maior que zero.",
      );
      return;
    }
    if (isEstorno) {
      const maxEstorno = parcela?.valorPagoOuRecebido ?? 0;
      if (valorBaixa > maxEstorno + 0.01) {
        setError(
          `O valor não pode exceder o valor já pago/recebido (R$ ${maxEstorno.toFixed(2)}).`,
        );
        return;
      }
    } else {
      if (valorBaixa > saldoRestante + 0.01) {
        setError(
          `O valor não pode exceder o saldo restante (R$ ${saldoRestante.toFixed(2)}).`,
        );
        return;
      }
    }
    mutation.mutate();
  };

  useHotkeys(
    [
      {
        hotkey: "Alt+Enter",
        callback: (e) => {
          if (typeof document !== "undefined" && contentRef.current) {
            const dialogs = document.querySelectorAll('[role="dialog"]');
            const topDialog =
              dialogs.length > 0 ? dialogs[dialogs.length - 1] : null;
            const myDialog =
              contentRef.current.closest('[role="dialog"]') ||
              contentRef.current;
            if (topDialog && myDialog !== topDialog) return;
          }
          e.preventDefault();
          e.stopPropagation();
          handleConfirm();
        },
        options: {
          enabled: open && !mutation.isPending,
          ignoreInputs: false,
        },
      },
    ],
    { conflictBehavior: "allow" },
  );

  const getTitle = () => {
    if (isEstorno) {
      return tipo === "PAGAR" ? "Estornar Pagamento" : "Estornar Recebimento";
    }
    return tipo === "PAGAR" ? "Registrar Pagamento" : "Registrar Recebimento";
  };

  const getDescription = () => {
    if (isEstorno) {
      return `Informe o valor a estornar para a parcela #${parcela?.numeroParcela} da conta #${contaId}.`;
    }
    return `Informe o valor pago para a parcela #${parcela?.numeroParcela} da conta #${contaId}.`;
  };

  const getFieldLabel = () => {
    return isEstorno ? "Valor a Estornar (R$)" : "Valor a Baixar (R$)";
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent ref={contentRef} className="max-w-md">
        <DialogHeader>
          <DialogTitle>{getTitle()}</DialogTitle>
          <DialogDescription>{getDescription()}</DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="text-muted-foreground flex items-center justify-between border-b pb-2 text-sm">
            <span>Valor Total da Parcela:</span>
            <span className="text-foreground font-semibold">
              {new Intl.NumberFormat("pt-BR", {
                style: "currency",
                currency: "BRL",
              }).format(parcela?.valorParcela ?? 0)}
            </span>
          </div>

          <div className="text-muted-foreground flex items-center justify-between border-b pb-2 text-sm">
            <span>Valor Já Quitado:</span>
            <span className="text-foreground font-semibold">
              {new Intl.NumberFormat("pt-BR", {
                style: "currency",
                currency: "BRL",
              }).format(parcela?.valorPagoOuRecebido ?? 0)}
            </span>
          </div>

          {!isEstorno && (
            <div className="text-muted-foreground flex items-center justify-between border-b pb-2 text-sm">
              <span>Saldo Devedor Restante:</span>
              <span className="text-foreground font-semibold">
                {new Intl.NumberFormat("pt-BR", {
                  style: "currency",
                  currency: "BRL",
                }).format(saldoRestante)}
              </span>
            </div>
          )}

          <Field data-invalid={!!error}>
            <div className="flex items-center justify-between">
              <FieldLabel className="text-right font-semibold">
                {getFieldLabel()}
              </FieldLabel>
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
              <FieldError className="mt-1 block text-right">{error}</FieldError>
            )}
          </Field>
        </div>

        <DialogFooter className="flex justify-end gap-2">
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
              <span className="flex items-center gap-2">
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
