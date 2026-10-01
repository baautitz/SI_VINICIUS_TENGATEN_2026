"use client";

import { Kbd, KbdGroup } from "@/ui/primitives";
import React, { useEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils";
import { AlertDialogFooter, Button } from "@/ui/primitives";
import { Field, FieldGroup, FieldLabel } from "@/ui/primitives";
import { NumberInput } from "@/ui/composites";
import { Alert, AlertDescription } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from "@/ui/primitives";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { useQueryClient } from "@tanstack/react-query";
import { estoqueApi } from "@/api/estoque";
import type { Resultado } from "@/api/types";
import { SkuInput } from "@/components/entity-inputs/sku-input";
import { Sku, getFullSkuName } from "@/features/catalogo/skus/types";
import { Trash2 } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/primitives";
import { useWindow, useWindowCommands, useUi } from "@/ui/imperative";
import {
  MovimentacaoEstoque,
  MovimentacaoEstoqueFormValues,
  MovimentacaoEstoqueItemFormValues,
  movimentacaoEstoqueSchema,
  tipoPrecisaDeCusto,
  statusLabels,
} from "./types";
import { ItemLinha } from "./upsert";
import { navigationCell } from "@/ui/keyboard-navigation";

interface MovimentacoesUpsertFormProps {
  editingItem: MovimentacaoEstoque | null;
  readOnly: boolean;
  initialItems?: ItemLinha[];
  fixedTipo?: "ENTRADA" | "SAIDA" | "BALANCO" | "VENDA";
}

type SaveAction = "draft" | "effect";

interface SaveConfirmationProps {
  onSelect?: never;
}

export function MovimentacoesUpsertForm({
  editingItem,
  readOnly,
  initialItems,
  fixedTipo,
}: MovimentacoesUpsertFormProps) {
  const isEditMode = !!editingItem;
  const activeWindow = useWindow<true>();
  const ui = useUi();

  const [itens, setItens] = useState<ItemLinha[]>(() => {
    if (initialItems) return initialItems;
    return (
      editingItem?.movimentacoesEstoquesItens.map((i) => {
        const fullSkuName = getFullSkuName(i.sku);
        return {
          sku: i.sku.sku,
          produtoNome: fullSkuName || i.produtoNome,
          quantidade: Number(i.quantidade),
          custoUnitario: Number(i.custoUnitario),
          estoqueAtual: i.quantidadeAnterior ?? i.sku.estoque,
          precoSugerido: Number(i.sku.preco),
          custoMedio: i.custoMedioAnterior ?? i.sku.custoMedio,
          custoUltimaCompra: Number(i.sku.custoUltimaCompra),
          unidadeMedidaSigla: i.unidadeMedidaSigla,
          permiteDecimais:
            i.sku.produto?.unidadeMedida?.permiteDecimais ?? false,
        };
      }) ?? []
    );
  });

  const [skuInputKey, setSkuInputKey] = useState(0);
  const [validationErrors, setValidationErrors] = useState<
    Record<string, string>
  >({});

  const queryClient = useQueryClient();

  const createdIdRef = useRef<number | null>(null);
  const skuInputRef = useRef<HTMLInputElement>(null);

  // Adding an item intentionally remounts SkuInput to reset its controlled
  // value. Restore focus after that DOM replacement, not before it.
  useEffect(() => {
    if (skuInputKey === 0) return;
    const frame = requestAnimationFrame(() => skuInputRef.current?.focus());
    return () => cancelAnimationFrame(frame);
  }, [skuInputKey]);

  const { mutation, getFieldError, resetErrors, backendFieldErrors } =
    useUpsertMutation<
      { values: MovimentacaoEstoqueFormValues; efetivar: boolean },
      Resultado<MovimentacaoEstoque>
    >({
      mutationFn: async ({ values, efetivar }) => {
        const existingId = editingItem?.id ?? createdIdRef.current;

        const saveRes = existingId
          ? await estoqueApi.update(existingId, values)
          : await estoqueApi.create(values);

        if (!saveRes.success || !saveRes.data) {
          return saveRes;
        }

        if (!editingItem) {
          createdIdRef.current = saveRes.data.id;
        }

        if (efetivar) {
          const confirmRes = await estoqueApi.confirmar(saveRes.data.id);
          return confirmRes;
        }

        return saveRes;
      },
      queryKey: ["movimentacoes"],
      onSuccessCallback: () => {
        queryClient.invalidateQueries({ queryKey: ["skus"] });
        queryClient.invalidateQueries({ queryKey: ["produtos"] });
        createdIdRef.current = null;
        activeWindow.resolve(true);
      },
    });

  const form = useForm({
    defaultValues: {
      tipoMovimentacao: editingItem?.tipoMovimentacao ?? fixedTipo ?? "ENTRADA",
      usuarioId: editingItem?.usuario?.id ?? null,
      nfeId: editingItem?.nfeId ?? null,
      vendaId: editingItem?.vendaId ?? null,
      observacao: editingItem?.observacao ?? "",
      itens: [] as MovimentacaoEstoqueItemFormValues[],
    } as MovimentacaoEstoqueFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      setValidationErrors({});

      const cleanItens: MovimentacaoEstoqueItemFormValues[] = itens.map(
        (i) => ({
          sku: i.sku,
          quantidade: i.quantidade,
          custoUnitario: i.custoUnitario,
        }),
      );

      const payload: MovimentacaoEstoqueFormValues = {
        ...value,
        itens: cleanItens,
      };

      const parsed = movimentacaoEstoqueSchema.safeParse(payload);
      if (!parsed.success) {
        const errors: Record<string, string> = {};
        parsed.error.errors.forEach((err) => {
          const path = err.path.join(".");
          errors[path] = err.message;
        });
        setValidationErrors(errors);
        return;
      }

      const result = await ui.windows.open<SaveAction, SaveConfirmationProps>({
        component: SaveConfirmationWindow,
        props: {},
        title: "Salvar Movimentação?",
        description:
          "Deseja salvar a movimentação como rascunho ou efetivar imediatamente para atualizar o estoque físico?",
        surface: "confirmation",
        size: "small",
      });
      if (result.status === "confirmed") {
        try {
          await mutation.mutateAsync({
            values: payload,
            efetivar: result.value === "effect",
          });
        } catch {
          // O hook central já apresenta o erro operacional em um toast.
        }
      }
    },
  });

  const handleCancel = async () => {
    activeWindow.dismiss("cancel");
  };

  const handleFormSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    event.stopPropagation();
    await form.handleSubmit();
  };

  const handleTipoMovimentacaoChange = async (val: string) => {
    form.setFieldValue(
      "tipoMovimentacao",
      val as MovimentacaoEstoqueFormValues["tipoMovimentacao"],
    );
    if (!readOnly) {
      setItens((prev) =>
        prev.map((item) => {
          if (val === "SAIDA" || val === "VENDA") {
            return { ...item, custoUnitario: item.custoMedio ?? 0 };
          }
          if (val === "ENTRADA") {
            return { ...item, custoUnitario: item.custoUltimaCompra ?? 0 };
          }
          return item;
        }),
      );
    }
  };

  const registerDirty = React.useCallback(() => {
    activeWindow.setDirtyCheck(
      () => !readOnly && (form.state.isDirty || itens.length > 0),
    );
    return () => activeWindow.setDirtyCheck(null);
  }, [activeWindow, form, itens.length, readOnly]);

  useWindowCommands(
    React.useMemo(
      () => [
        {
          id: "movimentacoes.focus-sku",
          hotkey: "Alt+K" as const,
          label: "Buscar SKU",
          enabled: !readOnly,
          run: (event: KeyboardEvent) => {
            event.preventDefault();
            skuInputRef.current?.focus();
          },
        },
        {
          id: "movimentacoes.submit",
          hotkey: "Alt+Enter" as const,
          label: "Salvar movimentação",
          enabled: !readOnly && !mutation.isPending,
          run: async (event: KeyboardEvent) => {
            event.preventDefault();
            await form.handleSubmit();
          },
        },
      ],
      [form, mutation.isPending, readOnly],
    ),
  );

  const totalGeral = itens.reduce((sum, item) => {
    const itemTotal = Number(
      ((item.quantidade || 0) * (item.custoUnitario || 0)).toFixed(2),
    );
    return sum + itemTotal;
  }, 0);

  const removeItemRow = async (index: number) => {
    const itemToRemove = itens[index];
    if (!itemToRemove) return;
    const result = await ui.windows.confirm({
      title: "Remover Item?",
      description: `Deseja realmente remover o SKU ${itemToRemove.sku} desta movimentação?`,
      confirmLabel: "Remover Item",
      confirmVariant: "destructive",
    });
    if (!result) return;
    setItens((current) =>
      current.filter((_, itemIndex) => itemIndex !== index),
    );
    ui.feedback.notify({
      type: "info",
      title: `SKU "${itemToRemove.sku}" removido.`,
    });
  };

  const handleSkuAdded = (skuRes: Sku | null, qtdeAdicionada: number = 1) => {
    if (!skuRes) return;

    const tipoMov = form.getFieldValue("tipoMovimentacao");
    const custoInicial =
      tipoMov === "SAIDA" || tipoMov === "VENDA"
        ? Number(Number(skuRes.custoMedio || 0).toFixed(2))
        : Number(Number(skuRes.custoUltimaCompra || 0).toFixed(2));

    const existingIndex = itens.findIndex((i) => i.sku === skuRes.sku);
    if (existingIndex > -1) {
      const updated = [...itens];
      const newQty = Number(
        (updated[existingIndex].quantidade + qtdeAdicionada).toFixed(4),
      );

      if (newQty <= 0) {
        return removeItemRow(existingIndex);
      }

      updated[existingIndex].quantidade = newQty;
      setItens(updated);

      const acao = qtdeAdicionada >= 0 ? "incrementada" : "decrementada";
      const qtyExibicao =
        qtdeAdicionada >= 0 ? `+${qtdeAdicionada}` : qtdeAdicionada.toString();

      ui.feedback.notify({
        type: "success",
        title: `Quantidade do SKU "${skuRes.sku}" ${acao} (${qtyExibicao}).`,
      });
    } else {
      if (qtdeAdicionada <= 0) {
        ui.feedback.notify({
          type: "warning",
          title:
            "Não é possível adicionar um item com quantidade inicial zero ou negativa.",
        });
        return;
      }

      setItens([
        ...itens,
        {
          sku: skuRes.sku,
          produtoNome: getFullSkuName(skuRes),
          quantidade: Number(
            qtdeAdicionada.toFixed(
              skuRes.produto?.unidadeMedida?.permiteDecimais ? 4 : 0,
            ),
          ),
          custoUnitario: custoInicial,
          estoqueAtual: Number(skuRes.estoque),
          precoSugerido: Number(skuRes.preco),
          custoMedio: Number(skuRes.custoMedio) || 0,
          custoUltimaCompra: Number(skuRes.custoUltimaCompra) || 0,
          unidadeMedidaSigla: skuRes.produto?.unidadeMedida?.sigla ?? "",
          permiteDecimais: !!skuRes.produto?.unidadeMedida?.permiteDecimais,
        },
      ]);
      ui.feedback.notify({
        type: "success",
        title: `SKU "${skuRes.sku}" adicionado (Qtde: ${qtdeAdicionada}).`,
      });
    }

    setSkuInputKey((prev) => prev + 1);
    skuInputRef.current?.focus();
  };

  const updateItemRow = (
    index: number,
    key: keyof ItemLinha,
    val: string | number | undefined,
  ) => {
    const updated = [...itens];
    const item = updated[index];
    let finalVal = val;

    if (key === "quantidade") {
      const precision = item.permiteDecimais ? 4 : 0;
      finalVal = Number(Number(val || 0).toFixed(precision));
    } else if (key === "custoUnitario") {
      finalVal = Number(Number(val || 0).toFixed(2));
    }

    updated[index] = {
      ...updated[index],
      [key]: finalVal,
    } as ItemLinha;
    setItens(updated);
  };

  let title = isEditMode
    ? `Editar Movimentação #${editingItem?.id}`
    : "Nova Movimentação de Estoque";

  if (readOnly && editingItem) {
    title = `Visualizar Movimentação #${editingItem.id} [${statusLabels[editingItem.status]}]`;
  }

  return (
    <div className="flex flex-col gap-4">
      <div data-window-actions className="flex justify-end gap-2 border-b pb-4">
        <Button type="button" variant="outline" onClick={handleCancel}>
          <span className="flex items-center gap-2">
            {readOnly ? "Fechar" : "Cancelar"} <Kbd>Esc</Kbd>
          </span>
        </Button>
        {!readOnly && (
          <form.Subscribe
            selector={(state) => [state.canSubmit, state.isSubmitting]}
          >
            {([canSubmit, isSubmitting]) => (
              <Button
                type="submit"
                form="upsert-movimentacao"
                disabled={!canSubmit || isSubmitting || mutation.isPending}
              >
                {isSubmitting || mutation.isPending ? (
                  "Salvando..."
                ) : (
                  <span className="flex items-center gap-2">
                    Salvar{" "}
                    <KbdGroup>
                      <Kbd>Alt</Kbd>
                      <Kbd>Enter</Kbd>
                    </KbdGroup>
                  </span>
                )}
              </Button>
            )}
          </form.Subscribe>
        )}
      </div>
      <div aria-label={title}>
        <form
          ref={registerDirty}
          id="upsert-movimentacao"
          className="flex flex-col gap-6"
          onSubmit={handleFormSubmit}
        >
          <FieldGroup className="flex flex-row flex-wrap items-end gap-4">
            {editingItem && (
              <div className="flex w-fit flex-col gap-2">
                <FieldLabel>Código</FieldLabel>
                <div className="bg-muted/50 text-foreground/80 flex h-8 items-center rounded-lg border px-3 font-mono text-sm">
                  {editingItem.id}
                </div>
              </div>
            )}

            <form.Subscribe
              selector={(state) => [state.values.tipoMovimentacao]}
            >
              {([tipoMovimentacao]) => (
                <>
                  <div className="flex w-48 flex-col gap-2">
                    <form.Field name="tipoMovimentacao">
                      {(field) => (
                        <Field>
                          <FieldLabel htmlFor={field.name}>
                            Tipo de Movimentação
                          </FieldLabel>
                          <Select
                            value={field.state.value}
                            onValueChange={handleTipoMovimentacaoChange}
                            disabled={readOnly || isEditMode || !!fixedTipo}
                          >
                            <SelectTrigger
                              id={field.name}
                              className="h-8 w-full rounded-lg"
                            >
                              <SelectValue placeholder="Selecione o tipo..." />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="ENTRADA">Entrada</SelectItem>
                              <SelectItem value="SAIDA">Saída</SelectItem>
                              <SelectItem value="BALANCO">Balanço</SelectItem>
                              {field.state.value === "VENDA" && (
                                <SelectItem value="VENDA">Venda</SelectItem>
                              )}
                            </SelectContent>
                          </Select>
                        </Field>
                      )}
                    </form.Field>
                  </div>

                  {tipoMovimentacao === "VENDA" && (
                    <div className="flex w-48 flex-col gap-2">
                      <form.Field name="vendaId">
                        {(field) => {
                          const err =
                            validationErrors["vendaId"] ||
                            getFieldError(field.name, field.state.meta.errors);
                          return (
                            <FormFieldUI
                              field={field}
                              label="ID da Venda"
                              inputSize="full"
                              type="number"
                              decimals={0}
                              disabled={readOnly || isEditMode}
                              placeholder="ID da Venda..."
                              getFieldError={() => err}
                            />
                          );
                        }}
                      </form.Field>
                    </div>
                  )}

                  <div className="flex w-48 flex-col gap-2">
                    <form.Field name="nfeId">
                      {(field) => (
                        <FormFieldUI
                          field={field}
                          label="ID da NF-e"
                          inputSize="full"
                          type="number"
                          decimals={0}
                          disabled={readOnly}
                          getFieldError={getFieldError}
                        />
                      )}
                    </form.Field>
                  </div>
                </>
              )}
            </form.Subscribe>
          </FieldGroup>

          <FieldGroup className="grid grid-cols-1 gap-4">
            <form.Field name="observacao">
              {(field) => (
                <FormFieldUI
                  field={field}
                  label="Observação"
                  inputSize="full"
                  disabled={readOnly}
                  placeholder="Justificativa da movimentação..."
                  getFieldError={getFieldError}
                  maxLength={500}
                />
              )}
            </form.Field>
          </FieldGroup>

          <form.Subscribe selector={(state) => [state.values.tipoMovimentacao]}>
            {([tipoMovimentacao]) => {
              const comCusto = tipoPrecisaDeCusto(tipoMovimentacao);

              return (
                <div className="flex flex-col gap-3 border-t pt-4">
                  {!readOnly && (
                    <div className="flex flex-col gap-2">
                      <div className="max-w-md">
                        <SkuInput
                          ref={skuInputRef}
                          key={skuInputKey}
                          name="add-sku"
                          label="Itens da Movimentação"
                          onSelectSku={handleSkuAdded}
                        />
                      </div>
                    </div>
                  )}

                  {validationErrors["itens"] && (
                    <Alert variant="destructive" className="py-2">
                      <AlertDescription className="text-sm">
                        {validationErrors["itens"]}
                      </AlertDescription>
                    </Alert>
                  )}

                  <div className="bg-card overflow-x-auto rounded-lg border">
                    <Table className="w-full">
                      <TableHeader className="bg-muted border-b">
                        <TableRow className="border-b hover:bg-transparent">
                          <TableHead className="w-24 px-4 py-2 text-left">
                            SKU
                          </TableHead>
                          <TableHead className="w-full px-4 py-2 text-left">
                            Produto
                          </TableHead>
                          <TableHead className="w-36 px-4 py-2 text-right">
                            Estoque
                          </TableHead>
                          <TableHead className="min-w-32 px-4 py-2 text-right">
                            Qtde
                          </TableHead>
                          {comCusto && (
                            <TableHead className="w-44 px-4 py-2 text-right">
                              Custo Unit.
                            </TableHead>
                          )}
                          {comCusto && (
                            <TableHead className="w-36 px-4 py-2 text-right">
                              Total
                            </TableHead>
                          )}
                          {!readOnly && (
                            <TableHead className="w-14 px-4 py-2 text-center">
                              Ação
                            </TableHead>
                          )}
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {itens.length === 0 ? (
                          <TableRow>
                            <TableCell
                              colSpan={comCusto ? 7 : 5}
                              className="text-muted-foreground px-4 py-8 text-center"
                            >
                              Nenhum item adicionado ainda.
                            </TableCell>
                          </TableRow>
                        ) : (
                          itens.map((item, index) => {
                            const itemTotal = Number(
                              (
                                (item.quantidade || 0) *
                                (item.custoUnitario || 0)
                              ).toFixed(2),
                            );

                            const estoqueApos = (() => {
                              const rawApos =
                                tipoMovimentacao === "ENTRADA"
                                  ? (item.estoqueAtual || 0) +
                                    (item.quantidade || 0)
                                  : tipoMovimentacao === "SAIDA" ||
                                      tipoMovimentacao === "VENDA"
                                    ? (item.estoqueAtual || 0) -
                                      (item.quantidade || 0)
                                    : item.quantidade || 0;
                              return Number(rawApos.toFixed(4));
                            })();

                            const skuErr =
                              validationErrors[`itens.${index}.sku`] ||
                              backendFieldErrors[
                                `itens.${index}.sku`.toLowerCase()
                              ];
                            const qtdErr =
                              validationErrors[`itens.${index}.quantidade`] ||
                              backendFieldErrors[
                                `itens.${index}.quantidade`.toLowerCase()
                              ];
                            const custoErr =
                              validationErrors[
                                `itens.${index}.custoUnitario`
                              ] ||
                              backendFieldErrors[
                                `itens.${index}.custoUnitario`.toLowerCase()
                              ];

                            return (
                              <TableRow
                                key={index}
                                className="group hover:bg-muted/10 border-b last:border-0"
                              >
                                <TableCell className="px-4 py-2.5 align-middle">
                                  <span className="text-foreground/90 font-mono text-sm font-bold">
                                    {item.sku}
                                  </span>
                                  {skuErr && (
                                    <p className="mt-0.5 text-xs text-red-500">
                                      {skuErr}
                                    </p>
                                  )}
                                </TableCell>

                                <TableCell className="px-4 py-2.5 align-middle">
                                  <div className="flex items-baseline gap-1.5">
                                    <span className="text-foreground/90 text-sm font-medium">
                                      {item.produtoNome}
                                    </span>
                                    {item.unidadeMedidaSigla && (
                                      <span className="text-muted-foreground font-mono text-[10px] font-semibold uppercase">
                                        {item.unidadeMedidaSigla}
                                      </span>
                                    )}
                                  </div>
                                </TableCell>

                                <TableCell className="px-4 py-2.5 text-right align-middle">
                                  <div className="flex items-baseline justify-end gap-1">
                                    <span className="text-muted-foreground text-sm">
                                      {item.estoqueAtual?.toLocaleString(
                                        "pt-BR",
                                        {
                                          minimumFractionDigits:
                                            item.permiteDecimais ? 4 : 0,
                                        },
                                      ) ?? "-"}
                                    </span>
                                    <span className="text-muted-foreground/60 text-xs">
                                      →
                                    </span>
                                    <span
                                      className={cn(
                                        "text-sm font-bold",
                                        estoqueApos < 0
                                          ? "text-destructive"
                                          : "text-foreground",
                                      )}
                                    >
                                      {estoqueApos.toLocaleString("pt-BR", {
                                        minimumFractionDigits:
                                          item.permiteDecimais ? 4 : 0,
                                      })}
                                    </span>
                                  </div>
                                </TableCell>

                                <TableCell className="w-44 px-4 py-2.5 text-right align-middle">
                                  <div className="flex flex-col items-end">
                                    <NumberInput
                                      {...navigationCell({
                                        grid: "estoque-movimentacao-itens",
                                        row: index,
                                        column: 0,
                                      })}
                                      inputSize="full"
                                      value={item.quantidade}
                                      decimals={item.permiteDecimais ? 4 : 0}
                                      inputMode={
                                        item.permiteDecimais
                                          ? "decimal"
                                          : "numeric"
                                      }
                                      aria-invalid={!!qtdErr}
                                      disabled={readOnly}
                                      onNumberChange={(num) => {
                                        updateItemRow(index, "quantidade", num);
                                      }}
                                      className={cn(
                                        "h-8 text-right text-sm font-bold",
                                        qtdErr &&
                                          "border-destructive focus-visible:ring-destructive",
                                      )}
                                    />
                                    {qtdErr && (
                                      <p className="text-right text-xs text-red-500">
                                        {qtdErr}
                                      </p>
                                    )}
                                  </div>
                                </TableCell>

                                {comCusto && (
                                  <TableCell className="w-48 px-4 py-2.5 text-right align-middle">
                                    <div className="flex flex-col items-end">
                                      <NumberInput
                                        {...navigationCell({
                                          grid: "estoque-movimentacao-itens",
                                          row: index,
                                          column: 1,
                                        })}
                                        inputSize="full"
                                        value={item.custoUnitario}
                                        decimals={2}
                                        inputMode="decimal"
                                        aria-invalid={!!custoErr}
                                        disabled={
                                          readOnly ||
                                          tipoMovimentacao === "SAIDA" ||
                                          tipoMovimentacao === "VENDA"
                                        }
                                        onNumberChange={(num) => {
                                          updateItemRow(
                                            index,
                                            "custoUnitario",
                                            num,
                                          );
                                        }}
                                        className={cn(
                                          "h-8 text-right text-sm font-bold",
                                          custoErr &&
                                            "border-destructive focus-visible:ring-destructive",
                                        )}
                                      />
                                      {custoErr && (
                                        <p className="text-right text-xs text-red-500">
                                          {custoErr}
                                        </p>
                                      )}
                                    </div>
                                  </TableCell>
                                )}

                                {comCusto && (
                                  <TableCell className="text-primary/90 px-4 py-2.5 text-right align-middle text-sm font-bold">
                                    {itemTotal.toLocaleString("pt-BR", {
                                      style: "currency",
                                      currency: "BRL",
                                    })}
                                  </TableCell>
                                )}

                                {!readOnly && (
                                  <TableCell className="px-4 py-2.5 text-center align-middle">
                                    <Button
                                      {...navigationCell({
                                        grid: "estoque-movimentacao-itens",
                                        row: index,
                                        column: 2,
                                      })}
                                      type="button"
                                      variant="ghost"
                                      size="icon"
                                      className="text-destructive hover:bg-destructive/10 h-7 w-7"
                                      onClick={() => removeItemRow(index)}
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
                      {comCusto && itens.length > 0 && (
                        <TableFooter className="bg-muted/30 border-t font-semibold">
                          <TableRow>
                            <TableCell
                              colSpan={readOnly ? 4 : 5}
                              className="px-4 py-3 text-left text-sm font-bold uppercase"
                            >
                              Total Geral
                            </TableCell>
                            <TableCell className="text-primary px-4 py-3 text-right text-base font-bold">
                              {totalGeral.toLocaleString("pt-BR", {
                                style: "currency",
                                currency: "BRL",
                              })}
                            </TableCell>
                          </TableRow>
                        </TableFooter>
                      )}
                    </Table>
                  </div>
                </div>
              );
            }}
          </form.Subscribe>
        </form>
      </div>
    </div>
  );
}

function SaveConfirmationWindow() {
  const activeWindow = useWindow<SaveAction>();
  const handleCancel = React.useCallback(async () => {
    activeWindow.dismiss("cancel");
  }, [activeWindow]);
  const handleDraft = React.useCallback(async () => {
    activeWindow.resolve("draft");
  }, [activeWindow]);
  const handleEffect = React.useCallback(async () => {
    activeWindow.resolve("effect");
  }, [activeWindow]);
  const commands = React.useMemo(
    () => [
      {
        id: "movimentacoes.save-draft",
        hotkey: "Alt+S" as const,
        label: "Salvar rascunho",
        run: handleDraft,
      },
      {
        id: "movimentacoes.effect",
        hotkey: "Alt+Enter" as const,
        label: "Efetivar movimentação",
        run: handleEffect,
      },
    ],
    [handleDraft, handleEffect],
  );
  useWindowCommands(commands);

  return (
    <AlertDialogFooter
      data-window-actions
      className="flex-row flex-wrap items-center justify-end gap-2"
    >
      <Button type="button" variant="outline" onClick={handleCancel}>
        Cancelar <Kbd>Esc</Kbd>
      </Button>
      <Button type="button" variant="secondary" onClick={handleDraft}>
        Salvar Rascunho{" "}
        <KbdGroup>
          <Kbd>Alt</Kbd>
          <Kbd>S</Kbd>
        </KbdGroup>
      </Button>
      <Button type="button" onClick={handleEffect}>
        Efetivar{" "}
        <KbdGroup>
          <Kbd>Alt</Kbd>
          <Kbd>Enter</Kbd>
        </KbdGroup>
      </Button>
    </AlertDialogFooter>
  );
}
