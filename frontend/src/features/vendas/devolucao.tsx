"use client";

import React from "react";
import { WindowActions } from "@/imperative-ui";
import { useQuery } from "@tanstack/react-query";
import { NumberInput } from "@/ui/composites";
import {
  Button,
  Field,
  FieldError,
  FieldLabel,
  Kbd,
  KbdGroup,
  Spinner,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Textarea,
} from "@/ui/primitives";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { vendasApi } from "@/api/vendas";
import { getFullSkuName } from "@/features/catalogo/skus/types";
import { formatToLocal } from "@/utils/date-utils";
import type { CriarDevolucaoValues, Venda } from "./types";

const brl = (v: number) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(v);

// Valor líquido unitário (desconto rateado), igual ao calculado pelo backend.
const valorLiquidoUnitario = (item: Venda["itens"][number]) =>
  Number(item.valorTotal) / Number(item.quantidade);

export type TipoDevolucao = "total" | "parcial";

export function DevolucaoTipoWindow() {
  const activeWindow = useWindow<TipoDevolucao>();
  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm">Escolha o tipo de devolução.</p>
      <WindowActions>
        <Button type="button" variant="outline" onClick={() => activeWindow.dismiss("cancel")}>
          Cancelar <Kbd>Esc</Kbd>
        </Button>
        <Button type="button" variant="outline" onClick={() => activeWindow.resolve("parcial")}>
          Devolução parcial
        </Button>
        <Button type="button" autoFocus onClick={() => activeWindow.resolve("total")}>
          Devolução total
        </Button>
      </WindowActions>
    </div>
  );
}

export interface DevolucaoWindowProps {
  venda: Venda;
  /** Total: quantidades já preenchidas com o saldo devolvível; parcial: zeradas. */
  total?: boolean;
}

export function DevolucaoWindow({ venda, total: devolucaoTotal }: DevolucaoWindowProps) {
  const activeWindow = useWindow<CriarDevolucaoValues>();
  const { data: full, isLoading } = useQuery({
    queryKey: ["vendas", "detail", venda.id],
    queryFn: () => vendasApi.getById(venda.id),
  });
  const [quantidades, setQuantidades] = React.useState<Record<number, number>>({});
  const [error, setError] = React.useState("");

  // Campo não controlado: digitar o motivo não re-renderiza a janela.
  const motivoRef = React.useRef<HTMLTextAreaElement | null>(null);
  const registerMotivo = React.useCallback(
    (element: HTMLTextAreaElement | null) => {
      motivoRef.current = element;
      if (!element) return;
      activeWindow.setDirtyCheck(() => element.value.trim().length > 0);
      return () => activeWindow.setDirtyCheck(null);
    },
    [activeWindow],
  );

  const itens = React.useMemo(() => full?.itens ?? [], [full]);
  const qtdDe = React.useCallback(
    (i: Venda["itens"][number]) =>
      quantidades[i.id] ?? (devolucaoTotal ? Number(i.saldoDevolvivel ?? i.quantidade) : 0),
    [quantidades, devolucaoTotal],
  );
  const total = itens.reduce(
    (sum, i) => sum + qtdDe(i) * valorLiquidoUnitario(i),
    0,
  );

  const confirm = React.useCallback(() => {
    const motivo = (motivoRef.current?.value ?? "").trim();
    const selecionados = itens
      .filter((i) => qtdDe(i) > 0)
      .map((i) => ({ vendaItemId: i.id, quantidade: qtdDe(i) }));

    if (selecionados.length === 0) return setError("Informe a quantidade de ao menos um item.");
    if (itens.some((i) => qtdDe(i) > Number(i.saldoDevolvivel ?? i.quantidade)))
      return setError("Há quantidade acima do saldo devolvível.");
    if (motivo.length < 5) return setError("O motivo deve ter pelo menos 5 caracteres.");

    activeWindow.resolve({ motivo, itens: selecionados });
  }, [activeWindow, itens, qtdDe]);

  useWindowCommands(
    React.useMemo(
      () => [
        {
          id: "vendas.devolucao.confirm",
          hotkey: "Alt+Enter" as const,
          label: "Confirmar devolução",
          run: (event: KeyboardEvent) => {
            event.preventDefault();
            confirm();
          },
        },
      ],
      [confirm],
    ),
  );

  if (isLoading)
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );

  return (
    <div className="flex flex-col gap-4">
      <p className="text-muted-foreground text-xs">
        Os itens voltam ao estoque pelo custo original da venda. O valor abate o
        saldo em aberto da conta a receber; o que já foi recebido gera uma conta
        a pagar ao cliente.
      </p>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Item</TableHead>
            <TableHead className="text-right">Vendido</TableHead>
            <TableHead className="text-right">Devolvido</TableHead>
            <TableHead className="text-right">Saldo</TableHead>
            <TableHead className="w-32 text-right">Devolver</TableHead>
            <TableHead className="text-right">Valor</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {itens.map((item) => {
            const saldo = Number(item.saldoDevolvivel ?? item.quantidade);
            const decimais = !!item.sku.produto?.unidadeMedida?.permiteDecimais;
            const qtd = qtdDe(item);
            return (
              <TableRow key={item.id}>
                <TableCell>{getFullSkuName(item.sku) || item.sku.sku}</TableCell>
                <TableCell className="text-right">{Number(item.quantidade)}</TableCell>
                <TableCell className="text-right">{Number(item.quantidadeDevolvida ?? 0)}</TableCell>
                <TableCell className="text-right">{saldo}</TableCell>
                <TableCell className="text-right">
                  <NumberInput
                    value={qtd}
                    decimals={decimais ? 4 : 0}
                    inputSize="full"
                    inputMode={decimais ? "decimal" : "numeric"}
                    disabled={saldo <= 0}
                    aria-invalid={qtd > saldo}
                    onNumberChange={(v) => {
                      setError("");
                      setQuantidades((prev) => ({ ...prev, [item.id]: v ?? 0 }));
                    }}
                  />
                </TableCell>
                <TableCell className="text-right">{brl(qtd * valorLiquidoUnitario(item))}</TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
      <div className="text-right text-sm">
        Total a devolver: <strong className="text-lg">{brl(total)}</strong>
      </div>
      <Field data-invalid={!!error}>
        <FieldLabel htmlFor="motivo-devolucao">Motivo da Devolução</FieldLabel>
        <Textarea
          id="motivo-devolucao"
          ref={registerMotivo}
          onChange={() => error && setError("")}
          placeholder="Informe o motivo (mínimo de 5 caracteres)..."
          rows={3}
          maxLength={500}
        />
        {error && <FieldError>{error}</FieldError>}
      </Field>
      <WindowActions>
        <Button type="button" variant="outline" onClick={() => activeWindow.dismiss("cancel")}>
          Cancelar <Kbd>Esc</Kbd>
        </Button>
        <Button type="button" onClick={confirm}>
          Confirmar Devolução
          <KbdGroup className="ml-2">
            <Kbd>Alt</Kbd>
            <Kbd>Enter</Kbd>
          </KbdGroup>
        </Button>
      </WindowActions>
    </div>
  );
}

export interface DevolucoesHistoricoWindowProps {
  vendaId: number;
}

export function DevolucoesHistoricoWindow({ vendaId }: DevolucoesHistoricoWindowProps) {
  const { data: devolucoes, isLoading } = useQuery({
    queryKey: ["vendas", "devolucoes", vendaId],
    queryFn: () => vendasApi.listDevolucoes(vendaId),
  });

  if (isLoading)
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );

  if (!devolucoes?.length)
    return <p className="text-muted-foreground text-sm">Venda sem devoluções.</p>;

  return (
    <div className="flex flex-col gap-4">
      {devolucoes.map((d) => (
        <div key={d.id} className="rounded-md border p-3">
          <div className="flex justify-between text-sm font-semibold">
            <span>
              Devolução #{d.id} em {formatToLocal(d.dataDevolucao)}
            </span>
            <span>{brl(d.valorTotal)}</span>
          </div>
          <p className="text-muted-foreground mt-1 text-xs">Motivo: {d.motivo}</p>
          <ul className="mt-2 text-xs">
            {d.itens.map((i) => (
              <li key={i.id}>
                {i.sku}: {Number(i.quantidade)} x {brl(i.valorUnitario)} = {brl(i.valorTotal)}
              </li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  );
}
