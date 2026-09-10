"use client";

import { useState, useRef, useMemo, useEffect, useCallback } from "react";
import { useForm, useStore } from "@tanstack/react-form";
import { useQuery } from "@tanstack/react-query";
import { cn } from "@/lib/utils";
import { Button } from "@/ui/primitives";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { Field, FieldLabel, FieldError } from "@/ui/primitives";
import { NumberInput } from "@/ui/composites";
import { DatePicker } from "@/ui/composites";
import { Textarea } from "@/ui/primitives";
import { Card, CardContent } from "@/ui/primitives";
import { Separator } from "@/ui/primitives";
import { Alert, AlertDescription } from "@/ui/primitives";
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from "@/ui/primitives";
import { ScrollArea } from "@/ui/primitives";
import { ClienteInput } from "@/components/entity-inputs/cliente-input";
import { EmitenteInput } from "@/components/entity-inputs/emitente-input";
import { CondicaoPagamentoInput } from "@/components/entity-inputs/condicao-pagamento-input";
import { SkuInput } from "@/components/entity-inputs/sku-input";
import {
  useUpsertMutation,
  type BackendResult,
} from "@/hooks/use-upsert-mutation";
import { vendasApi } from "@/api/vendas";
import { getFullSkuName, Sku } from "@/features/catalogo/skus/types";
import { Cliente } from "@/features/parceiros/clientes/types";
import { Emitente } from "@/features/parceiros/emitentes/types";
import { CondicaoPagamento } from "@/features/financeiro/condicoes/types";
import {
  vendaSchema,
  type Venda,
  type VendaItem,
  type VendaFormValues,
} from "./types";
import { Trash2 } from "lucide-react";
import { useWindow, useWindowCommands, useUi } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface VendasUpsertProps {
  editingItem: Venda | null;
  readOnly?: boolean;
}

export function VendasUpsertForm({
  editingItem,
  readOnly = false,
}: VendasUpsertProps) {
  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["vendas", "detail", editingItem?.id],
    queryFn: () => vendasApi.getById(editingItem!.id),
    enabled: !!editingItem,
  });

  if (editingItem && isLoading) {
    return <div className="flex min-h-48 items-center justify-center"><Spinner className="size-6" /></div>;
  }

  return <VendasFormBody editingItem={fullItem ?? editingItem} readOnly={readOnly} />;
}

interface VendasFormBodyProps {
  editingItem: Venda | null;
  readOnly: boolean;
}

export interface VendasCheckoutProps {
  totalItensCount: number;
  subtotalGross: number;
  totalDiscount: number;
  totalNet: number;
  initialCondicao: CondicaoPagamento | null;
  initialObservacao: string;
}

export interface VendasCheckoutResult {
  condicao: CondicaoPagamento;
  observacao: string;
}

function VendasCheckout({
  totalItensCount,
  subtotalGross,
  totalDiscount,
  totalNet,
  initialCondicao,
  initialObservacao,
}: VendasCheckoutProps) {
  const activeWindow = useWindow<VendasCheckoutResult>();
  const [condicao, setCondicao] = useState<CondicaoPagamento | null>(initialCondicao);
  const [observacao, setObservacao] = useState(initialObservacao);

  const finish = useCallback(() => {
    if (!condicao) return;
    activeWindow.resolve({ condicao, observacao });
  }, [activeWindow, condicao, observacao]);

  useWindowCommands(
    useMemo(
      () => [{
        id: "vendas.checkout.confirm",
        hotkey: "Alt+Enter" as const,
        label: "Finalizar venda",
        enabled: !!condicao,
        run: (event: KeyboardEvent) => {
          event.preventDefault();
          finish();
        },
      }],
      [condicao, finish],
    ),
  );

  useEffect(() => {
    activeWindow.setDirty(!!condicao || observacao.length > 0);
    return () => activeWindow.setDirty(false);
  }, [activeWindow, condicao, observacao]);

  return (
    <div className="flex flex-col gap-5 py-2">
      <div className="flex flex-col gap-2.5 rounded-lg border p-4">
        <div className="flex items-center justify-between"><span className="text-muted-foreground font-medium">Itens no Carrinho:</span><span className="font-semibold">{totalItensCount}</span></div>
        <div className="flex items-center justify-between"><span className="text-muted-foreground font-medium">Subtotal</span><span className="font-semibold">{formatCurrency(subtotalGross)}</span></div>
        <div className="flex items-center justify-between text-red-500"><span className="font-medium">Descontos:</span><span className="font-bold">-{formatCurrency(totalDiscount)}</span></div>
        <Separator />
        <div className="flex items-center justify-between font-bold text-emerald-600"><span>Total:</span><span className="text-lg">{formatCurrency(totalNet)}</span></div>
      </div>
      <div className="w-full">
        <CondicaoPagamentoInput
          name="checkoutCondicaoId"
          label="Método de Pagamento"
          initialItem={condicao}
          onSelectItem={setCondicao}
          onSelectId={() => {}}
          error={!condicao ? "Selecione o método/condição de pagamento." : undefined}
        />
      </div>
      <div className="w-full">
        <Field>
          <FieldLabel htmlFor="venda-checkout-observacao">Observação da Venda</FieldLabel>
          <Textarea
            id="venda-checkout-observacao"
            value={observacao}
            onChange={(event) => setObservacao(event.target.value)}
            placeholder="Informações adicionais da venda..."
            rows={2}
          />
        </Field>
      </div>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="outline" onClick={() => activeWindow.dismiss("cancel")}>Voltar <Kbd>Esc</Kbd></Button>
        <Button type="button" onClick={finish} disabled={!condicao}>Concluir <KbdGroup><Kbd>Alt</Kbd><Kbd>Enter</Kbd></KbdGroup></Button>
      </div>
    </div>
  );
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value);
}

function VendasFormBody({ editingItem, readOnly }: VendasFormBodyProps) {
  const activeWindow = useWindow<true>();
  const ui = useUi();
  const {
    mutation,
    globalError,
    getFieldError: originalGetFieldError,
    resetErrors,
  } = useUpsertMutation<VendaFormValues, BackendResult<Venda>>({
    mutationFn: async (value) => {
      const now = new Date();
      const [year, month, day] = value.dataVenda.split("-").map(Number);
      const saleDate = new Date(
        year,
        month - 1,
        day,
        now.getHours(),
        now.getMinutes(),
        now.getSeconds(),
      );
      const payload = {
        ...value,
        dataVenda: saleDate.toISOString(),
        parcelas: value.parcelas?.map((p) => ({
          ...p,
          dataVencimento: new Date(
            p.dataVencimento + "T12:00:00",
          ).toISOString(),
        })),
      };
      return await vendasApi.create(payload);
    },
    queryKey: ["vendas"],
    onSuccessCallback: () => activeWindow.resolve(true),
  });

interface ParcelaPreview {
  numeroParcela: number;
  dataVencimento: string;
  valorParcela: number;
}

  const [itens, setItens] = useState<VendaItem[]>(() => {
    if (!editingItem || !editingItem.itens) return [];
    return editingItem.itens.map((i) => {
      const itemGross = Number(i.quantidade) * Number(i.valorUnitario);
      const pctDesconto =
        itemGross > 0 ? (Number(i.valorDesconto) / itemGross) * 100 : 0;
      const basePrice = Number(i.valorUnitario);
      const qty = Number(i.quantidade);
      const discTotal = Number(i.valorDesconto);
      const finalPrice = basePrice - (qty > 0 ? discTotal / qty : 0);
      return {
        sku: i.sku.sku,
        produtoNome: getFullSkuName(i.sku) || i.sku.sku,
        quantidade: qty,
        valorUnitario: basePrice,
        precoFinal: parseFloat(finalPrice.toFixed(2)),
        percentualDesconto: parseFloat(pctDesconto.toFixed(2)),
        valorDesconto: discTotal,
        valorTotal: Number(i.valorTotal),
        unidadeMedidaSigla: i.sku.produto?.unidadeMedida?.sigla ?? "UN",
        permiteDecimais: !!i.sku.produto?.unidadeMedida?.permiteDecimais,
        estoqueAtual: Number(i.sku.estoque) + qty,
      };
    });
  });

  const [cliente, setCliente] = useState<Cliente | null>(
    editingItem?.cliente ?? null,
  );
  const [emitente, setEmitente] = useState<Emitente | null>(
    editingItem?.emitente ?? null,
  );
  const [localErrors, setLocalErrors] = useState<Record<string, string>>({});
  const [skuInputKey, setSkuInputKey] = useState(0);

  const [dataVenda, setDataVenda] = useState(() =>
    editingItem?.dataVenda
      ? editingItem.dataVenda.split("T")[0]
      : new Date().toISOString().split("T")[0],
  );

  const skuInputRef = useRef<HTMLInputElement>(null);

  const totalItensCount = itens.reduce((sum, i) => sum + i.quantidade, 0);
  const subtotalGross = itens.reduce(
    (sum, i) => sum + i.quantidade * i.valorUnitario,
    0,
  );
  const totalDiscount = itens.reduce((sum, i) => sum + i.valorDesconto, 0);
  const totalNet = Math.max(0, subtotalGross - totalDiscount);

  const activeEmitente = emitente;

  const form = useForm({
    defaultValues: {
      dataVenda: dataVenda,
      observacao: editingItem?.observacao ?? "",
    } as VendaFormValues,
    onSubmit: async () => {
      resetErrors();
      setLocalErrors({});

      if (!cliente) {
        setLocalErrors((prev) => ({
          ...prev,
          clienteId: "Cliente é obrigatório.",
        }));
        return;
      }
      if (!activeEmitente) {
        setLocalErrors((prev) => ({
          ...prev,
          emitenteId: "Emitente é obrigatório.",
        }));
        return;
      }
      if (itens.length === 0) {
        ui.feedback.notify({ type: "error", title: "A venda deve conter ao menos um item." });
        return;
      }

      let hasInvalidQty = false;
      itens.forEach((item) => {
        if (item.quantidade <= 0) {
          hasInvalidQty = true;
          ui.feedback.notify({ type: "error", title: `Quantidade do SKU "${item.sku}" deve ser maior que zero.` });
        }
      });
      if (hasInvalidQty) return;

      let hasStockShortage = false;
      itens.forEach((item) => {
        if (item.quantidade > item.estoqueAtual) {
          hasStockShortage = true;
          ui.feedback.notify({ type: "error", title: `Estoque insuficiente para o SKU "${item.sku}".` });
        }
      });
      if (hasStockShortage) return;

      const checkout = await ui.windows.open<VendasCheckoutResult, VendasCheckoutProps>({
        component: VendasCheckout,
        props: {
          totalItensCount,
          subtotalGross,
          totalDiscount,
          totalNet,
          initialCondicao: null,
          initialObservacao: form.getFieldValue("observacao") || "",
        },
        title: "Finalizar Venda",
        size: "medium",
      });
      if (checkout.status === "confirmed") await handleFinalSubmit(checkout.value);
    },
  });

  const handleFinalSubmit = async (checkout: VendasCheckoutResult) => {
    resetErrors();
    setLocalErrors({});

    const payload = {
      dataVenda: dataVenda,
      clienteId: cliente?.id ?? 0,
      emitenteId: activeEmitente?.id ?? 0,
      observacao: form.getFieldValue("observacao") || "",
      condicaoPagamentoId: checkout.condicao.id,
      itens: itens.map((i) => ({
        sku: i.sku,
        quantidade: i.quantidade,
        valorUnitario: i.valorUnitario,
        percentualDesconto: i.percentualDesconto,
        valorDesconto: i.valorDesconto,
        valorTotal: i.valorTotal,
        produtoNome: i.produtoNome,
        unidadeMedidaSigla: i.unidadeMedidaSigla,
        permiteDecimais: i.permiteDecimais,
        estoqueAtual: i.estoqueAtual,
      })),
      parcelas: buildParcelas(checkout.condicao),
    };

    const validationResult = vendaSchema.safeParse(payload);
    if (!validationResult.success) {
      validationResult.error.errors.forEach((err) => {
        ui.feedback.notify({ type: "error", title: err.message });
      });
      return;
    }

    await mutation.mutateAsync({
      ...(payload as VendaFormValues),
      observacao: checkout.observacao,
    });
  };

  const commands = [
      {
        id: "vendas.focus-sku",
        hotkey: "Alt+K" as const,
        label: "Adicionar SKU",
        enabled: !readOnly,
        run: (event: KeyboardEvent) => {
          event.preventDefault();
          skuInputRef.current?.focus();
        },
      },
      {
        id: "vendas.submit",
        hotkey: "Alt+Enter" as const,
        label: "Avançar para finalização",
        enabled: !readOnly && !mutation.isPending,
        run: (event: KeyboardEvent) => {
          event.preventDefault();
          void form.handleSubmit();
        },
      },
  ];
  useWindowCommands(commands);

  const isDirty = useStore(form.store, (state) => state.isDirty);
  useEffect(() => {
    activeWindow.setDirty(isDirty || itens.length > 0);
    return () => activeWindow.setDirty(false);
  }, [activeWindow, isDirty, itens.length]);

  const getFieldError = (name: string, formErrors: unknown[]) => {
    return localErrors[name] || originalGetFieldError(name, formErrors);
  };

  const handleSkuAdded = (skuRes: Sku | null, qtdeAdicionada: number = 1) => {
    if (!skuRes) return;

    const existingIndex = itens.findIndex((i) => i.sku === skuRes.sku);

    if (existingIndex === -1) {
      if (qtdeAdicionada <= 0) {
        ui.feedback.notify({ type: "error", title: "Produto não está no carrinho para ser decrementado." });
        return;
      }

      const priceVal = isNaN(Number(skuRes.preco)) ? 0 : Number(skuRes.preco);
      const gross = qtdeAdicionada * priceVal;

      if (qtdeAdicionada > skuRes.estoque) {
        ui.feedback.notify({ type: "error", title: `Estoque insuficiente para o SKU "${skuRes.sku}". Disponível: ${skuRes.estoque.toFixed(skuRes.produto?.unidadeMedida?.permiteDecimais ? 4 : 0)}.` });
        return;
      }

      setItens([
        ...itens,
        {
          sku: skuRes.sku,
          produtoNome: getFullSkuName(skuRes),
          quantidade: qtdeAdicionada,
          valorUnitario: priceVal,
          precoFinal: priceVal,
          percentualDesconto: 0,
          valorDesconto: 0,
          valorTotal: gross,
          unidadeMedidaSigla: skuRes.produto?.unidadeMedida?.sigla ?? "UN",
          permiteDecimais: !!skuRes.produto?.unidadeMedida?.permiteDecimais,
          estoqueAtual: Number(skuRes.estoque),
        },
      ]);
      ui.feedback.notify({ type: "success", title: `SKU "${skuRes.sku}" adicionado com sucesso.` });
    } else {
      const currentQty = itens[existingIndex].quantidade;
      const newQty = currentQty + qtdeAdicionada;

      if (newQty <= 0) {
        void handleRemoveItem(existingIndex);
        return;
      }

      if (newQty > skuRes.estoque) {
        ui.feedback.notify({ type: "error", title: `Estoque insuficiente para o SKU "${skuRes.sku}". Disponível: ${skuRes.estoque.toFixed(skuRes.produto?.unidadeMedida?.permiteDecimais ? 4 : 0)}.` });
        return;
      }

      const updated = [...itens];
      updated[existingIndex].quantidade = newQty;
      const item = updated[existingIndex];
      const gross = newQty * item.valorUnitario;
      item.valorTotal = parseFloat((newQty * item.precoFinal).toFixed(2));
      item.valorDesconto = parseFloat((gross - item.valorTotal).toFixed(2));
      setItens(updated);

      const acao = qtdeAdicionada >= 0 ? "alterada" : "decrementada";
      ui.feedback.notify({ type: "success", title: `Quantidade do SKU "${skuRes.sku}" ${acao} para ${newQty}.` });
    }

    setSkuInputKey((prev) => prev + 1);
    skuInputRef.current?.focus();
  };

  const handleRemoveItem = async (index: number) => {
    if (readOnly) return;
    const itemToRemove = itens[index];
    if (!itemToRemove) return;
    const result = await ui.windows.confirm({
      title: "Remover Item?",
      description: `Deseja realmente remover o SKU ${itemToRemove.sku} desta venda?`,
      confirmLabel: "Remover Item",
      confirmVariant: "destructive",
    });
    if (!result) return;
    setItens(itens.filter((_, i) => i !== index));
    ui.feedback.notify({ type: "info", title: `SKU "${itemToRemove.sku}" removido.` });
  };

  const updateItemRow = (
    index: number,
    key: "quantidade" | "percentualDesconto" | "valorUnitario" | "precoFinal",
    value: number,
  ) => {
    const updated = [...itens];
    const item = updated[index];

    if (key === "quantidade") {
      item.quantidade = value;
    } else if (key === "percentualDesconto") {
      const discPercent = Math.min(100, Math.max(0, value));
      item.percentualDesconto = discPercent;
      item.precoFinal = parseFloat(
        (item.valorUnitario * (1 - discPercent / 100)).toFixed(2),
      );
    } else if (key === "valorUnitario") {
      item.valorUnitario = value;
      item.precoFinal = parseFloat(
        (value * (1 - item.percentualDesconto / 100)).toFixed(2),
      );
    } else if (key === "precoFinal") {
      const finalPrice = Math.min(item.valorUnitario, Math.max(0, value));
      item.precoFinal = finalPrice;
      item.percentualDesconto =
        item.valorUnitario > 0
          ? parseFloat(
              (
                ((item.valorUnitario - finalPrice) / item.valorUnitario) *
                100
              ).toFixed(2),
            )
          : 0;
    }

    const gross = item.quantidade * item.valorUnitario;
    item.valorTotal = parseFloat(
      (item.quantidade * item.precoFinal).toFixed(2),
    );
    item.valorDesconto = parseFloat((gross - item.valorTotal).toFixed(2));
    setItens(updated);
  };

  const buildParcelas = (payment: CondicaoPagamento): ParcelaPreview[] => {
    if (totalNet <= 0) return [];
    const baseDate = dataVenda ? new Date(dataVenda + "T12:00:00") : new Date();
    const result: ParcelaPreview[] = [];
    let count = 1;
    const entradaPercent = payment.entradaMinimaPercentual ?? 0;
    if (entradaPercent > 0) {
      result.push({ numeroParcela: count++, dataVencimento: baseDate.toISOString().split("T")[0], valorParcela: parseFloat((totalNet * (entradaPercent / 100)).toFixed(2)) });
    }
    (payment.condicoesPagamentosParcelas || []).forEach((item) => {
      const venc = new Date(baseDate);
      venc.setDate(baseDate.getDate() + item.prazoDias);
      result.push({ numeroParcela: count++, dataVencimento: venc.toISOString().split("T")[0], valorParcela: parseFloat((totalNet * (item.percentual / 100)).toFixed(2)) });
    });
    if (result.length) {
      const difference = totalNet - result.reduce((sum, item) => sum + item.valorParcela, 0);
      result[result.length - 1].valorParcela = parseFloat((result[result.length - 1].valorParcela + difference).toFixed(2));
    }
    return result;
  };

  return (
    <div className="flex flex-col gap-4">
      <div data-window-actions className="flex justify-end gap-2 border-b pb-4">
        <Button
          type="button"
          variant="outline"
          onClick={() => activeWindow.dismiss("cancel")}
        >
          Cancelar <Kbd>Esc</Kbd>
        </Button>
        {!readOnly && (
          <Button type="submit" form="upsert-venda" disabled={mutation.isPending}>
            {mutation.isPending ? "Salvando..." : (
              <span className="flex items-center gap-2">
                Ir para Finalização
                <KbdGroup><Kbd>Alt</Kbd><Kbd>Enter</Kbd></KbdGroup>
              </span>
            )}
          </Button>
        )}
      </div>
      <form
        id="upsert-venda"
        className="flex h-full flex-col gap-4"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        {editingItem?.dataCancelamento && (
          <Alert variant="destructive" className="mb-2">
            <AlertDescription className="text-sm font-semibold">
              Venda Cancelada em{" "}
              {new Date(editingItem.dataCancelamento).toLocaleDateString(
                "pt-BR",
              )}{" "}
              às{" "}
              {new Date(editingItem.dataCancelamento).toLocaleTimeString(
                "pt-BR",
              )}
              .<br />
              Motivo: {editingItem.motivoCancelamento ?? "-"}
            </AlertDescription>
          </Alert>
        )}

        {globalError && (
          <Alert variant="destructive">
            <AlertDescription>{globalError}</AlertDescription>
          </Alert>
        )}

        <div className="flex h-full w-full flex-col gap-4">
          <div className="flex w-full flex-row gap-2">
            <div className="w-[20%]">
              <form.Field name="dataVenda">
                {(field) => (
                  <Field
                    data-invalid={
                      !!getFieldError(field.name, field.state.meta.errors)
                    }
                  >
                    <FieldLabel htmlFor={field.name}>Data da Venda</FieldLabel>
                    <DatePicker
                      id={field.name}
                      name={field.name}
                      value={dataVenda}
                      onChange={(val) => {
                        const newVal =
                          val || new Date().toISOString().split("T")[0];
                        setDataVenda(newVal);
                        field.handleChange(newVal);
                      }}
                      disabled={readOnly}
                    />
                    {getFieldError(field.name, field.state.meta.errors) && (
                      <FieldError>
                        {getFieldError(field.name, field.state.meta.errors)}
                      </FieldError>
                    )}
                  </Field>
                )}
              </form.Field>
            </div>

            <div className="w-[80%]">
              <EmitenteInput
                name="emitenteId"
                initialItem={emitente}
                onSelectItem={(e) => {
                  setEmitente(e);
                  setLocalErrors((prev) => {
                    const copy = { ...prev };
                    delete copy.emitenteId;
                    return copy;
                  });
                }}
                onSelectId={() => {}}
                disabled={readOnly}
                error={localErrors["emitenteId"]}
              />
            </div>
          </div>

          <div className="w-full">
            <ClienteInput
              name="clienteId"
              initialItem={cliente}
              onSelectItem={(c) => {
                setCliente(c);
                setLocalErrors((prev) => {
                  const copy = { ...prev };
                  delete copy.clienteId;
                  return copy;
                });
              }}
              onSelectId={() => {}}
              disabled={readOnly}
              error={localErrors["clienteId"]}
            />
          </div>

          {!readOnly && (
            <div className="w-full">
              <SkuInput
                ref={skuInputRef}
                key={skuInputKey}
                name="add-sku-pos"
                label="Inserir Produto"
                onSelectSku={handleSkuAdded}
              />
            </div>
          )}

          <div className="h-full w-full flex-1">
            <Card className="flex h-full flex-1 flex-col p-0 shadow-sm">
              <CardContent className="flex h-full flex-1 flex-col p-0">
                <ScrollArea className="h-full w-full">
                  <Table className="min-w-250">
                    <TableHeader className="bg-muted border-b">
                      <TableRow className="border-b hover:bg-transparent">
                        <TableHead className="w-28 px-4 text-left">
                          SKU
                        </TableHead>
                        <TableHead className="w-full px-4 text-left">
                          Produto
                        </TableHead>
                        <TableHead className="w-24 px-4 text-right">
                          Quantidade
                        </TableHead>
                        <TableHead className="w-28 px-4 text-right">
                          Preço Base
                        </TableHead>
                        <TableHead className="w-28 px-4 text-right">
                          Desconto (%)
                        </TableHead>
                        <TableHead className="w-28 px-4 text-right">
                          Preço Final
                        </TableHead>
                        <TableHead className="w-28 px-4 text-right">
                          Total
                        </TableHead>
                        {!readOnly && (
                          <TableHead className="w-12 px-4 text-center"></TableHead>
                        )}
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {itens.length != 0 &&
                        itens.map((item, index) => (
                          <TableRow key={item.sku} className="hover:/30">
                            <TableCell className="px-4 py-1 font-mono text-xs font-medium">
                              {item.sku}
                            </TableCell>
                            <TableCell className="px-4 py-1 text-xs font-medium">
                              {item.produtoNome}
                            </TableCell>

                            <TableCell className="px-4 py-1 text-right">
                              <div className="flex flex-col items-end">
                                <NumberInput
                                  value={item.quantidade}
                                  decimals={item.permiteDecimais ? 4 : 0}
                                  inputSize="full"
                                  inputMode={
                                    item.permiteDecimais ? "decimal" : "numeric"
                                  }
                                  aria-invalid={
                                    item.quantidade <= 0 ||
                                    item.quantidade > item.estoqueAtual
                                  }
                                  className={cn(
                                    "h-7 text-right text-xs font-semibold",
                                    (item.quantidade <= 0 ||
                                      item.quantidade > item.estoqueAtual) &&
                                      "border-destructive focus-visible:ring-destructive",
                                  )}
                                  disabled={readOnly}
                                  onNumberChange={(val) =>
                                    updateItemRow(index, "quantidade", val)
                                  }
                                />
                                {(item.quantidade <= 0 ||
                                  item.quantidade > item.estoqueAtual) && (
                                  <span className="text-destructive text-2xs mt-0.5 text-right font-semibold whitespace-nowrap">
                                    {item.quantidade <= 0
                                      ? "Mín: >0"
                                      : `Estoque: ${item.estoqueAtual.toFixed(item.permiteDecimais ? 4 : 0)}`}
                                  </span>
                                )}
                              </div>
                            </TableCell>

                            <TableCell className="px-4 py-1 text-right">
                              <div className="flex flex-col items-end">
                                <NumberInput
                                  value={item.valorUnitario}
                                  decimals={2}
                                  inputSize="full"
                                  inputMode="decimal"
                                  aria-invalid={item.valorUnitario < 0}
                                  className={cn(
                                    "h-7 text-right text-xs font-semibold",
                                    item.valorUnitario < 0 &&
                                      "border-destructive focus-visible:ring-destructive",
                                  )}
                                  disabled={readOnly}
                                  onNumberChange={(val) =>
                                    updateItemRow(index, "valorUnitario", val)
                                  }
                                />
                                {item.valorUnitario < 0 && (
                                  <span className="text-destructive text-2xs mt-0.5 text-right font-semibold whitespace-nowrap">
                                    Mín: 0
                                  </span>
                                )}
                              </div>
                            </TableCell>

                            <TableCell className="px-4 py-1 text-right">
                              <div className="flex flex-col items-end">
                                <NumberInput
                                  value={item.percentualDesconto}
                                  decimals={2}
                                  inputSize="full"
                                  inputMode="decimal"
                                  aria-invalid={
                                    item.percentualDesconto < 0 ||
                                    item.percentualDesconto > 100
                                  }
                                  className={cn(
                                    "h-7 text-right text-xs font-semibold text-red-500",
                                    item.percentualDesconto < 0 ||
                                      item.percentualDesconto > 100
                                      ? "border-destructive focus-visible:ring-destructive"
                                      : "border-red-200 focus-visible:ring-red-500",
                                  )}
                                  disabled={readOnly}
                                  onNumberChange={(val) =>
                                    updateItemRow(
                                      index,
                                      "percentualDesconto",
                                      val,
                                    )
                                  }
                                />
                                {(item.percentualDesconto < 0 ||
                                  item.percentualDesconto > 100) && (
                                  <span className="text-destructive text-2xs mt-0.5 text-right font-semibold whitespace-nowrap">
                                    0% a 100%
                                  </span>
                                )}
                              </div>
                            </TableCell>

                            <TableCell className="px-4 py-1 text-right">
                              <div className="flex flex-col items-end">
                                <NumberInput
                                  value={item.precoFinal}
                                  decimals={2}
                                  inputSize="full"
                                  inputMode="decimal"
                                  aria-invalid={
                                    item.precoFinal < 0 ||
                                    item.precoFinal > item.valorUnitario
                                  }
                                  className={cn(
                                    "h-7 text-right text-xs font-semibold",
                                    (item.precoFinal < 0 ||
                                      item.precoFinal > item.valorUnitario) &&
                                      "border-destructive focus-visible:ring-destructive",
                                  )}
                                  disabled={readOnly}
                                  onNumberChange={(val) =>
                                    updateItemRow(index, "precoFinal", val)
                                  }
                                />
                                {(item.precoFinal < 0 ||
                                  item.precoFinal > item.valorUnitario) && (
                                  <span className="text-destructive text-2xs mt-0.5 text-right font-semibold whitespace-nowrap">
                                    {item.precoFinal < 0
                                      ? "Mín: 0"
                                      : "Excede base"}
                                  </span>
                                )}
                              </div>
                            </TableCell>

                            <TableCell className="px-4 py-1 text-right text-xs font-bold">
                              {new Intl.NumberFormat("pt-BR", {
                                style: "currency",
                                currency: "BRL",
                              }).format(item.valorTotal)}
                            </TableCell>

                            {!readOnly && (
                              <TableCell className="px-4 py-1 text-center">
                                <Button
                                  size="icon-xs"
                                  variant="ghost"
                                  className="text-red-500 hover:bg-red-50 hover:text-red-600"
                                  onClick={() => handleRemoveItem(index)}
                                >
                                  <Trash2 className="size-3.5" />
                                </Button>
                              </TableCell>
                            )}
                          </TableRow>
                        ))}
                    </TableBody>
                  </Table>
                </ScrollArea>

                <div className="/50 /10 grid grid-cols-1 divide-y border-t md:grid-cols-4 md:divide-x md:divide-y-0">
                  <div className="flex flex-col px-4 py-2 text-end">
                    <span className="text-2xs text-muted-foreground font-semibold tracking-wider uppercase">
                      Itens Totais
                    </span>
                    <span className="text-lg font-bold">{totalItensCount}</span>
                  </div>
                  <div className="flex flex-col px-4 py-2 text-end">
                    <span className="text-2xs text-muted-foreground font-semibold tracking-wider uppercase">
                      Subtotal
                    </span>
                    <span className="text-lg font-bold">
                      {new Intl.NumberFormat("pt-BR", {
                        style: "currency",
                        currency: "BRL",
                      }).format(subtotalGross)}
                    </span>
                  </div>
                  <div className="flex flex-col px-4 py-2 text-end">
                    <span className="text-2xs font-semibold tracking-wider text-red-500 uppercase">
                      Desconto Total
                    </span>
                    <span className="text-lg font-bold text-red-500">
                      {new Intl.NumberFormat("pt-BR", {
                        style: "currency",
                        currency: "BRL",
                      }).format(totalDiscount)}
                    </span>
                  </div>
                  <div className="flex flex-col bg-emerald-500/5 px-4 py-2 text-end">
                    <span className="text-2xs font-bold tracking-wider text-emerald-600 uppercase">
                      Total Líquido
                    </span>
                    <span className="text-lg font-bold text-emerald-600">
                      {new Intl.NumberFormat("pt-BR", {
                        style: "currency",
                        currency: "BRL",
                      }).format(totalNet)}
                    </span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>

          {readOnly && (
            <div className="w-full">
              <Card className="shadow-sm">
                <CardContent className="flex flex-col gap-2">
                  <form.Field name="observacao">
                    {(field) => (
                      <Field>
                        <FieldLabel htmlFor={field.name}>
                          Observação da Venda
                        </FieldLabel>
                        <Textarea
                          id={field.name}
                          name={field.name}
                          value={field.state.value ?? ""}
                          disabled={true}
                          rows={2}
                        />
                      </Field>
                    )}
                  </form.Field>
                </CardContent>
              </Card>
            </div>
          )}
        </div>
      </form>
    </div>
  );
}
