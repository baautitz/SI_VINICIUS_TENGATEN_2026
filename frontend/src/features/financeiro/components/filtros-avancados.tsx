"use client";

import React from "react";
import { SlidersHorizontal } from "lucide-react";
import { Button, Input, Kbd, KbdGroup, Popover, PopoverContent, PopoverTrigger } from "@/ui/primitives";
import { DatePicker, NumberInput } from "@/ui/composites";
import { useWindowCommands } from "@/ui/imperative";
import { NavigationScope } from "@/ui/keyboard-navigation";

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

// Parâmetros da API; vazio vira undefined para não ir na query string. O NumberInput guarda "1234,50".
export const filtrosAvancadosParaApi = (f: FiltrosAvancados) => ({
  cpfCnpj: f.cpfCnpj.trim() || undefined,
  vencimentoDe: f.vencimentoDe || undefined,
  vencimentoAte: f.vencimentoAte || undefined,
  emissaoDe: f.emissaoDe || undefined,
  emissaoAte: f.emissaoAte || undefined,
  valorMinimo: f.valorMinimo ? f.valorMinimo.replace(",", ".") : undefined,
  valorMaximo: f.valorMaximo ? f.valorMaximo.replace(",", ".") : undefined,
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

const ATRASO_MS = 400;

// Os campos mudam na hora (rascunho); o filtro só vai para a consulta depois que a digitação para.
export function FiltrosAvancadosPopover({ valor, onChange }: {
  valor: FiltrosAvancados;
  onChange: (v: FiltrosAvancados) => void;
}) {
  const [aberto, setAberto] = React.useState(false);
  const [rascunho, setRascunho] = React.useState(valor);
  const timer = React.useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const ativos = Object.values(valor).filter(Boolean).length;

  useWindowCommands([
    { id: "filtros-avancados.abrir", hotkey: "Alt+F", label: "Abrir filtros avançados", run: () => setAberto(true) },
  ]);

  const alterar = (novo: FiltrosAvancados) => {
    setRascunho(novo);
    clearTimeout(timer.current);
    timer.current = setTimeout(() => onChange(novo), ATRASO_MS);
  };
  const limpar = () => {
    clearTimeout(timer.current);
    setRascunho(FILTROS_AVANCADOS_VAZIOS);
    onChange(FILTROS_AVANCADOS_VAZIOS);
  };

  return (
    <Popover open={aberto} onOpenChange={setAberto}>
      <PopoverTrigger asChild>
        <Button variant={ativos ? "default" : "outline"} aria-label="Filtros avançados">
          <SlidersHorizontal /> Filtros{ativos ? ` (${ativos})` : ""}
          <KbdGroup>
            <Kbd>Alt</Kbd>
            <Kbd>F</Kbd>
          </KbdGroup>
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-[24rem] gap-4 p-4">
        <NavigationScope id="filtros-avancados" priority={150} className="flex flex-col gap-4">
          <div className="flex items-center justify-between">
            <span className="text-sm font-semibold text-foreground">Filtros avançados</span>
            {ativos > 0 && (
              <Button size="xs" variant="ghost" onClick={limpar}>
                Limpar
              </Button>
            )}
          </div>
          {grupos.map((g) => (
            <fieldset key={g.titulo} className="flex flex-col gap-1.5">
              <legend className="mb-1.5 text-xs font-medium text-muted-foreground">{g.titulo}</legend>
              <div className={g.campos.length > 1 ? "grid grid-cols-2 gap-2" : "grid grid-cols-1"}>
                {g.campos.map((c) => {
                  const rotulo = `${g.titulo} ${c.rotulo}`;
                  if (c.type === "date")
                    return (
                      <DatePicker
                        key={c.chave}
                        placeholder={c.rotulo}
                        value={rascunho[c.chave] || null}
                        onChange={(v) => alterar({ ...rascunho, [c.chave]: v ?? "" })}
                      />
                    );
                  if (c.type === "number")
                    return (
                      <NumberInput
                        key={c.chave}
                        aria-label={rotulo}
                        placeholder={c.rotulo}
                        className="w-full text-right"
                        value={rascunho[c.chave]}
                        onValueChange={(v) => alterar({ ...rascunho, [c.chave]: v })}
                      />
                    );
                  return (
                    <Input
                      key={c.chave}
                      aria-label={rotulo}
                      placeholder={c.rotulo}
                      value={rascunho[c.chave]}
                      onChange={(e) => alterar({ ...rascunho, [c.chave]: e.target.value })}
                    />
                  );
                })}
              </div>
            </fieldset>
          ))}
        </NavigationScope>
      </PopoverContent>
    </Popover>
  );
}
