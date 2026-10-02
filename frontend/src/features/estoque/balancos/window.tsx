"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { balancosApi } from "@/api/estoque";
import { WindowActions } from "@/imperative-ui";
import { useWindow, useWindowCommands, useUi } from "@/ui/imperative";
import { Button, Kbd, Spinner } from "@/ui/primitives";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/ui/primitives";
import { NumberInput } from "@/ui/composites";
import { cn } from "@/lib/utils";
import { Balanco, statusBalancoLabels } from "./types";

export interface BalancoWindowProps {
  balancoId: number;
}

const fmt = (n: number) => n.toLocaleString("pt-BR", { maximumFractionDigits: 4 });

// Contagem do balanço: salva a contagem digitada; "Fechar" apura as diferenças e gera as movimentações de ajuste.
export function BalancoWindow({ balancoId }: BalancoWindowProps) {
  const activeWindow = useWindow<true>();
  const ui = useUi();
  const { data: balanco, isLoading } = useQuery({
    queryKey: ["balancos", "detail", balancoId],
    queryFn: () => balancosApi.getById(balancoId),
  });
  const [contagem, setContagem] = React.useState<Record<string, string>>({});
  const [busy, setBusy] = React.useState(false);

  const aberto = balanco?.status === "ABERTO";
  const valor = (sku: string, salvo?: number | null) =>
    contagem[sku] ?? (salvo === null || salvo === undefined ? "" : String(salvo));

  const salvar = React.useCallback(async (): Promise<Balanco | null> => {
    if (!balanco) return null;
    const itens = Object.entries(contagem).map(([sku, v]) => ({
      sku,
      quantidadeContada: v === "" ? null : parseFloat(v.replace(",", ".")),
    }));
    if (itens.length === 0) return balanco;
    const res = await balancosApi.informarContagem(balanco.id, itens);
    if (res.success === false) {
      ui.feedback.notifyError(res, { fallbackTitle: "Não foi possível salvar a contagem." });
      return null;
    }
    setContagem({});
    return res.data ?? balanco;
  }, [balanco, contagem, ui]);

  const run = async (acao: () => Promise<void>) => {
    setBusy(true);
    try {
      await acao();
    } catch (error) {
      ui.feedback.notifyError(error, { fallbackTitle: "Operação não concluída." });
    } finally {
      setBusy(false);
    }
  };

  const salvarEFechar = () =>
    run(async () => {
      if (!balanco || !(await salvar())) return;
      const ok = await ui.windows.confirm({
        title: "Fechar balanço?",
        description:
          "As diferenças entre a contagem e o saldo atual serão lançadas como movimentações de ajuste (origem Balanço). Itens sem contagem são ignorados.",
        confirmLabel: "Fechar balanço",
      });
      if (!ok) return;
      const res = await balancosApi.fechar(balanco.id);
      if (res.success === false) {
        ui.feedback.notifyError(res, { fallbackTitle: "Não foi possível fechar o balanço." });
        return;
      }
      activeWindow.resolve(true);
    });

  const salvarContagem = () =>
    run(async () => {
      if (!(await salvar())) return;
      ui.feedback.notify({ type: "success", title: "Contagem salva." });
    });

  useWindowCommands(
    React.useMemo(
      () => [
        {
          id: "balancos.close",
          hotkey: "Alt+Enter" as const,
          label: "Fechar balanço",
          enabled: !!aberto && !busy,
          run: async (event: KeyboardEvent) => {
            event.preventDefault();
            await salvarEFechar();
          },
        },
      ],
      // eslint-disable-next-line react-hooks/exhaustive-deps
      [aberto, busy, contagem, balanco],
    ),
  );

  if (isLoading || !balanco) {
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <WindowActions>
        <Button type="button" variant="outline" onClick={() => activeWindow.dismiss("cancel")}>
          {aberto ? "Cancelar" : "Fechar"} <Kbd>Esc</Kbd>
        </Button>
        {aberto && (
          <>
            <Button type="button" variant="secondary" disabled={busy} onClick={salvarContagem}>
              Salvar contagem
            </Button>
            <Button type="button" disabled={busy} onClick={salvarEFechar}>
              Fechar balanço <Kbd>Alt+Enter</Kbd>
            </Button>
          </>
        )}
      </WindowActions>

      <p className="text-muted-foreground text-sm">
        Balanço #{balanco.id} — {statusBalancoLabels[balanco.status]}
        {balanco.observacao ? ` — ${balanco.observacao}` : ""}
      </p>

      <div className="bg-card overflow-x-auto rounded-lg border">
        <Table className="w-full">
          <TableHeader className="bg-muted border-b">
            <TableRow className="hover:bg-transparent">
              <TableHead className="w-28 px-4 py-2">SKU</TableHead>
              <TableHead className="px-4 py-2">Produto</TableHead>
              <TableHead className="w-36 px-4 py-2 text-right">Sistema</TableHead>
              <TableHead className="w-44 px-4 py-2 text-right">Contado</TableHead>
              <TableHead className="w-36 px-4 py-2 text-right">Diferença</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {balanco.itens.map((item) => {
              const v = valor(item.sku, item.quantidadeContada);
              const contado = v === "" ? null : parseFloat(v.replace(",", "."));
              const diferenca = contado === null || Number.isNaN(contado) ? null : contado - item.quantidadeSistema;
              return (
                <TableRow key={item.sku}>
                  <TableCell className="px-4 py-2 font-mono text-sm font-bold">{item.sku}</TableCell>
                  <TableCell className="px-4 py-2 text-sm">
                    {item.produtoNome}{" "}
                    <span className="text-muted-foreground font-mono text-[10px] uppercase">{item.unidadeMedidaSigla}</span>
                  </TableCell>
                  <TableCell className="text-muted-foreground px-4 py-2 text-right text-sm">{fmt(item.quantidadeSistema)}</TableCell>
                  <TableCell className="px-4 py-2 text-right">
                    <NumberInput
                      inputSize="full"
                      value={v}
                      decimals={4}
                      inputMode="decimal"
                      disabled={!aberto}
                      onValueChange={(val) => setContagem((c) => ({ ...c, [item.sku]: val }))}
                      className="h-8 text-right text-sm font-bold"
                    />
                  </TableCell>
                  <TableCell
                    className={cn(
                      "px-4 py-2 text-right text-sm font-bold",
                      diferenca !== null && diferenca > 0 && "text-emerald-600",
                      diferenca !== null && diferenca < 0 && "text-destructive",
                    )}
                  >
                    {diferenca === null ? "-" : `${diferenca > 0 ? "+" : ""}${fmt(diferenca)}`}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
