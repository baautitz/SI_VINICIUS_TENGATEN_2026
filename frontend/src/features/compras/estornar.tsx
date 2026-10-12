"use client";

import React from "react";
import { WindowActions, WindowCancelButton } from "@/ui/imperative";
import { comprasApi } from "@/api/compras";
import {
  Alert,
  AlertDescription,
  Button,
  Checkbox,
  Field,
  FieldDescription,
  FieldLabel,
  Kbd,
  KbdGroup,
  Textarea,
} from "@/ui/primitives";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { extractApiErrors, getApiErrorMessage, type ApiErrorItem } from "@/utils/api-error";

export const MIN_MOTIVO_ESTORNO = 5;
export const MAX_MOTIVO_ESTORNO = 255;

export interface EstornarCompraWindowProps {
  compraId: number;
  temNfe: boolean;
}

/** Pede o motivo, exige confirmação explícita e faz o POST; as travas do backend aparecem em lista e a janela segue aberta. */
export function EstornarCompraWindow({ compraId, temNfe }: EstornarCompraWindowProps) {
  const activeWindow = useWindow<true>();
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
      await comprasApi.estornar(compraId, motivo.trim());
      activeWindow.resolve(true);
    } catch (e) {
      const { errors } = extractApiErrors(e);
      setErros(errors.length ? errors : [{ code: "ERRO_API", message: getApiErrorMessage(e, "Não foi possível estornar a compra."), field: null }]);
      setEnviando(false);
    }
  };

  useWindowCommands([
    {
      id: "compras.estornar.confirmar",
      hotkey: "Alt+Enter",
      label: "Estornar compra",
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
        O estorno devolve o estoque (com o custo médio de antes da compra), cancela as contas a pagar
        {temNfe ? ", cancela a NF-e de entrada" : ""} e mantém a compra registrada como estornada.
        Esta ação não pode ser desfeita.
      </p>
      <Field>
        <FieldLabel htmlFor="compra-motivo-estorno">Motivo do estorno</FieldLabel>
        <Textarea
          id="compra-motivo-estorno"
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
        Entendo que o estorno da compra #{compraId} não pode ser desfeito.
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
          {enviando ? "Estornando..." : "Estornar compra"}
          <KbdGroup>
            <Kbd>Alt</Kbd>
            <Kbd>Enter</Kbd>
          </KbdGroup>
        </Button>
      </WindowActions>
    </div>
  );
}
