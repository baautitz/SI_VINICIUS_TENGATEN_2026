"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { Trash2 } from "lucide-react";
import { balancosApi } from "@/api/estoque";
import { WindowActions } from "@/imperative-ui";
import { SkuInput } from "@/components/entity-inputs/sku-input";
import { SkuViewLink } from "@/components/sku-view-link";
import { Sku, getFullSkuName } from "@/features/catalogo/skus/types";
import { useWindow, useWindowCommands, useUi } from "@/ui/imperative";
import { Button, Field, FieldLabel, Input, Kbd, KbdGroup, Spinner } from "@/ui/primitives";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/ui/primitives";
import { NumberInput } from "@/ui/composites";
import { cn } from "@/lib/utils";
import { statusBalancoLabels } from "./types";

export interface BalancoWindowProps {
  /** null = novo balanço. */
  balancoId: number | null;
}

interface ItemLinha {
  sku: string;
  produtoNome: string;
  unidadeMedidaSigla: string;
  permiteDecimais: boolean;
  quantidadeSistema: number;
  quantidadeContada: number;
}

const fmt = (n: number) => n.toLocaleString("pt-BR", { maximumFractionDigits: 4 });

// Balanço com itens escolhidos: adiciona SKUs com a quantidade contada, salva como rascunho (aberto) ou fecha,
// o que lança as diferenças como movimentações de estoque.
export function BalancoWindow({ balancoId }: BalancoWindowProps) {
  const { data: balanco, isLoading } = useQuery({
    queryKey: ["balancos", "detail", balancoId],
    queryFn: () => balancosApi.getById(balancoId!),
    enabled: balancoId !== null,
    // O formulário copia os dados só na montagem: nunca reaproveita cache de uma abertura anterior.
    gcTime: 0,
  });

  if (balancoId !== null && (isLoading || !balanco)) {
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );
  }

  return <BalancoForm balancoId={balancoId} balanco={balanco ?? null} />;
}

function BalancoForm({
  balancoId,
  balanco,
}: {
  balancoId: number | null;
  balanco: import("./types").Balanco | null;
}) {
  const activeWindow = useWindow<"saved" | "closed">();
  const ui = useUi();
  const readOnly = !!balanco && balanco.status !== "ABERTO";

  const [observacao, setObservacao] = React.useState(balanco?.observacao ?? "");
  const [itens, setItens] = React.useState<ItemLinha[]>(
    () =>
      balanco?.itens.map((i) => ({
        sku: i.sku,
        produtoNome: i.produtoNome,
        unidadeMedidaSigla: i.unidadeMedidaSigla,
        permiteDecimais: !Number.isInteger(i.quantidadeSistema) || !Number.isInteger(i.quantidadeContada ?? 0),
        quantidadeSistema: Number(i.quantidadeSistema),
        quantidadeContada: Number(i.quantidadeContada ?? 0),
      })) ?? [],
  );
  const [skuInputKey, setSkuInputKey] = React.useState(0);
  const [busy, setBusy] = React.useState(false);
  const skuInputRef = React.useRef<HTMLInputElement>(null);

  // Adicionar item remonta o SkuInput (nova key); a ref devolve o foco depois da troca do DOM.
  const setSkuInput = React.useCallback(
    (element: HTMLInputElement | null) => {
      skuInputRef.current = element;
      if (element && skuInputKey > 0) requestAnimationFrame(() => element.focus());
    },
    [skuInputKey],
  );

  const registerDirty = React.useCallback(() => {
    activeWindow.setDirtyCheck(() => !readOnly && (observacao !== (balanco?.observacao ?? "") || itens.length > 0));
    return () => activeWindow.setDirtyCheck(null);
  }, [activeWindow, readOnly, observacao, balanco, itens.length]);

  // Cada leitura do mesmo SKU soma à contagem (contagem por leitor).
  const handleSkuAdded = (sku: Sku | null, quantidade: number = 1) => {
    if (!sku) return;
    const permiteDecimais = !!sku.produto?.unidadeMedida?.permiteDecimais;
    setItens((atual) => {
      const existente = atual.find((i) => i.sku === sku.sku);
      if (existente) {
        return atual.map((i) =>
          i.sku === sku.sku
            ? { ...i, quantidadeContada: Number(Math.max(0, i.quantidadeContada + quantidade).toFixed(4)) }
            : i,
        );
      }
      if (quantidade < 0) return atual;
      return [
        ...atual,
        {
          sku: sku.sku,
          produtoNome: getFullSkuName(sku),
          unidadeMedidaSigla: sku.produto?.unidadeMedida?.sigla ?? "",
          permiteDecimais,
          quantidadeSistema: Number(sku.estoque),
          quantidadeContada: Number(quantidade.toFixed(permiteDecimais ? 4 : 0)),
        },
      ];
    });
    setSkuInputKey((k) => k + 1);
  };

  const atualizarContagem = (index: number, valor: number) =>
    setItens((atual) =>
      atual.map((item, i) =>
        i === index
          ? { ...item, quantidadeContada: Number(Number(valor || 0).toFixed(item.permiteDecimais ? 4 : 0)) }
          : item,
      ),
    );

  const removerItem = async (index: number) => {
    const item = itens[index];
    const ok = await ui.windows.confirm({
      title: "Remover Item?",
      description: `Deseja realmente remover o SKU ${item.sku} deste balanço?`,
      confirmLabel: "Remover Item",
      confirmVariant: "destructive",
    });
    if (ok) setItens((atual) => atual.filter((_, i) => i !== index));
  };

  // Cria ou atualiza o balanço; devolve o id salvo ou null se houve erro (já notificado).
  const salvar = async (): Promise<number | null> => {
    if (itens.length === 0) {
      ui.feedback.notify({ type: "warning", title: "Adicione pelo menos um item ao balanço." });
      return null;
    }
    const payload = {
      observacao: observacao.trim() || null,
      itens: itens.map((i) => ({ sku: i.sku, quantidadeContada: i.quantidadeContada })),
    };
    const res = balancoId ? await balancosApi.update(balancoId, payload) : await balancosApi.create(payload);
    if (res.success === false || !res.data) {
      ui.feedback.notifyError(res, { fallbackTitle: "Não foi possível salvar o balanço." });
      return null;
    }
    return res.data.id;
  };

  const run = async (acao: () => Promise<void>) => {
    if (busy) return;
    setBusy(true);
    try {
      await acao();
    } catch (error) {
      ui.feedback.notifyError(error, { fallbackTitle: "Operação não concluída." });
    } finally {
      setBusy(false);
    }
  };

  const salvarRascunho = () =>
    run(async () => {
      if ((await salvar()) !== null) activeWindow.resolve("saved");
    });

  const fecharBalanco = () =>
    run(async () => {
      const ok = await ui.windows.confirm({
        title: "Fechar balanço?",
        description:
          "As diferenças entre a contagem e o saldo atual serão lançadas como movimentações de estoque (origem Balanço). Esta ação não pode ser desfeita, só estornada.",
        confirmLabel: "Fechar balanço",
      });
      if (!ok) return;
      const id = await salvar();
      if (id === null) return;
      const res = await balancosApi.fechar(id);
      if (res.success === false) {
        ui.feedback.notifyError(res, { fallbackTitle: "Não foi possível fechar o balanço." });
        return;
      }
      activeWindow.resolve("closed");
    });

  useWindowCommands(
    React.useMemo(
      () => [
        {
          id: "balancos.focus-sku",
          hotkey: "Alt+K" as const,
          label: "Buscar SKU",
          enabled: !readOnly,
          run: (event: KeyboardEvent) => {
            event.preventDefault();
            skuInputRef.current?.focus();
          },
        },
        {
          id: "balancos.save-draft",
          hotkey: "Alt+S" as const,
          label: "Salvar rascunho",
          enabled: !readOnly && !busy,
          run: async (event: KeyboardEvent) => {
            event.preventDefault();
            await salvarRascunho();
          },
        },
        {
          id: "balancos.close",
          hotkey: "Alt+Enter" as const,
          label: "Fechar balanço",
          enabled: !readOnly && !busy,
          run: async (event: KeyboardEvent) => {
            event.preventDefault();
            await fecharBalanco();
          },
        },
      ],
      // eslint-disable-next-line react-hooks/exhaustive-deps
      [readOnly, busy, itens, observacao],
    ),
  );

  return (
    <div className="flex flex-col gap-4">
      <WindowActions>
        <Button type="button" variant="outline" onClick={() => activeWindow.dismiss("cancel")}>
          {readOnly ? "Fechar" : "Cancelar"} <Kbd>Esc</Kbd>
        </Button>
        {!readOnly && (
          <>
            <Button type="button" variant="secondary" disabled={busy} onClick={salvarRascunho}>
              Salvar Rascunho{" "}
              <KbdGroup>
                <Kbd>Alt</Kbd>
                <Kbd>S</Kbd>
              </KbdGroup>
            </Button>
            <Button type="button" disabled={busy} onClick={fecharBalanco}>
              Fechar Balanço{" "}
              <KbdGroup>
                <Kbd>Alt</Kbd>
                <Kbd>Enter</Kbd>
              </KbdGroup>
            </Button>
          </>
        )}
      </WindowActions>

      <div ref={registerDirty} className="flex flex-col gap-6">
        <div className="flex flex-row flex-wrap items-end gap-4">
          {balanco && (
            <div className="flex w-fit flex-col gap-2">
              <FieldLabel>Código</FieldLabel>
              <div className="bg-muted/50 text-foreground/80 flex h-8 items-center rounded-lg border px-3 font-mono text-sm">
                {balanco.id} · {statusBalancoLabels[balanco.status]}
              </div>
            </div>
          )}
          <Field className="min-w-64 flex-1">
            <FieldLabel htmlFor="balanco-observacao">Observação</FieldLabel>
            <Input
              id="balanco-observacao"
              value={observacao}
              maxLength={500}
              disabled={readOnly}
              placeholder="Ex.: inventário geral de outubro..."
              onChange={(e) => setObservacao(e.target.value)}
            />
          </Field>
        </div>

        <div className="flex flex-col gap-3 border-t pt-4">
          {!readOnly && (
            <div className="max-w-md">
              <SkuInput
                ref={setSkuInput}
                key={skuInputKey}
                name="balanco-add-sku"
                label="Itens do Balanço"
                onSelectSku={handleSkuAdded}
              />
            </div>
          )}

          <div className="bg-card overflow-x-auto rounded-lg border">
            <Table className="w-full">
              <TableHeader className="bg-muted border-b">
                <TableRow className="hover:bg-transparent">
                  <TableHead className="w-24 px-4 py-2">SKU</TableHead>
                  <TableHead className="w-full px-4 py-2">Produto</TableHead>
                  <TableHead className="w-36 px-4 py-2 text-right">Sistema</TableHead>
                  <TableHead className="min-w-40 px-4 py-2 text-right">Contado</TableHead>
                  <TableHead className="w-36 px-4 py-2 text-right">Diferença</TableHead>
                  {!readOnly && <TableHead className="w-14 px-4 py-2 text-center">Ação</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {itens.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={readOnly ? 5 : 6} className="text-muted-foreground px-4 py-8 text-center">
                      Nenhum item adicionado ainda.
                    </TableCell>
                  </TableRow>
                ) : (
                  itens.map((item, index) => {
                    const diferenca = Number((item.quantidadeContada - item.quantidadeSistema).toFixed(4));
                    return (
                      <TableRow key={item.sku} className="border-b last:border-0">
                        <TableCell className="px-4 py-2 font-mono text-sm font-bold">
                          <SkuViewLink sku={item.sku} enabled={readOnly} />
                        </TableCell>
                        <TableCell className="px-4 py-2 text-sm">
                          {item.produtoNome}{" "}
                          <span className="text-muted-foreground font-mono text-[10px] font-semibold uppercase">
                            {item.unidadeMedidaSigla}
                          </span>
                        </TableCell>
                        <TableCell className="text-muted-foreground px-4 py-2 text-right text-sm">
                          {fmt(item.quantidadeSistema)}
                        </TableCell>
                        <TableCell className="px-4 py-2 text-right">
                          <NumberInput
                            inputSize="full"
                            value={item.quantidadeContada}
                            decimals={item.permiteDecimais ? 4 : 0}
                            inputMode={item.permiteDecimais ? "decimal" : "numeric"}
                            disabled={readOnly}
                            onNumberChange={(num) => atualizarContagem(index, num)}
                            className="h-8 text-right text-sm font-bold"
                          />
                        </TableCell>
                        <TableCell
                          className={cn(
                            "px-4 py-2 text-right text-sm font-bold",
                            diferenca > 0 && "text-emerald-600",
                            diferenca < 0 && "text-destructive",
                          )}
                        >
                          {diferenca > 0 ? "+" : ""}
                          {fmt(diferenca)}
                        </TableCell>
                        {!readOnly && (
                          <TableCell className="px-4 py-2 text-center">
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon"
                              className="text-destructive hover:bg-destructive/10 h-7 w-7"
                              onClick={() => void removerItem(index)}
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </TableCell>
                        )}
                      </TableRow>
                    );
                  })
                )}
              </TableBody>
            </Table>
          </div>
        </div>
      </div>
    </div>
  );
}
