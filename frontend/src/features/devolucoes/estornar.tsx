"use client";

import React from "react";
import { useQueryClient } from "@tanstack/react-query";
import { WindowActions, WindowCancelButton, useUi, useWindow, useWindowCommands } from "@/ui/imperative";
import { vendasApi } from "@/api/vendas";
import { Alert, AlertDescription, Button, Checkbox, Field, FieldDescription, FieldLabel, Textarea, HotkeyKbd } from "@/ui/primitives";
import { extractApiErrors, getApiErrorMessage, type ApiErrorItem } from "@/utils/api-error";
import { combinacaoDe } from "@/lib/atalhos";
import { MAX_MOTIVO_ESTORNO, MIN_MOTIVO_ESTORNO } from "@/lib/motivo-estorno";
import { abrirNfe } from "@/features/nfes";
import type { Devolucao } from "@/features/vendas/types";

export interface EstornarDevolucaoWindowProps {
  devolucaoId: number;
}

/** Pede o motivo, exige confirmação explícita e faz o POST; as travas do backend aparecem em lista e a janela segue aberta. */
export function EstornarDevolucaoWindow({ devolucaoId }: EstornarDevolucaoWindowProps) {
  const activeWindow = useWindow<{ nfeEstorno?: Devolucao["nfeEstorno"] }>();
  const [motivo, setMotivo] = React.useState("");
  const [confirmado, setConfirmado] = React.useState(false);
  const [enviando, setEnviando] = React.useState(false);
  const [erros, setErros] = React.useState<ApiErrorItem[]>([]);
  const tamanho = motivo.trim().length;
  const motivoValido = tamanho >= MIN_MOTIVO_ESTORNO;
  const pode = motivoValido && confirmado && !enviando;

  const estornar = async () => {
    if (!pode) return;
    setEnviando(true);
    setErros([]);
    try {
      const res = await vendasApi.estornarDevolucao(devolucaoId, motivo.trim());
      activeWindow.resolve({ nfeEstorno: res?.data?.nfeEstorno });
    } catch (e) {
      const { errors } = extractApiErrors(e);
      setErros(errors.length ? errors : [{ code: "ERRO_API", message: getApiErrorMessage(e, "Não foi possível estornar a devolução."), field: null }]);
      setEnviando(false);
    }
  };

  useWindowCommands([
    {
      id: "devolucoes.estornar.confirmar",
      hotkey: combinacaoDe("confirmar"),
      label: "Estornar devolução",
      enabled: pode,
      run: (e) => {
        e.preventDefault();
        void estornar();
      },
    },
  ]);

  return (
    <div className="flex flex-col gap-4 py-2">
      <p className="text-sm text-muted-foreground">
        O estorno faz os itens saírem de novo do estoque, desfaz o abatimento nas contas a receber e cancela o
        reembolso ao cliente, se houver. Se o reembolso já tiver baixa (paga ou parcial), o estorno é barrado:
        estorne o pagamento primeiro. A devolução fica registrada como estornada. Esta ação não pode ser desfeita.
      </p>
      <Field>
        <FieldLabel htmlFor="devolucao-motivo-estorno" required>Motivo do estorno</FieldLabel>
        <Textarea
          id="devolucao-motivo-estorno"
          autoFocus
          rows={4}
          maxLength={MAX_MOTIVO_ESTORNO}
          value={motivo}
          onChange={(e) => setMotivo(e.target.value)}
        />
        <FieldDescription className={motivoValido ? "" : "text-destructive"}>
          {tamanho}/{MAX_MOTIVO_ESTORNO} caracteres (mínimo {MIN_MOTIVO_ESTORNO}).
        </FieldDescription>
      </Field>
      <label className="flex items-center gap-2 text-sm">
        <Checkbox checked={confirmado} onCheckedChange={(v) => setConfirmado(v === true)} />
        Entendo que o estorno da devolução #{devolucaoId} não pode ser desfeito.
      </label>
      {erros.length > 0 && (
        <Alert variant="destructive" role="alert">
          <AlertDescription>
            <p className="mb-1 font-medium">O estorno não foi realizado:</p>
            <ul className="list-disc space-y-1 pl-5">
              {erros.map((e, i) => (
                <li key={i}>{e.message}</li>
              ))}
            </ul>
          </AlertDescription>
        </Alert>
      )}
      <WindowActions>
        <WindowCancelButton />
        <Button type="button" variant="destructive" disabled={!pode} onClick={() => void estornar()}>
          {enviando ? "Estornando..." : "Estornar devolução"}
          <HotkeyKbd hotkey={combinacaoDe("confirmar")} />
        </Button>
      </WindowActions>
    </div>
  );
}

/** Fluxo de estornar devolução: janela, invalidação das queries afetadas e aviso de NF-e ativa. Retorna true se estornou. */
export function useEstornarDevolucao() {
  const ui = useUi();
  const queryClient = useQueryClient();

  return async (devolucaoId: number): Promise<boolean> => {
    const r = await ui.windows.open<{ nfeEstorno?: Devolucao["nfeEstorno"] }, EstornarDevolucaoWindowProps>({
      component: EstornarDevolucaoWindow,
      props: { devolucaoId },
      title: `Estornar Devolução de venda #${devolucaoId}`,
      size: "small",
    });
    if (r.status !== "confirmed") return false;
    await Promise.all(
      ["devolucoes", "vendas", "contas-receber", "contas-pagar", "movimentacoes", "skus", "produtos", "nfes"].map((k) =>
        queryClient.invalidateQueries({ queryKey: [k] }),
      ),
    );
    ui.feedback.notify({ type: "success", title: `Devolução de venda #${devolucaoId} estornada.` });
    const nfe = r.value?.nfeEstorno;
    if (nfe) {
      // A NF-e de devolução não se cancela: o estorno emite a NF-e de estorno da devolução.
      const abrir = await ui.windows.confirm({
        title: "NF-e de estorno da devolução emitida",
        description: `O estorno emitiu a NF-e de estorno da devolução (chave ${nfe.chaveAcesso}). Deseja abrir a NF-e?`,
        confirmLabel: "Abrir NF-e",
      });
      if (abrir) await abrirNfe(ui, nfe.nfeId);
    }
    return true;
  };
}
