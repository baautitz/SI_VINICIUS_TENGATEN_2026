"use client";

import React from "react";
import { SlidersHorizontal } from "lucide-react";
import { Button, Input, Popover, PopoverContent, PopoverTrigger } from "@/ui/primitives";

export interface FiltrosAvancados {
  cpfCnpj: string;
  vencimentoDe: string;
  vencimentoAte: string;
  emissaoDe: string;
  emissaoAte: string;
  valorMinimo: string;
  valorMaximo: string;
}

export const FILTROS_AVANCADOS_VAZIOS: FiltrosAvancados = {
  cpfCnpj: "",
  vencimentoDe: "",
  vencimentoAte: "",
  emissaoDe: "",
  emissaoAte: "",
  valorMinimo: "",
  valorMaximo: "",
};

// Parâmetros da API; vazio vira undefined para não ir na query string.
export const filtrosAvancadosParaApi = (f: FiltrosAvancados) => ({
  cpfCnpj: f.cpfCnpj.trim() || undefined,
  vencimentoDe: f.vencimentoDe || undefined,
  vencimentoAte: f.vencimentoAte || undefined,
  emissaoDe: f.emissaoDe || undefined,
  emissaoAte: f.emissaoAte || undefined,
  valorMinimo: f.valorMinimo || undefined,
  valorMaximo: f.valorMaximo || undefined,
});

type Campo = { chave: keyof FiltrosAvancados; rotulo: string; type: "text" | "date" | "number" };

// Cada grupo vira uma linha; o par De/Até fica lado a lado.
const grupos: { titulo: string; campos: Campo[] }[] = [
  { titulo: "Contraparte", campos: [{ chave: "cpfCnpj", rotulo: "CPF/CNPJ", type: "text" }] },
  {
    titulo: "Vencimento",
    campos: [
      { chave: "vencimentoDe", rotulo: "De", type: "date" },
      { chave: "vencimentoAte", rotulo: "Até", type: "date" },
    ],
  },
  {
    titulo: "Emissão",
    campos: [
      { chave: "emissaoDe", rotulo: "De", type: "date" },
      { chave: "emissaoAte", rotulo: "Até", type: "date" },
    ],
  },
  {
    titulo: "Valor (R$)",
    campos: [
      { chave: "valorMinimo", rotulo: "Mínimo", type: "number" },
      { chave: "valorMaximo", rotulo: "Máximo", type: "number" },
    ],
  },
];

export function FiltrosAvancadosPopover({ valor, onChange }: {
  valor: FiltrosAvancados;
  onChange: (v: FiltrosAvancados) => void;
}) {
  const ativos = Object.values(valor).filter(Boolean).length;
  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant={ativos ? "default" : "outline"} aria-label="Filtros avançados">
          <SlidersHorizontal /> Filtros{ativos ? ` (${ativos})` : ""}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-[24rem] gap-4 p-4">
        <div className="flex items-center justify-between">
          <span className="text-sm font-semibold text-foreground">Filtros avançados</span>
          {ativos > 0 && (
            <Button size="xs" variant="ghost" onClick={() => onChange(FILTROS_AVANCADOS_VAZIOS)}>
              Limpar
            </Button>
          )}
        </div>
        {grupos.map((g) => (
          <fieldset key={g.titulo} className="flex flex-col gap-1.5">
            <legend className="mb-1.5 text-xs font-medium text-muted-foreground">{g.titulo}</legend>
            <div className={g.campos.length > 1 ? "grid grid-cols-2 gap-2" : "grid grid-cols-1"}>
              {g.campos.map((c) => (
                <Input
                  key={c.chave}
                  type={c.type}
                  aria-label={`${g.titulo} ${c.rotulo}`}
                  placeholder={c.type === "date" ? undefined : c.rotulo}
                  step={c.type === "number" ? "0.01" : undefined}
                  min={c.type === "number" ? "0" : undefined}
                  className="h-8"
                  value={valor[c.chave]}
                  onChange={(e) => onChange({ ...valor, [c.chave]: e.target.value })}
                />
              ))}
            </div>
          </fieldset>
        ))}
      </PopoverContent>
    </Popover>
  );
}
