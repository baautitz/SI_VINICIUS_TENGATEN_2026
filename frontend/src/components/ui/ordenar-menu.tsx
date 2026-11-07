"use client";

import * as React from "react";
import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";

import { cn } from "@/lib/utils";
import type { Ordenacao } from "@/lib/ordenacao";

export interface ColunaOrdenavel {
  campo: string;
  rotulo: string;
}

type Modo = "padrao" | "asc" | "desc";
const MODOS: Modo[] = ["padrao", "asc", "desc"];
const ROTULO_MODO: Record<Modo, string> = { padrao: "Padrão", asc: "Crescente", desc: "Decrescente" };

function modoDe(ordenacao: Ordenacao | null, campo: string): Modo {
  return ordenacao?.campo === campo ? ordenacao.direcao : "padrao";
}

/**
 * Ordenação pelo teclado: ↑ ↓ escolhem a coluna, ← → giram o modo (padrão, crescente, decrescente)
 * e aplicam na hora. Enter ou Esc fecham.
 */
export function OrdenarMenu({
  colunas,
  ordenacao,
  onChange,
  onClose,
}: {
  colunas: ColunaOrdenavel[];
  ordenacao: Ordenacao | null;
  onChange: (ordenacao: Ordenacao | null) => void;
  onClose: () => void;
}) {
  const ref = React.useRef<HTMLDivElement>(null);
  const [indice, setIndice] = React.useState(() =>
    Math.max(0, colunas.findIndex((c) => c.campo === ordenacao?.campo)),
  );

  React.useEffect(() => {
    ref.current?.focus();
  }, []);

  const girar = (passo: 1 | -1) => {
    const coluna = colunas[indice];
    const atual = MODOS.indexOf(modoDe(ordenacao, coluna.campo));
    const proximo = MODOS[(atual + passo + MODOS.length) % MODOS.length];
    onChange(proximo === "padrao" ? null : { campo: coluna.campo, direcao: proximo });
  };

  const aoTeclar = (event: React.KeyboardEvent) => {
    const acoes: Record<string, () => void> = {
      ArrowUp: () => setIndice((i) => (i - 1 + colunas.length) % colunas.length),
      ArrowDown: () => setIndice((i) => (i + 1) % colunas.length),
      ArrowRight: () => girar(1),
      ArrowLeft: () => girar(-1),
      Enter: onClose,
      Escape: onClose,
    };
    const acao = acoes[event.key];
    if (!acao || event.altKey) return;
    event.preventDefault();
    event.stopPropagation();
    acao();
  };

  return (
    <div
      ref={ref}
      role="listbox"
      tabIndex={-1}
      aria-label="Ordenar por"
      aria-activedescendant={`ordenar-${colunas[indice]?.campo}`}
      onKeyDown={aoTeclar}
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node)) onClose();
      }}
      className="bg-popover absolute top-full right-0 z-50 mt-1 w-64 rounded-lg border p-1 shadow-md outline-none"
    >
      {colunas.map((coluna, i) => {
        const modo = modoDe(ordenacao, coluna.campo);
        return (
          <div
            key={coluna.campo}
            id={`ordenar-${coluna.campo}`}
            role="option"
            aria-selected={i === indice}
            className={cn(
              "flex items-center justify-between gap-2 rounded-md px-2 py-1.5 text-sm",
              i === indice && "bg-primary/10 font-medium",
            )}
          >
            <span className="truncate">{coluna.rotulo}</span>
            <span className="text-muted-foreground flex shrink-0 items-center gap-1 text-xs">
              {modo === "asc" ? <ArrowUp className="size-3" /> : modo === "desc" ? <ArrowDown className="size-3" /> : <ArrowUpDown className="size-3 opacity-40" />}
              {ROTULO_MODO[modo]}
            </span>
          </div>
        );
      })}
      <p className="text-muted-foreground border-t px-2 pt-1.5 pb-1 text-xs">↑ ↓ coluna · ← → modo · Enter fecha</p>
    </div>
  );
}
