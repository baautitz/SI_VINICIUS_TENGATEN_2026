"use client";

import React from "react";
import { WindowActions } from "@/imperative-ui";
import { Button, Field, FieldDescription, FieldLabel, Kbd, KbdGroup, Textarea } from "@/ui/primitives";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { MIN_JUSTIFICATIVA } from "./types";

/** Pede a justificativa (>= 15 caracteres) e resolve com o texto; quem chama faz o POST. */
export function CancelarNfeWindow({ numero }: { numero: number }) {
  const activeWindow = useWindow<string>();
  const [texto, setTexto] = React.useState("");
  const tamanho = texto.trim().length;
  const valido = tamanho >= MIN_JUSTIFICATIVA;
  const confirmar = () => valido && activeWindow.resolve(texto.trim());

  useWindowCommands([
    {
      id: "nfe.cancelar.confirmar",
      hotkey: "Alt+Enter",
      label: "Confirmar cancelamento",
      run: (e) => {
        e.preventDefault();
        confirmar();
      },
    },
  ]);

  return (
    <div className="flex flex-col gap-4 py-2">
      <p className="text-sm text-muted-foreground">
        O cancelamento da NF-e {numero} é apenas fiscal: não desfaz a venda, o estoque nem o financeiro. Esta ação não pode ser desfeita.
      </p>
      <Field>
        <FieldLabel htmlFor="nfe-justificativa">Justificativa</FieldLabel>
        <Textarea
          id="nfe-justificativa"
          autoFocus
          rows={4}
          maxLength={255}
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
        />
        <FieldDescription className={valido ? "" : "text-destructive"}>
          {tamanho}/255 caracteres (mínimo {MIN_JUSTIFICATIVA}).
        </FieldDescription>
      </Field>
      <WindowActions>
        <Button type="button" variant="outline" onClick={() => activeWindow.dismiss("cancel")}>
          Voltar <Kbd>Esc</Kbd>
        </Button>
        <Button type="button" variant="destructive" disabled={!valido} onClick={confirmar}>
          Cancelar NF-e
          <KbdGroup>
            <Kbd>Alt</Kbd>
            <Kbd>Enter</Kbd>
          </KbdGroup>
        </Button>
      </WindowActions>
    </div>
  );
}
