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
  FieldDescription,
  FieldLabel,
  Input,
  Kbd,
  KbdGroup,
} from "@/ui/primitives";
import type { ImportacaoXml } from "./upsert";

const MAX_BYTES = 2 * 1024 * 1024;

/** Escolha o XML da NF-e; devolve a prévia (nada é gravado ainda). */
export function ImportarXmlWindow() {
  const activeWindow = useWindow<ImportacaoXml>();
  const ui = useUi();
  const [xml, setXml] = React.useState("");
  const [emitente, setEmitente] = React.useState<Emitente | null>(null);
  const [busy, setBusy] = React.useState(false);
  const [erroEmitente, setErroEmitente] = React.useState<string>();
  const [arquivo, setArquivo] = React.useState<string>();
  const emitenteRef = React.useRef<HTMLDivElement>(null);
  const seletor = React.useRef<HTMLInputElement>(null);
  const escolherArquivo = () => seletor.current?.click();
  const foco = React.useCallback((el: HTMLDivElement | null) => {
    el?.querySelector("input")?.focus();
  }, []);

  const ler = async () => {
    if (busy) return;
    if (!emitente) {
      setErroEmitente("Selecione o emitente (sua empresa).");
      return void foco(emitenteRef.current);
    }
    if (!xml.trim())
      return void ui.feedback.notify({
        type: "error",
        title: "Escolha o arquivo XML da NF-e.",
      });
    if (new Blob([xml]).size > MAX_BYTES)
      return void ui.feedback.notify({ type: "error", title: "XML maior que 2 MB." });
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
      <div ref={(el) => { if (el && !emitenteRef.current) foco(el); emitenteRef.current = el; }}>
        <EmitenteInput
          name="xml-emitente"
          initialItem={emitente}
          error={erroEmitente}
          onSelectItem={(e) => { setEmitente(e); setErroEmitente(undefined); }}
          onSelectId={() => {}}
        />
      </div>
      <Field>
        <FieldLabel htmlFor="xml-arquivo">Arquivo XML</FieldLabel>
        {/* O seletor nativo mostra "Choose file" no idioma do navegador: fica oculto e o campo abaixo o aciona. */}
        <input
          ref={seletor}
          type="file"
          accept=".xml,text/xml,application/xml"
          className="hidden"
          tabIndex={-1}
          onChange={async (e) => {
            const f = e.target.files?.[0];
            if (!f) return;
            if (f.size > MAX_BYTES) {
              e.target.value = "";
              setArquivo(undefined);
              setXml("");
              return void ui.feedback.notify({ type: "error", title: "Arquivo maior que 2 MB." });
            }
            setArquivo(f.name);
            setXml(await f.text());
          }}
        />
        <div className="relative w-full">
          <Input
            id="xml-arquivo"
            readOnly
            inputSize="full"
            className="cursor-pointer pr-10"
            value={arquivo ?? ""}
            placeholder="Nenhum arquivo selecionado. Clique ou pressione Enter para escolher."
            onClick={escolherArquivo}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.altKey && !e.repeat) {
                e.preventDefault();
                escolherArquivo();
              }
            }}
          />
          <Button
            type="button"
            size="icon-xs"
            variant="ghost"
            tabIndex={-1}
            title="Escolher arquivo"
            aria-label="Escolher arquivo"
            className="text-muted-foreground hover:text-foreground absolute top-1 right-1 h-6 w-6"
            onMouseDown={(e) => e.preventDefault()}
            onClick={escolherArquivo}
          >
            <FileUp className="size-4" />
          </Button>
        </div>
        <FieldDescription>XML da NF-e, até 2 MB.</FieldDescription>
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
