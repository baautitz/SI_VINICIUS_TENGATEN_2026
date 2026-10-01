"use client";

import React, { useState } from "react";
import { WindowActions } from "@/imperative-ui";
import { useMutation } from "@tanstack/react-query";
import { Button } from "@/ui/primitives";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { Field, FieldLabel, FieldError } from "@/ui/primitives";
import { NumberInput } from "@/ui/composites";
import { contasPagarApi, contasReceberApi } from "@/api/financeiro";
import { useWindow, useWindowCommands } from "@/ui/imperative";

export interface BaixaParcelaWindowProps {
  contaId: number;
  parcela: {
    numeroParcela: number;
    valorParcela: number;
    valorPagoOuRecebido: number;
    status: string;
  } | null;
  tipo: "PAGAR" | "RECEBER";
  isEstorno?: boolean;
}

export function BaixaParcelaWindow({
  contaId,
  parcela,
  tipo,
  isEstorno = false,
}: BaixaParcelaWindowProps) {
  const activeWindow = useWindow<true>();
  const saldoRestante = parcela
    ? Math.max(0, parcela.valorParcela - parcela.valorPagoOuRecebido)
    : 0;

  // Set default value based on whether we are performing a write-off (baixa) or a reversal (estorno)
  const initialValue = isEstorno
    ? (parcela?.valorPagoOuRecebido ?? 0)
    : saldoRestante;
  const [valorBaixa, setValorBaixa] = useState<number>(initialValue);
  const [error, setError] = useState<string | null>(null);

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
  });

  const handleConfirm = React.useCallback(async () => {
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
      if (Math.round(valorBaixa * 100) > Math.round(maxEstorno * 100)) {
        setError(
          `O valor não pode exceder o valor já pago/recebido (R$ ${maxEstorno.toFixed(2)}).`,
        );
        return;
      }
    } else {
      if (Math.round(valorBaixa * 100) > Math.round(saldoRestante * 100)) {
        setError(
          `O valor não pode exceder o saldo restante (R$ ${saldoRestante.toFixed(2)}).`,
        );
        return;
      }
    }
    try {
      await mutation.mutateAsync();
      activeWindow.resolve(true);
    } catch {
      // O MutationCache central apresenta o erro operacional em um toast.
    }
  }, [activeWindow, isEstorno, mutation, parcela, saldoRestante, valorBaixa]);

  const commands = React.useMemo(
    () => [
      {
        id: "financeiro.baixa-parcela.confirm",
        hotkey: "Alt+Enter" as const,
        label: "Confirmar baixa da parcela",
        enabled: !mutation.isPending,
        run: async (event: KeyboardEvent) => {
          event.preventDefault();
          await handleConfirm();
        },
      },
    ],
    [handleConfirm, mutation.isPending],
  );

  useWindowCommands(commands);

  const getFieldLabel = () => {
    return isEstorno ? "Valor a Estornar (R$)" : "Valor a Baixar (R$)";
  };

  return (
    <>
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
              className="h-8 w-48 text-right font-semibold"
              aria-invalid={!!error}
              disabled={mutation.isPending}
            />
          </div>
          {error && (
            <FieldError className="mt-1 block text-right">{error}</FieldError>
          )}
        </Field>
      </div>

      <WindowActions>
        <Button
          type="button"
          variant="outline"
          disabled={mutation.isPending}
          onClick={() => activeWindow.dismiss("cancel")}
        >
          Cancelar <Kbd>Esc</Kbd>
        </Button>
        <Button
          type="button"
          disabled={mutation.isPending}
          onClick={() => void handleConfirm()}
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
      </WindowActions>
    </>
  );
}
