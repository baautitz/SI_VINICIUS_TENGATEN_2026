"use client";

import React from "react";
import { FileUp } from "lucide-react";
import { WindowActions } from "@/imperative-ui";
import { comprasApi } from "@/api/compras";
import { EmitenteInput } from "@/components/entity-inputs/emitente-input";
import type { Emitente } from "@/features/parceiros/emitentes/types";
import { useWindow, useWindowCommands, useUi } from "@/ui/imperative";
import {
  Button,
  Field,
  FieldLabel,
  Kbd,
  KbdGroup,
  Textarea,
} from "@/ui/primitives";
import type { ImportacaoXml } from "./upsert";

/** Cole ou escolha o XML da NF-e; devolve a prévia (nada é gravado ainda). */
export function ImportarXmlWindow() {
  const activeWindow = useWindow<ImportacaoXml>();
  const ui = useUi();
  const [xml, setXml] = React.useState("");
  const [emitente, setEmitente] = React.useState<Emitente | null>(null);
  const [busy, setBusy] = React.useState(false);

  const ler = async () => {
    if (busy) return;
    if (!emitente)
      return void ui.feedback.notify({
        type: "error",
        title: "Selecione o emitente (sua empresa).",
      });
    if (!xml.trim())
      return void ui.feedback.notify({
        type: "error",
        title: "Cole o XML ou escolha um arquivo.",
      });
    setBusy(true);
    try {
      const res = await comprasApi.previaNfe(xml, emitente.id);
      if (res.data) activeWindow.resolve({ xml, previa: res.data, emitente });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível ler o XML.",
      });
    } finally {
      setBusy(false);
    }
  };

  useWindowCommands([
    {
      id: "compras.xml.ler",
      hotkey: "Alt+Enter",
      label: "Ler XML",
      run: (e) => {
        e.preventDefault();
        void ler();
      },
    },
  ]);

  return (
    <div className="flex flex-col gap-4 py-2">
      <EmitenteInput
        name="xml-emitente"
        initialItem={emitente}
        onSelectItem={setEmitente}
        onSelectId={() => {}}
      />
      <Field>
        <FieldLabel htmlFor="xml-arquivo">Arquivo XML</FieldLabel>
        <input
          id="xml-arquivo"
          type="file"
          accept=".xml,text/xml,application/xml"
          className="text-sm"
          onChange={async (e) => {
            const f = e.target.files?.[0];
            if (f) setXml(await f.text());
          }}
        />
      </Field>
      <Field>
        <FieldLabel htmlFor="xml-texto">ou cole o conteúdo</FieldLabel>
        <Textarea
          id="xml-texto"
          value={xml}
          rows={10}
          className="font-mono text-xs"
          placeholder="<nfeProc>...</nfeProc>"
          onChange={(e) => setXml(e.target.value)}
        />
      </Field>
      <WindowActions>
        <Button
          type="button"
          variant="outline"
          onClick={() => activeWindow.dismiss("cancel")}
        >
          Cancelar <Kbd>Esc</Kbd>
        </Button>
        <Button type="button" disabled={busy} onClick={() => void ler()}>
          <FileUp /> {busy ? "Lendo..." : "Ler XML"}
          <KbdGroup>
            <Kbd>Alt</Kbd>
            <Kbd>Enter</Kbd>
          </KbdGroup>
        </Button>
      </WindowActions>
    </div>
  );
}
