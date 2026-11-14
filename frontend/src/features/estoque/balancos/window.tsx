"use client";
import { capturarFocoDeRemocao } from "@/ui/foco-remocao";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { Trash2 } from "lucide-react";
import { balancosApi } from "@/api/estoque";
import { WindowActions } from "@/ui/imperative";
import { SkuInput } from "@/components/entity-inputs/sku-input";
import { SkuViewInput } from "@/components/entity-inputs/sku-view-input";
import { Sku, getFullSkuName } from "@/features/catalogo/skus/types";
import { useWindow, useWindowCommands, useUi, WindowCancelButton } from "@/ui/imperative";
import { Button, Field, FieldLabel, Input, Spinner, HotkeyKbd } from "@/ui/primitives";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/ui/primitives";
import { NumberInput } from "@/ui/composites";
import { navigationCell } from "@/ui/keyboard-navigation";
import { combinacaoDe } from "@/lib/atalhos";
import { cn } from "@/lib/utils";
import { statusBalancoLabels } from "./types";
import { Checkbox } from "@/ui/primitives";
import { EmitenteInput } from "@/components/entity-inputs/emitente-input";
import { MotivoMovimentacaoSelect } from "@/components/motivo-movimentacao-select";
import { diferencaDoItem, motivosDaDivergencia, resumoDoFechamento, type MotivoMovimentacao } from "@/features/estoque/motivos";

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
  motivoDivergencia: MotivoMovimentacao | null;
  recontado: boolean;
}

const paraDivergente = (i: ItemLinha) => ({
  sku: i.sku,
  quantidadeSistema: i.quantidadeSistema,
  quantidadeContada: i.quantidadeContada,
  motivoDivergencia: i.motivoDivergencia,
  recontado: i.recontado,
});

const semMotivo = (item: ItemLinha, diferenca: number) => diferenca !== 0 && item.motivoDivergencia === null;

const fmt = (n: number) =>
  n.toLocaleString("pt-BR", { maximumFractionDigits: 4 });

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
        permiteDecimais:
          !Number.isInteger(i.quantidadeSistema) ||
          !Number.isInteger(i.quantidadeContada ?? 0),
        quantidadeSistema: Number(i.quantidadeSistema),
        quantidadeContada: Number(i.quantidadeContada ?? 0),
        motivoDivergencia: i.motivoDivergencia ?? null,
        recontado: i.recontado ?? false,
      })) ?? [],
  );
  const [skuInputKey, setSkuInputKey] = React.useState(0);
  const [busy, setBusy] = React.useState(false);
  const [emitenteId, setEmitenteId] = React.useState<number | null>(null);
  const skuInputRef = React.useRef<HTMLInputElement>(null);

  // Adicionar item remonta o SkuInput (nova key); a ref devolve o foco depois da troca do DOM.
  const setSkuInput = React.useCallback(
    (element: HTMLInputElement | null) => {
      skuInputRef.current = element;
      if (element && skuInputKey > 0)
        requestAnimationFrame(() => element.focus());
    },
    [skuInputKey],
  );

  const registerDirty = React.useCallback(() => {
    activeWindow.setDirtyCheck(
      () =>
        !readOnly &&
        (observacao !== (balanco?.observacao ?? "") || itens.length > 0),
    );
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
            ? {
                ...i,
                quantidadeContada: Number(
                  Math.max(0, i.quantidadeContada + quantidade).toFixed(4),
                ),
              }
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
          quantidadeContada: Number(
            quantidade.toFixed(permiteDecimais ? 4 : 0),
          ),
          motivoDivergencia: null,
          recontado: false,
        },
      ];
    });
    setSkuInputKey((k) => k + 1);
  };

  // Mudar a contagem pode trocar falta por sobra (ou zerar a diferença): o motivo que já não vale é descartado.
  const atualizarContagem = (index: number, valor: number) =>
    setItens((atual) =>
      atual.map((item, i) => {
        if (i !== index) return item;
        const contada = Number(Number(valor || 0).toFixed(item.permiteDecimais ? 4 : 0));
        const diferenca = Number((contada - item.quantidadeSistema).toFixed(4));
        const motivoValido =
          item.motivoDivergencia !== null && motivosDaDivergencia(diferenca).includes(item.motivoDivergencia);
        return {
          ...item,
          quantidadeContada: contada,
          motivoDivergencia: motivoValido ? item.motivoDivergencia : null,
          recontado: diferenca > 0 ? item.recontado : false,
        };
      }),
    );

  const atualizarItem = (index: number, parcial: Partial<ItemLinha>) =>
    setItens((atual) => atual.map((item, i) => (i === index ? { ...item, ...parcial } : item)));

  const fechamento = resumoDoFechamento(itens.map(paraDivergente), emitenteId);

  const removerItem = async (index: number) => {
    const item = itens[index];
    const restaurarFoco = capturarFocoDeRemocao(() => setSkuInputKey((k) => k + 1));
    const ok = await ui.windows.confirm({
      title: "Remover Item?",
      description: `Deseja realmente remover o SKU ${item.sku} deste balanço?`,
      confirmLabel: "Remover Item",
      confirmVariant: "destructive",
    });
    if (ok) setItens((atual) => atual.filter((_, i) => i !== index));
    restaurarFoco();
  };

  // Cria ou atualiza o balanço; devolve o id salvo ou null se houve erro (já notificado).
  const salvar = async (): Promise<number | null> => {
    if (itens.length === 0) {
      ui.feedback.notify({
        type: "warning",
        title: "Adicione pelo menos um item ao balanço.",
      });
      return null;
    }
    const payload = {
      observacao: observacao.trim() || null,
      itens: itens.map((i) => {
        const diverge = (diferencaDoItem(paraDivergente(i)) ?? 0) !== 0;
        return {
          sku: i.sku,
          quantidadeContada: i.quantidadeContada,
          motivoDivergencia: diverge ? i.motivoDivergencia : null,
          recontado: diverge && i.recontado,
        };
      }),
    };
    const res = balancoId
      ? await balancosApi.update(balancoId, payload)
      : await balancosApi.create(payload);
    if (res.success === false || !res.data) {
      ui.feedback.notifyError(res, {
        fallbackTitle: "Não foi possível salvar o balanço.",
      });
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
      ui.feedback.notifyError(error, {
        fallbackTitle: "Operação não concluída.",
      });
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
      if (fechamento.erros.length > 0) {
        ui.feedback.notify({
          type: "warning",
          title: "Não dá para finalizar o balanço.",
          description: fechamento.erros.slice(0, 3).join(" ") + (fechamento.erros.length > 3 ? ` (+${fechamento.erros.length - 3})` : ""),
        });
        return;
      }
      const ok = await ui.windows.confirm({
        title: "Finalizar balanço?",
        description:
          "As diferenças entre a contagem e o saldo atual serão lançadas como movimentações de estoque (origem Balanço). Faltas por perda, furto ou vencimento emitem NF-e de baixa; sobras entram sem nota. Esta ação não pode ser desfeita, só estornada.",
        confirmLabel: "Finalizar balanço",
      });
      if (!ok) return;
      const id = await salvar();
      if (id === null) return;
      const res = await balancosApi.fechar(id, emitenteId);
      if (res.success === false) {
        ui.feedback.notifyError(res, {
          fallbackTitle: "Não foi possível finalizar o balanço.",
        });
        return;
      }
      activeWindow.resolve("closed");
    });

  useWindowCommands(
    React.useMemo(
      () => [
        {
          id: "balancos.focus-sku",
          hotkey: combinacaoDe("adicionarSku"),
          label: "Buscar SKU",
          enabled: !readOnly,
          run: (event: KeyboardEvent) => {
            event.preventDefault();
            skuInputRef.current?.focus();
          },
        },
        {
          id: "balancos.save-draft",
          hotkey: combinacaoDe("salvarRascunho"),
          label: "Salvar rascunho",
          enabled: !readOnly && !busy,
          run: async (event: KeyboardEvent) => {
            event.preventDefault();
            await salvarRascunho();
          },
        },
        {
          id: "balancos.close",
          hotkey: combinacaoDe("confirmar"),
          label: "Finalizar balanço",
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
        <WindowCancelButton readOnly={readOnly} />
        {!readOnly && (
          <>
            <Button
              type="button"
              variant="secondary"
              disabled={busy}
              onClick={salvarRascunho}
            >
              Salvar Rascunho{" "}
              <HotkeyKbd hotkey={combinacaoDe("salvarRascunho")} />
            </Button>
            <Button type="button" disabled={busy} onClick={fecharBalanco}>
              Finalizar balanço{" "}
              <HotkeyKbd hotkey={combinacaoDe("confirmar")} />
            </Button>
          </>
        )}
      </WindowActions>

      <div ref={registerDirty} className="flex flex-col gap-6">
        <div className="flex flex-row flex-wrap items-end gap-4">
          {balanco && (
            <div className="flex w-fit flex-col gap-2">
              <FieldLabel>Código</FieldLabel>
              <Input
                readOnly
                aria-label="Código"
                value={`${balanco.id} · ${statusBalancoLabels[balanco.status]}`}
                className="font-mono"
              />
            </div>
          )}
          {!readOnly && fechamento.exigeEmitente && (
            <div className="min-w-64">
              <EmitenteInput
                name="balanco-emitente"
                label="Emitente da NF-e de ajuste de estoque"
                required
                usarPadrao
                onSelectId={setEmitenteId}
              />
            </div>
          )}
          <Field className="min-w-64 flex-1">
            <FieldLabel htmlFor="balanco-observacao">Observação</FieldLabel>
            <Input
              id="balanco-observacao"
              value={observacao}
              maxLength={500}
              readOnly={readOnly}
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
                required
                onSelectSku={handleSkuAdded}
              />
            </div>
          )}

          <div className="bg-card overflow-clip rounded-lg border">
            <Table pageScroll>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-24">SKU</TableHead>
                  <TableHead>Produto</TableHead>
                  <TableHead className="w-36 text-right">Sistema</TableHead>
                  <TableHead className="min-w-40 text-right">Contado</TableHead>
                  <TableHead className="w-36 text-right">Diferença</TableHead>
                  <TableHead className="min-w-48">Motivo</TableHead>
                  <TableHead className="w-28 text-center">Recontado</TableHead>
                  {!readOnly && (
                    <TableHead className="w-14 text-center">Ação</TableHead>
                  )}
                </TableRow>
              </TableHeader>
              <TableBody>
                {itens.length === 0 ? (
                  <TableRow>
                    <TableCell
                      colSpan={readOnly ? 7 : 8}
                      className="text-muted-foreground py-8 text-center"
                    >
                      Nenhum item adicionado ainda.
                    </TableCell>
                  </TableRow>
                ) : (
                  itens.map((item, index) => {
                    const diferenca = Number(
                      (item.quantidadeContada - item.quantidadeSistema).toFixed(
                        4,
                      ),
                    );
                    return (
                      <TableRow key={item.sku}>
                        <TableCell className="font-mono font-bold">
                          <SkuViewInput name={`balanco-sku-${item.sku}`} sku={item.sku} />
                        </TableCell>
                        <TableCell>
                          {item.produtoNome}{" "}
                          <span className="text-muted-foreground font-mono text-xs font-semibold uppercase">
                            {item.unidadeMedidaSigla}
                          </span>
                        </TableCell>
                        <TableCell className="text-right">
                          <Input
                            {...navigationCell({
                              grid: "balanco-itens",
                              row: index,
                              column: 0,
                            })}
                            readOnly
                            aria-label={`Sistema de ${item.sku}`}
                            value={fmt(item.quantidadeSistema)}
                            inputSize="full"
                            className="text-right"
                          />
                        </TableCell>
                        <TableCell className="text-right">
                          <NumberInput
                            {...navigationCell({
                              grid: "balanco-itens",
                              row: index,
                              column: 1,
                            })}
                            inputSize="full"
                            value={item.quantidadeContada}
                            decimals={item.permiteDecimais ? 4 : 0}
                            inputMode={
                              item.permiteDecimais ? "decimal" : "numeric"
                            }
                            readOnly={readOnly}
                            onNumberChange={(num) =>
                              atualizarContagem(index, num)
                            }
                            className="text-right font-bold"
                          />
                        </TableCell>
                        <TableCell className="text-right">
                          <Input
                            {...navigationCell({
                              grid: "balanco-itens",
                              row: index,
                              column: 2,
                            })}
                            inputSize="full"
                            readOnly
                            aria-label={`Diferença de ${item.sku}`}
                            value={`${diferenca > 0 ? "+" : ""}${fmt(diferenca)}`}
                            className={cn(
                              "text-right font-bold",
                              diferenca > 0 && "text-success",
                              diferenca < 0 && "text-destructive",
                            )}
                          />
                        </TableCell>
                        <TableCell>
                          {diferenca !== 0 && (
                            <MotivoMovimentacaoSelect
                              name={`balanco-motivo-${item.sku}`}
                              label={`Motivo de ${item.sku}`}
                              compacto
                              opcoes={motivosDaDivergencia(diferenca)}
                              value={item.motivoDivergencia}
                              onChange={(motivo) => atualizarItem(index, { motivoDivergencia: motivo })}
                              disabled={readOnly}
                              error={semMotivo(item, diferenca) ? "Informe o motivo." : undefined}
                              triggerProps={navigationCell({ grid: "balanco-itens", row: index, column: 3 })}
                            />
                          )}
                        </TableCell>
                        <TableCell className="text-center">
                          {diferenca > 0 && (
                            <Checkbox
                              {...navigationCell({ grid: "balanco-itens", row: index, column: 4 })}
                              aria-label={`Recontado ${item.sku}`}
                              checked={item.recontado}
                              disabled={readOnly}
                              onCheckedChange={(marcado) => atualizarItem(index, { recontado: marcado === true })}
                            />
                          )}
                        </TableCell>
                        {!readOnly && (
                          <TableCell className="text-center">
                            <Button
                              {...navigationCell({
                                grid: "balanco-itens",
                                row: index,
                                column: 5,
                              })}
                              type="button"
                              variant="outline"
                              size="icon-sm"
                              aria-label="Remover Item"
                              onClick={() => void removerItem(index)}
                            >
                              <Trash2 className="size-4" />
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
