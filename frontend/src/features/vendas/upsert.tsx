"use client";

import type React from "react";
import { useState, useRef, useMemo, useCallback } from "react";
import { WindowActions } from "@/ui/imperative";
import { useForm, useStore } from "@tanstack/react-form";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { FilePlus2 } from "lucide-react";
import { nfesApi } from "@/api/nfes";
import { abrirNfe } from "@/features/nfes";
import { ApiRequestError } from "@/utils/api-error";
import { cn } from "@/lib/utils";
import { Button } from "@/ui/primitives";
import { HotkeyKbd, Kbd, KbdGroup } from "@/ui/primitives";
import { Field, FieldLabel, FieldError } from "@/ui/primitives";
import { NumberInput } from "@/ui/composites";
import { DatePicker } from "@/ui/composites";
import { Textarea } from "@/ui/primitives";
import { Card, CardContent } from "@/ui/primitives";
import { Separator } from "@/ui/primitives";
import { TextoWindow, type TextoWindowProps } from "@/components/texto-window";
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from "@/ui/primitives";
import { ClienteInput } from "@/components/entity-inputs/cliente-input";
import { EmitenteInput } from "@/components/entity-inputs/emitente-input";
import { CondicaoPagamentoInput } from "@/components/entity-inputs/condicao-pagamento-input";
import { SkuViewInput } from "@/components/entity-inputs/sku-view-input";
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
import { Trash2, Boxes, Receipt, FileText, Undo2 } from "lucide-react";
import { DevolucoesHistoricoWindow, type DevolucoesHistoricoWindowProps } from "./devolucao";
import { estoqueApi } from "@/api/estoque";
import { ContasReceberUpsertForm } from "@/features/financeiro/contas-receber/upsert";
import { useRelated, mapItens } from "@/hooks/use-related";
import { contasReceberApi } from "@/api/financeiro";
import { MovimentacoesUpsert } from "@/features/estoque/movimentacoes/upsert";
import { useWindow, useWindowCommands, useUi, WindowCancelButton } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";
import { navigationCell } from "@/ui/keyboard-navigation";
import { toLocalISODate, todayLocalISODate } from "@/utils/date-utils";

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
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );
  }

  return (
    <VendasFormBody editingItem={fullItem ?? editingItem} readOnly={readOnly} />
  );
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
  const [condicao, setCondicao] = useState<CondicaoPagamento | null>(
    initialCondicao,
  );
  const [valorRecebido, setValorRecebido] = useState(0);
  const [condicaoTocada, setCondicaoTocada] = useState(false);

  const isDinheiro = condicao?.metodoPagamento?.permiteTroco ?? false;
  const recebidoInsuficiente = isDinheiro && valorRecebido + 0.005 < totalNet;
  const troco = isDinheiro ? Math.max(0, valorRecebido - totalNet) : 0;
  const canFinish = !!condicao && totalNet > 0 && !recebidoInsuficiente;

  // Campo não controlado: digitar não re-renderiza o checkout; a observação é
  // lida ao finalizar e ao consultar se há alterações não salvas.
  const observacaoRef = useRef<HTMLTextAreaElement | null>(null);

  const finish = useCallback(() => {
    if (!condicao || totalNet <= 0 || recebidoInsuficiente) return;
    activeWindow.resolve({
      condicao,
      observacao: observacaoRef.current?.value ?? "",
    });
  }, [activeWindow, condicao, totalNet, recebidoInsuficiente]);

  useWindowCommands(
    useMemo(
      () => [
        {
          id: "vendas.checkout.confirm",
          hotkey: "Alt+Enter" as const,
          label: "Finalizar venda",
          enabled: canFinish,
          run: (event: KeyboardEvent) => {
            event.preventDefault();
            finish();
          },
        },
      ],
      [canFinish, finish],
    ),
  );

  const registerDirty = useCallback(
    (element: HTMLDivElement | null) => {
      if (!element) return;
      activeWindow.setDirtyCheck(
        () => !!condicao || (observacaoRef.current?.value.length ?? 0) > 0,
      );
      return () => activeWindow.setDirtyCheck(null);
    },
    [activeWindow, condicao],
  );

  return (
    <div ref={registerDirty} className="flex flex-col gap-5 py-2">
      <div className="flex flex-col gap-2.5 rounded-lg border p-4">
        <div className="flex items-center justify-between">
          <span className="text-muted-foreground font-medium">
            Itens no Carrinho:
          </span>
          <span className="font-semibold">{totalItensCount}</span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-muted-foreground font-medium">Subtotal</span>
          <span className="font-semibold">{formatCurrency(subtotalGross)}</span>
        </div>
        <div className="flex items-center justify-between text-destructive">
          <span className="font-medium">Descontos:</span>
          <span className="font-bold">{formatCurrency(-totalDiscount)}</span>
        </div>
        <Separator />
        <div className="flex items-center justify-between font-bold text-success">
          <span>Total:</span>
          <span className="text-lg">{formatCurrency(totalNet)}</span>
        </div>
      </div>
      <div className="w-full">
        <CondicaoPagamentoInput
          name="checkoutCondicaoId"
          label="Método de Pagamento"
          initialItem={condicao}
          onSelectItem={(item) => {
            setCondicao(item);
            setCondicaoTocada(true);
          }}
          onSelectId={() => {}}
          error={
            totalNet <= 0
              ? "Venda com total zerado não pode ser finalizada: o sistema exige ao menos uma parcela. Ajuste os descontos."
              : !condicao && condicaoTocada
                ? "Selecione o método/condição de pagamento."
                : undefined
          }
        />
      </div>
      {isDinheiro && (
        <Field data-invalid={recebidoInsuficiente}>
          <div className="flex items-center justify-between gap-4">
            <FieldLabel htmlFor="venda-checkout-valor-recebido">
              Valor recebido (R$)
            </FieldLabel>
            <NumberInput
              id="venda-checkout-valor-recebido"
              inputSize="full"
              value={valorRecebido}
              decimals={2}
              inputMode="decimal"
              onNumberChange={setValorRecebido}
              className="h-8 w-48 text-right font-semibold"
              aria-invalid={recebidoInsuficiente}
            />
          </div>
          {recebidoInsuficiente ? (
            <FieldError className="mt-1 block text-right">
              O valor recebido é menor que o total da venda.
            </FieldError>
          ) : (
            <div className="text-muted-foreground mt-1 flex justify-between text-sm">
              <span>Troco:</span>
              <span className="text-foreground font-semibold">
                {formatCurrency(troco)}
              </span>
            </div>
          )}
        </Field>
      )}
      <div className="w-full">
        <Field>
          <FieldLabel htmlFor="venda-checkout-observacao">
            Observação da Venda
          </FieldLabel>
          <Textarea
            id="venda-checkout-observacao"
            ref={observacaoRef}
            defaultValue={initialObservacao}
            maxLength={500}
            placeholder="Informações adicionais da venda..."
            rows={2}
          />
        </Field>
      </div>
      <WindowActions>
        <WindowCancelButton />
        <Button type="button" onClick={finish} disabled={!canFinish}>
          Concluir{" "}
          <KbdGroup>
            <Kbd>Alt</Kbd>
            <Kbd>Enter</Kbd>
          </KbdGroup>
        </Button>
      </WindowActions>
    </div>
  );
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
}

function VendasFormBody({ editingItem, readOnly }: VendasFormBodyProps) {
  const activeWindow = useWindow<true>();
  const ui = useUi();
  const queryClient = useQueryClient();
  const [gerandoNfe, setGerandoNfe] = useState(false);

  const gerarNfe = async () => {
    if (gerandoNfe || !editingItem) return;
    setGerandoNfe(true);
    try {
      const r = await nfesApi.emitirDeVenda(editingItem.id);
      await queryClient.invalidateQueries({ queryKey: ["nfes"] });
      ui.feedback.notify({ type: "success", title: "NF-e gerada com sucesso." });
      if (r.data) await abrirNfe(ui, r.data.id, r.data.numero);
    } catch (error) {
      const e = error instanceof ApiRequestError ? error : null;
      if (e?.status === 409) {
        // Venda já tem NF-e ativa: a mensagem do backend traz o id; leva o usuário até ela.
        ui.feedback.notify({ type: "info", title: "Esta venda já possui uma NF-e ativa." });
        const id = /NF-e (\d+)/.exec(e.message)?.[1];
        if (id) await abrirNfe(ui, Number(id));
      } else if (e?.errors.some((x) => x.code === "EMITENTE_SEM_UF")) {
        ui.feedback.notify({
          type: "error",
          title: "Emitente sem UF.",
          description: "Cadastre bairro, cidade e estado no emitente da venda antes de gerar a NF-e.",
        });
      } else {
        ui.feedback.notifyError(error, { fallbackTitle: "Não foi possível gerar a NF-e." });
      }
    } finally {
      setGerandoNfe(false);
    }
  };

  const { openList } = useRelated();
  const {
    mutation,
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

  const [initialDataVenda] = useState(() =>
    editingItem?.dataVenda
      ? editingItem.dataVenda.split("T")[0]
      : todayLocalISODate(),
  );

  const skuInputRef = useRef<HTMLInputElement>(null);

  // Adding an item intentionally remounts SkuInput (new key) to reset its
  // controlled value. The ref callback changes identity only with the key, so
  // it runs once per remount and restores focus after the DOM replacement.
  const setSkuInput = useCallback(
    (element: HTMLInputElement | null) => {
      skuInputRef.current = element;
      if (element && skuInputKey > 0) {
        requestAnimationFrame(() => element.focus());
      }
    },
    [skuInputKey],
  );

  const totalItensCount = itens.reduce((sum, i) => sum + i.quantidade, 0);
  const subtotalGross = itens.reduce(
    (sum, i) => sum + i.quantidade * i.valorUnitario,
    0,
  );
  const totalDiscount = itens.reduce((sum, i) => sum + i.valorDesconto, 0);
  const totalNet = Math.max(0, subtotalGross - totalDiscount);

  const form = useForm({
    defaultValues: {
      dataVenda: initialDataVenda,
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
      if (!emitente) {
        setLocalErrors((prev) => ({
          ...prev,
          emitenteId: "Emitente é obrigatório.",
        }));
        return;
      }
      if (itens.length === 0) {
        ui.feedback.notify({
          type: "error",
          title: "A venda deve conter ao menos um item.",
        });
        return;
      }

      let hasInvalidQty = false;
      itens.forEach((item) => {
        if (item.quantidade <= 0) {
          hasInvalidQty = true;
          ui.feedback.notify({
            type: "error",
            title: `Quantidade do SKU "${item.sku}" deve ser maior que zero.`,
          });
        }
      });
      if (hasInvalidQty) return;

      let hasStockShortage = false;
      itens.forEach((item) => {
        if (item.quantidade > item.estoqueAtual) {
          hasStockShortage = true;
          ui.feedback.notify({
            type: "error",
            title: `Estoque insuficiente para o SKU "${item.sku}".`,
          });
        }
      });
      if (hasStockShortage) return;

      const checkout = await ui.windows.open<
        VendasCheckoutResult,
        VendasCheckoutProps
      >({
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
      if (checkout.status === "confirmed")
        await handleFinalSubmit(checkout.value);
    },
  });

  const dataVenda = useStore(form.store, (state) => state.values.dataVenda);

  const handleFinalSubmit = async (checkout: VendasCheckoutResult) => {
    resetErrors();
    setLocalErrors({});

    const payload = {
      dataVenda: dataVenda,
      clienteId: cliente?.id ?? 0,
      emitenteId: emitente?.id ?? 0,
      observacao: checkout.observacao,
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

    try {
      await mutation.mutateAsync(payload as VendaFormValues);
    } catch {
      // O hook central já apresenta o erro operacional em um toast.
    }
  };

  const openMovimentacao = () =>
    openList(
      () => estoqueApi.listByOrigem("VENDA", editingItem!.id).then(mapItens((x) => ({ id: x.id, descricao: `Movimentação #${x.id}`, detalhe: x.tipoMovimentacao }))),
      estoqueApi.getById,
      MovimentacoesUpsert,
      "Visualizar Movimentação de Estoque",
      "Venda sem movimentação de estoque.",
    );

  const openContasReceber = () =>
    openList(
      () => contasReceberApi.listByOrigem("VENDA", editingItem!.id).then(mapItens((x) => ({ id: x.id, descricao: x.descricao, detalhe: x.status }))),
      contasReceberApi.getById,
      ContasReceberUpsertForm,
      "Detalhes da Conta a Receber",
      "Venda sem contas a receber.",
      false,
    );

  const openTexto = (title: string, value: string) =>
    ui.windows.open<string, TextoWindowProps>({
      component: TextoWindow,
      props: { label: title, value },
      title,
      size: "large",
    });
  const openObservacao = () => openTexto("Observação da Venda", editingItem!.observacao ?? "");
  const openDevolucoes = () =>
    ui.windows.open<void, DevolucoesHistoricoWindowProps>({
      component: DevolucoesHistoricoWindow,
      props: { vendaId: editingItem!.id },
      title: `Devoluções da Venda #${editingItem!.id}`,
      size: "large",
    });

  const commands = [
    {
      id: "vendas.gerar-nfe",
      hotkey: "Alt+G" as const,
      label: "Gerar NF-e",
      enabled: readOnly && !!editingItem,
      run: (event: KeyboardEvent) => {
        event.preventDefault();
        void gerarNfe();
      },
    },
    {
      id: "vendas.view-devolucoes",
      hotkey: "Alt+D" as const,
      label: "Visualizar devoluções",
      enabled: readOnly && !!editingItem,
      run: (event: KeyboardEvent) => {
        event.preventDefault();
        void openDevolucoes();
      },
    },
    {
      id: "vendas.view-observacao",
      hotkey: "Alt+O" as const,
      label: "Visualizar observação",
      enabled: readOnly && !!editingItem,
      run: (event: KeyboardEvent) => {
        event.preventDefault();
        void openObservacao();
      },
    },
    {
      id: "vendas.view-contas-receber",
      hotkey: "Alt+R" as const,
      label: "Visualizar contas a receber",
      enabled: readOnly && !!editingItem,
      run: (event: KeyboardEvent) => {
        event.preventDefault();
        void openContasReceber();
      },
    },
    {
      id: "vendas.view-movimentacao",
      hotkey: "Alt+M" as const,
      label: "Visualizar movimentação de estoque",
      enabled: readOnly && !!editingItem,
      run: (event: KeyboardEvent) => {
        event.preventDefault();
        void openMovimentacao();
      },
    },
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
      run: async (event: KeyboardEvent) => {
        event.preventDefault();
        await form.handleSubmit();
      },
    },
  ];
  useWindowCommands(commands);

  const getFieldError = (name: string, formErrors: unknown[]) => {
    return localErrors[name] || originalGetFieldError(name, formErrors);
  };

  const handleSkuAdded = (skuRes: Sku | null, qtdeAdicionada: number = 1) => {
    if (!skuRes) return;

    const existingIndex = itens.findIndex((i) => i.sku === skuRes.sku);

    if (existingIndex === -1) {
      if (qtdeAdicionada <= 0) {
        ui.feedback.notify({
          type: "error",
          title: "Produto não está no carrinho para ser decrementado.",
        });
        return;
      }

      const priceVal = isNaN(Number(skuRes.preco)) ? 0 : Number(skuRes.preco);
      const gross = qtdeAdicionada * priceVal;

      if (qtdeAdicionada > skuRes.estoque) {
        ui.feedback.notify({
          type: "error",
          title: `Estoque insuficiente para o SKU "${skuRes.sku}". Disponível: ${skuRes.estoque.toFixed(skuRes.produto?.unidadeMedida?.permiteDecimais ? 4 : 0)}.`,
        });
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
      ui.feedback.notify({
        type: "success",
        title: `SKU "${skuRes.sku}" adicionado com sucesso.`,
      });
    } else {
      const currentQty = itens[existingIndex].quantidade;
      const newQty = currentQty + qtdeAdicionada;

      if (newQty <= 0) {
        return handleRemoveItem(existingIndex);
      }

      if (newQty > skuRes.estoque) {
        ui.feedback.notify({
          type: "error",
          title: `Estoque insuficiente para o SKU "${skuRes.sku}". Disponível: ${skuRes.estoque.toFixed(skuRes.produto?.unidadeMedida?.permiteDecimais ? 4 : 0)}.`,
        });
        return;
      }

      const current = itens[existingIndex];
      const grossNew = newQty * current.valorUnitario;
      const valorTotalNew = parseFloat(
        (newQty * current.precoFinal).toFixed(2),
      );
      setItens(
        itens.map((it, i) =>
          i === existingIndex
            ? {
                ...it,
                quantidade: newQty,
                valorTotal: valorTotalNew,
                valorDesconto: parseFloat(
                  (grossNew - valorTotalNew).toFixed(2),
                ),
              }
            : it,
        ),
      );

      const acao = qtdeAdicionada >= 0 ? "alterada" : "decrementada";
      ui.feedback.notify({
        type: "success",
        title: `Quantidade do SKU "${skuRes.sku}" ${acao} para ${newQty}.`,
      });
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
    ui.feedback.notify({
      type: "info",
      title: `SKU "${itemToRemove.sku}" removido.`,
    });
  };

  const updateItemRow = (
    index: number,
    key: "quantidade" | "percentualDesconto" | "precoFinal",
    value: number,
  ) => {
    const current = itens[index];
    if (!current) return;
    const item = { ...current };

    if (key === "quantidade") {
      item.quantidade = value;
    } else if (key === "percentualDesconto") {
      const discPercent = Math.min(100, value);
      if (discPercent !== value) {
        ui.feedback.notify({
          type: "error",
          title: `Desconto não pode passar de 100%. Ajustado para ${discPercent}%.`,
        });
      }
      item.percentualDesconto = discPercent;
      item.precoFinal = parseFloat(
        (item.valorUnitario * (1 - discPercent / 100)).toFixed(2),
      );
    } else if (key === "precoFinal") {
      const finalPrice = Math.max(0, value);
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
    setItens(itens.map((it, i) => (i === index ? item : it)));
  };

  const buildParcelas = (payment: CondicaoPagamento): ParcelaPreview[] => {
    if (totalNet <= 0) return [];
    const baseDate = dataVenda ? new Date(dataVenda + "T12:00:00") : new Date();
    const result: ParcelaPreview[] = [];
    let count = 1;
    const entradaPercent = payment.entradaMinimaPercentual ?? 0;
    if (entradaPercent > 0) {
      result.push({
        numeroParcela: count++,
        dataVencimento: toLocalISODate(baseDate),
        valorParcela: parseFloat(
          (totalNet * (entradaPercent / 100)).toFixed(2),
        ),
      });
    }
    (payment.condicoesPagamentosParcelas || []).forEach((item) => {
      const venc = new Date(baseDate);
      venc.setDate(baseDate.getDate() + item.prazoDias);
      result.push({
        numeroParcela: count++,
        dataVencimento: toLocalISODate(venc),
        valorParcela: parseFloat(
          (totalNet * (item.percentual / 100)).toFixed(2),
        ),
      });
    });
    if (result.length) {
      const difference =
        totalNet - result.reduce((sum, item) => sum + item.valorParcela, 0);
      result[result.length - 1].valorParcela = parseFloat(
        (result[result.length - 1].valorParcela + difference).toFixed(2),
      );
    }
    return result;
  };

  const registerDirty = () => {
    activeWindow.setDirtyCheck(
      () => !readOnly && (!editingItem || form.state.isDirty || itens.length > 0),
    );
    return () => activeWindow.setDirtyCheck(null);
  };

  return (
    <div className="flex h-full min-h-0 flex-col gap-4">
      <WindowActions>
        {readOnly && editingItem && (
          <div className="mr-auto flex gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={openMovimentacao}
            >
              <Boxes /> Movimentação de Estoque <HotkeyKbd hotkey="Alt+M" />
            </Button>
            <Button type="button" variant="outline" onClick={openContasReceber}>
              <Receipt /> Contas a Receber <HotkeyKbd hotkey="Alt+R" />
            </Button>
            <Button type="button" variant="outline" onClick={openDevolucoes}>
              <Undo2 /> Devoluções <HotkeyKbd hotkey="Alt+D" />
            </Button>
            <Button type="button" variant="outline" onClick={openObservacao}>
              <FileText /> Observação <HotkeyKbd hotkey="Alt+O" />
            </Button>
            <Button type="button" variant="outline" disabled={gerandoNfe} onClick={gerarNfe}>
              <FilePlus2 /> {gerandoNfe ? "Gerando..." : "Gerar NF-e"} <HotkeyKbd hotkey="Alt+G" />
            </Button>
          </div>
        )}
        <WindowCancelButton readOnly={readOnly} />
        {!readOnly && (
          <Button
            type="submit"
            form="upsert-venda"
            disabled={mutation.isPending}
          >
            {mutation.isPending ? (
              "Salvando..."
            ) : (
              <span className="flex items-center gap-2">
                Ir para Finalização
                <KbdGroup>
                  <Kbd>Alt</Kbd>
                  <Kbd>Enter</Kbd>
                </KbdGroup>
              </span>
            )}
          </Button>
        )}
      </WindowActions>
      <form
        ref={registerDirty}
        id="upsert-venda"
        className="flex min-h-0 flex-1 flex-col gap-4"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <div className="flex min-h-0 w-full flex-1 flex-col gap-4">
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
                          val || todayLocalISODate();
                        field.handleChange(newVal);
                      }}
                      readOnly={readOnly}
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
                ref={setSkuInput}
                key={skuInputKey}
                name="add-sku-pos"
                label="Inserir Produto"
                onSelectSku={handleSkuAdded}
              />
            </div>
          )}

          <div className="flex min-h-0 w-full flex-1 flex-col">
            <Card className="flex min-h-0 flex-1 flex-col p-0">
              <CardContent className="flex min-h-0 flex-1 flex-col p-0">
                <div className="flex min-h-0 w-full flex-1 flex-col">
                  <Table pageScroll className="min-w-250">
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-28">
                          SKU
                        </TableHead>
                        <TableHead className="w-full">
                          Produto
                        </TableHead>
                        <TableHead className="w-36 text-right">
                          Quantidade
                        </TableHead>
                        <TableHead className="w-28 text-right">
                          Preço Base
                        </TableHead>
                        <TableHead className="w-28 text-right">
                          Desconto (%)
                        </TableHead>
                        <TableHead className="w-28 text-right">
                          Preço Final
                        </TableHead>
                        <TableHead className="w-28 text-right">
                          Total
                        </TableHead>
                        {!readOnly && (
                          <TableHead className="w-12 text-center"></TableHead>
                        )}
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {itens.length === 0 && (
                        <TableRow>
                          <TableCell colSpan={readOnly ? 7 : 8} className="text-muted-foreground text-center">
                            Nenhum item adicionado.
                          </TableCell>
                        </TableRow>
                      )}
                      {itens.length != 0 &&
                        itens.map((item, index) => (
                          <TableRow key={item.sku}>
                            <TableCell className="font-mono">
                              <SkuViewInput name={`venda-sku-${index}`} sku={item.sku} />
                            </TableCell>
                            <TableCell>
                              {item.produtoNome}
                            </TableCell>

                            <TableCell className="text-right">
                              <div className="flex flex-col items-end">
                                <NumberInput
                                  {...navigationCell({
                                    grid: "venda-itens",
                                    row: index,
                                    column: 0,
                                  })}
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
                                    "text-right",
                                    (item.quantidade <= 0 ||
                                      item.quantidade > item.estoqueAtual) &&
                                      "border-destructive focus-visible:ring-destructive",
                                  )}
                                  readOnly={readOnly}
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

                            <TableCell className="text-right">
                              {formatCurrency(item.valorUnitario)}
                            </TableCell>

                            <TableCell className="text-right">
                              <div className="flex flex-col items-end">
                                <NumberInput
                                  {...navigationCell({
                                    grid: "venda-itens",
                                    row: index,
                                    column: 1,
                                  })}
                                  value={item.percentualDesconto}
                                  decimals={2}
                                  inputSize="full"
                                  inputMode="decimal"
                                  allowNegative
                                  aria-invalid={item.percentualDesconto > 100}
                                  className={cn(
                                    "text-right text-destructive",
                                    item.percentualDesconto > 100
                                      ? "border-destructive focus-visible:ring-destructive"
                                      : "border-red-200 focus-visible:ring-red-500",
                                  )}
                                  readOnly={readOnly}
                                  onNumberChange={(val) =>
                                    updateItemRow(
                                      index,
                                      "percentualDesconto",
                                      val,
                                    )
                                  }
                                />
                                {item.percentualDesconto > 100 && (
                                  <span className="text-destructive text-2xs mt-0.5 text-right font-semibold whitespace-nowrap">
                                    Máx: 100%
                                  </span>
                                )}
                              </div>
                            </TableCell>

                            <TableCell className="text-right">
                              <div className="flex flex-col items-end">
                                <NumberInput
                                  {...navigationCell({
                                    grid: "venda-itens",
                                    row: index,
                                    column: 2,
                                  })}
                                  value={item.precoFinal}
                                  decimals={2}
                                  inputSize="full"
                                  inputMode="decimal"
                                  aria-invalid={item.precoFinal < 0}
                                  className={cn(
                                    "text-right",
                                    item.precoFinal < 0 &&
                                      "border-destructive focus-visible:ring-destructive",
                                  )}
                                  readOnly={readOnly}
                                  onNumberChange={(val) =>
                                    updateItemRow(index, "precoFinal", val)
                                  }
                                />
                                {item.precoFinal < 0 && (
                                  <span className="text-destructive text-2xs mt-0.5 text-right font-semibold whitespace-nowrap">
                                    Mín: 0
                                  </span>
                                )}
                              </div>
                            </TableCell>

                            <TableCell className="text-right">
                              {new Intl.NumberFormat("pt-BR", {
                                style: "currency",
                                currency: "BRL",
                              }).format(item.valorTotal)}
                            </TableCell>

                            {!readOnly && (
                              <TableCell className="text-center">
                                <Button
                                  {...navigationCell({
                                    grid: "venda-itens",
                                    row: index,
                                    column: 3,
                                  })}
                                  size="icon-sm"
                                  variant="ghost"
                                  aria-label="Remover Item"
                                  onClick={() => handleRemoveItem(index)}
                                >
                                  <Trash2 className="size-4" />
                                </Button>
                              </TableCell>
                            )}
                          </TableRow>
                        ))}
                    </TableBody>
                  </Table>
                </div>

                <div className="/50 /10 grid grid-cols-1 divide-y border-t md:grid-cols-4 md:divide-x md:divide-y-0">
                  {[
                    ["Itens Totais", totalItensCount, 0, "text-muted-foreground", ""],
                    ["Subtotal", subtotalGross, 2, "text-muted-foreground", ""],
                    ["Desconto Total", totalDiscount, 2, "text-destructive", "text-destructive"],
                    ["Total Líquido", totalNet, 2, "text-success", "text-success"],
                  ].map(([rotulo, valor, decimals, labelCls, valueCls]) => (
                    <div
                      key={rotulo as string}
                      className={cn(
                        "flex flex-col px-4 py-2 text-end",
                        rotulo === "Total Líquido" && "bg-emerald-500/5",
                      )}
                    >
                      <span className={cn("text-2xs font-semibold tracking-wider uppercase", labelCls as string)}>
                        {rotulo}
                      </span>
                      <NumberInput
                        value={valor as number}
                        decimals={decimals as number}
                        inputSize="full"
                        inputMode="decimal"
                        aria-label={rotulo as string}
                        readOnly
                        className={cn("h-8 text-right text-lg font-bold", valueCls as string)}
                        onNumberChange={() => {}}
                      />
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </form>
    </div>
  );
}
