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

const campos: { chave: keyof FiltrosAvancados; rotulo: string; type: string }[] = [
  { chave: "cpfCnpj", rotulo: "CPF/CNPJ", type: "text" },
  { chave: "vencimentoDe", rotulo: "Vencimento de", type: "date" },
  { chave: "vencimentoAte", rotulo: "Vencimento até", type: "date" },
  { chave: "emissaoDe", rotulo: "Emissão de", type: "date" },
  { chave: "emissaoAte", rotulo: "Emissão até", type: "date" },
  { chave: "valorMinimo", rotulo: "Valor mínimo", type: "number" },
  { chave: "valorMaximo", rotulo: "Valor máximo", type: "number" },
];

export function FiltrosAvancadosPopover({ valor, onChange }: {
  valor: FiltrosAvancados;
  onChange: (v: FiltrosAvancados) => void;
}) {
  const ativos = Object.values(valor).filter(Boolean).length;
  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button size="sm" variant={ativos ? "default" : "outline"} aria-label="Filtros avançados">
          <SlidersHorizontal /> Filtros{ativos ? ` (${ativos})` : ""}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-72 space-y-3">
        {campos.map((c) => (
          <label key={c.chave} className="block space-y-1 text-xs font-medium text-muted-foreground">
            {c.rotulo}
            <Input
              type={c.type}
              step={c.type === "number" ? "0.01" : undefined}
              min={c.type === "number" ? "0" : undefined}
              value={valor[c.chave]}
              onChange={(e) => onChange({ ...valor, [c.chave]: e.target.value })}
            />
          </label>
        ))}
        {ativos > 0 && (
          <Button size="sm" variant="ghost" className="w-full" onClick={() => onChange(FILTROS_AVANCADOS_VAZIOS)}>
            Limpar filtros
          </Button>
        )}
      </PopoverContent>
    </Popover>
  );
}
