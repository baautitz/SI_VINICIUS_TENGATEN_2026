"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { useHotkeys } from "@tanstack/react-hotkeys";
import { useRef, useState } from "react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field";
import { NumberInput } from "@/components/ui/number-input";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { useQueryClient } from "@tanstack/react-query";
import { estoqueApi } from "@/api/estoque";
import type { Resultado } from "@/api/types";
import { SkuInput } from "@/components/entity-inputs/sku-input";
import { SkuResumo } from "@/api/catalogo";
import { Trash2 } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import {
  MovimentacaoEstoque,
  MovimentacaoEstoqueFormValues,
  MovimentacaoEstoqueItemFormValues,
  movimentacaoEstoqueSchema,
  tipoPrecisaDeCusto,
  statusLabels,
} from "./types";
import { ItemLinha } from "./upsert";

interface MovimentacoesUpsertFormProps {
  open: boolean;
  editingItem: MovimentacaoEstoque | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly: boolean;
  initialItems?: ItemLinha[];
  fixedTipo?: "ENTRADA" | "SAIDA" | "BALANCO" | "VENDA";
}

export function MovimentacoesUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly,
  initialItems,
  fixedTipo,
}: MovimentacoesUpsertFormProps) {
  const isEditMode = !!editingItem;

  const [itens, setItens] = useState<ItemLinha[]>(() => {
    if (initialItems) return initialItems;
    return (
      editingItem?.movimentacoesEstoquesItens.map((i) => ({
        sku: i.sku.sku,
        produtoNome: i.produtoNome,
        quantidade: Number(i.quantidade),
        custoUnitario: Number(i.custoUnitario),
        estoqueAtual: i.quantidadeAnterior ?? i.sku.estoque,
        precoSugerido: Number(i.sku.preco),
        custoMedio: i.custoMedioAnterior ?? i.sku.custoMedio,
        custoUltimaCompra: Number(i.sku.custoUltimaCompra),
        unidadeMedidaSigla: i.unidadeMedidaSigla,
        permiteDecimais: i.sku.permiteDecimais,
      })) ?? []
    );
  });

  const [skuInputKey, setSkuInputKey] = useState(0);
  const [validationErrors, setValidationErrors] = useState<
    Record<string, string>
  >({});
  const [confirmCloseOpen, setConfirmCloseOpen] = useState(false);
  const [saveConfirmOpen, setSaveConfirmOpen] = useState(false);
  const [itemToRemoveIndex, setItemToRemoveIndex] = useState<number | null>(
    null,
  );
  const [pendingPayload, setPendingPayload] =
    useState<MovimentacaoEstoqueFormValues | null>(null);

  const queryClient = useQueryClient();

  const createdIdRef = useRef<number | null>(null);
  const skuInputRef = useRef<HTMLInputElement>(null);

  useHotkeys(
    [
      {
        hotkey: "Alt+K",
        callback: (e) => {
          e.preventDefault();
          skuInputRef.current?.focus();
        },
        options: {
          enabled: open && !readOnly,
          ignoreInputs: false,
        },
      },
      {
        hotkey: "Alt+Enter",
        callback: (e) => {
          e.preventDefault();
          confirmRemoveItem();
        },
        options: {
          enabled: itemToRemoveIndex !== null,
          ignoreInputs: false,
        },
      },
      {
        hotkey: "Alt+Enter",
        callback: (e) => {
          e.preventDefault();
          setConfirmCloseOpen(false);
          onClose();
        },
        options: {
          enabled: confirmCloseOpen,
          ignoreInputs: false,
        },
      },
      {
        hotkey: "Alt+Enter",
        callback: (e) => {
          e.preventDefault();
          if (pendingPayload) {
            mutation.mutate({ values: pendingPayload, efetivar: true });
            setSaveConfirmOpen(false);
          }
        },
        options: {
          enabled: saveConfirmOpen,
          ignoreInputs: false,
        },
      },
      {
        hotkey: "Alt+S",
        callback: (e) => {
          e.preventDefault();
          if (pendingPayload) {
            mutation.mutate({ values: pendingPayload, efetivar: false });
            setSaveConfirmOpen(false);
          }
        },
        options: {
          enabled: saveConfirmOpen,
          ignoreInputs: false,
        },
      },
    ],
    { conflictBehavior: "allow" },
  );

  const {
    mutation,
    globalError,
    getFieldError,
    resetErrors,
    backendFieldErrors,
  } = useUpsertMutation<
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
      onSuccess();
    },
    onClose: () => {
      createdIdRef.current = null;
      onClose();
    },
  });

  const form = useForm({
    defaultValues: {
      tipoMovimentacao: editingItem?.tipoMovimentacao ?? fixedTipo ?? "ENTRADA",
      usuarioId: editingItem?.usuario?.id ?? null,
      nfeId: editingItem?.nfe?.id ?? null,
      vendaId: editingItem?.venda?.id ?? null,
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

      const result = movimentacaoEstoqueSchema.safeParse(payload);
      if (!result.success) {
        const errors: Record<string, string> = {};
        result.error.errors.forEach((err) => {
          const path = err.path.join(".");
          errors[path] = err.message;
        });
        setValidationErrors(errors);
        return;
      }

      setPendingPayload(payload);
      setSaveConfirmOpen(true);
    },
  });

  const totalGeral = itens.reduce((sum, item) => {
    const itemTotal = Number(
      ((item.quantidade || 0) * (item.custoUnitario || 0)).toFixed(2),
    );
    return sum + itemTotal;
  }, 0);

  const isDirty = form.state.isDirty || itens.length > 0;

  const handleCloseAttempt = () => {
    if (isDirty && !readOnly) {
      setConfirmCloseOpen(true);
    } else {
      onClose();
    }
  };

  const removeItemRow = (index: number) => {
    const itemToRemove = itens[index];
    if (itemToRemove) {
      setItemToRemoveIndex(index);
    }
  };

  const confirmRemoveItem = () => {
    if (itemToRemoveIndex !== null) {
      const itemToRemove = itens[itemToRemoveIndex];
      setItens(itens.filter((_, i) => i !== itemToRemoveIndex));
      if (itemToRemove) {
        toast.info(`SKU "${itemToRemove.sku}" removido.`);
      }
      setItemToRemoveIndex(null);
    }
  };

  const handleSkuAdded = (
    skuRes: SkuResumo | null,
    qtdeAdicionada: number = 1,
  ) => {
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
        setItemToRemoveIndex(existingIndex);
        return;
      }

      updated[existingIndex].quantidade = newQty;
      setItens(updated);

      const acao = qtdeAdicionada >= 0 ? "incrementada" : "decrementada";
      const qtyExibicao =
        qtdeAdicionada >= 0 ? `+${qtdeAdicionada}` : qtdeAdicionada.toString();

      toast.success(
        `Quantidade do SKU "${skuRes.sku}" ${acao} (${qtyExibicao}).`,
      );
    } else {
      if (qtdeAdicionada <= 0) {
        toast.warning(
          "Não é possível adicionar um item com quantidade inicial zero ou negativa.",
        );
        return;
      }

      setItens([
        ...itens,
        {
          sku: skuRes.sku,
          produtoNome: skuRes.produtoNome,
          quantidade: Number(
            qtdeAdicionada.toFixed(skuRes.permiteDecimais ? 4 : 0),
          ),
          custoUnitario: custoInicial,
          estoqueAtual: Number(skuRes.estoque),
          precoSugerido: Number(skuRes.preco),
          custoMedio: Number(skuRes.custoMedio) || 0,
          custoUltimaCompra: Number(skuRes.custoUltimaCompra) || 0,
          unidadeMedidaSigla: skuRes.unidadeMedidaSigla,
          permiteDecimais: skuRes.permiteDecimais,
        },
      ]);
      toast.success(
        `SKU "${skuRes.sku}" adicionado (Qtde: ${qtdeAdicionada}).`,
      );
    }

    setSkuInputKey((prev) => prev + 1);
    setTimeout(() => {
      skuInputRef.current?.focus();
    }, 50);
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
    <>
      <UpsertDialog
        open={open}
        onOpenChange={(openState) => {
          if (!openState) handleCloseAttempt();
        }}
        onEscapeKeyDown={(e) => {
          if (isDirty && !readOnly) {
            e.preventDefault();
            setConfirmCloseOpen(true);
          }
        }}
        onPointerDownOutside={(e) => {
          if (isDirty && !readOnly) {
            e.preventDefault();
            setConfirmCloseOpen(true);
          }
        }}
        title={title}
        footer={
          <>
            <Button
              type="button"
              variant="outline"
              onClick={handleCloseAttempt}
            >
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
          </>
        }
      >
        <form
          id="upsert-movimentacao"
          className="flex flex-col gap-6"
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <FieldGroup className="flex flex-row flex-wrap gap-4 items-end">
            {editingItem && (
              <div className="flex flex-col gap-1.5 w-fit">
                <FieldLabel>Código</FieldLabel>
                <div className="h-8 px-3 rounded-lg border bg-muted/50 flex items-center text-sm font-mono text-foreground/80">
                  {editingItem.id}
                </div>
              </div>
            )}

            <form.Subscribe
              selector={(state) => [state.values.tipoMovimentacao]}
            >
              {([tipoMovimentacao]) => (
                <>
                  <div className="flex flex-col gap-1.5 w-48">
                    <form.Field name="tipoMovimentacao">
                      {(field) => (
                        <Field>
                          <FieldLabel htmlFor={field.name}>
                            Tipo de Movimentação
                          </FieldLabel>
                          <Select
                            value={field.state.value}
                            onValueChange={(val) => {
                              field.handleChange(
                                val as MovimentacaoEstoqueFormValues["tipoMovimentacao"],
                              );
                              if (!readOnly) {
                                setItens((prev) =>
                                  prev.map((item) => {
                                    if (val === "SAIDA" || val === "VENDA") {
                                      return {
                                        ...item,
                                        custoUnitario: item.custoMedio ?? 0,
                                      };
                                    } else if (val === "ENTRADA") {
                                      return {
                                        ...item,
                                        custoUnitario:
                                          item.custoUltimaCompra ?? 0,
                                      };
                                    }
                                    return item;
                                  }),
                                );
                              }
                            }}
                            disabled={readOnly || isEditMode || !!fixedTipo}
                          >
                            <SelectTrigger
                              id={field.name}
                              className="w-full h-8 rounded-lg"
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
                    <div className="flex flex-col gap-1.5 w-48">
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

                  <div className="flex flex-col gap-1.5 w-48">
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

                  <div className="border rounded-lg overflow-hidden bg-card">
                    <Table>
                      <TableHeader className="bg-muted border-b">
                        <TableRow className="hover:bg-transparent border-b">
                          <TableHead className="px-4 py-2 text-left w-24">
                            SKU
                          </TableHead>
                          <TableHead className="px-4 py-2 text-left w-full">
                            Produto
                          </TableHead>
                          <TableHead className="px-4 py-2 text-center w-16">
                            UM
                          </TableHead>
                          <TableHead className="px-4 py-2 text-right w-32">
                            {readOnly ? "Estoque Ant." : "Estoque"}
                          </TableHead>
                          <TableHead className="px-4 py-2 text-right w-32">
                            {readOnly ? "Estoque Final" : "Após"}
                          </TableHead>
                          <TableHead className="px-4 py-2 text-right w-44">
                            Qtde
                          </TableHead>
                          <TableHead className="px-4 py-2 text-right w-40">
                            Preço Venda
                          </TableHead>
                          {comCusto && (
                            <TableHead className="px-4 py-2 text-right w-40">
                              Custo Médio
                            </TableHead>
                          )}
                          {comCusto && (
                            <TableHead className="px-4 py-2 text-right w-48">
                              Custo Unit.
                            </TableHead>
                          )}
                          {comCusto && (
                            <TableHead className="px-4 py-2 text-right w-40">
                              Total
                            </TableHead>
                          )}
                          {!readOnly && (
                            <TableHead className="px-4 py-2 text-center w-16">
                              Ação
                            </TableHead>
                          )}
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {itens.length === 0 ? (
                          <TableRow>
                            <TableCell
                              colSpan={comCusto ? 11 : 8}
                              className="px-4 py-8 text-center text-muted-foreground"
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
                                className="group border-b last:border-0 hover:bg-muted/10"
                              >
                                <TableCell className="px-4 py-2.5 align-middle">
                                  <span className="font-mono font-bold text-sm text-foreground/90">
                                    {item.sku}
                                  </span>
                                  {skuErr && (
                                    <p className="text-[10px] text-red-500 mt-0.5">
                                      {skuErr}
                                    </p>
                                  )}
                                </TableCell>

                                <TableCell className="px-4 py-2.5 align-middle">
                                  <span className="text-sm font-medium text-foreground/90">
                                    {item.produtoNome}
                                  </span>
                                </TableCell>

                                <TableCell className="px-4 py-2.5 text-center align-middle">
                                  <span className="text-xs font-bold text-muted-foreground uppercase">
                                    {item.unidadeMedidaSigla || "-"}
                                  </span>
                                </TableCell>

                                <TableCell className="px-4 py-2.5 text-right align-middle text-sm font-medium text-muted-foreground">
                                  {item.estoqueAtual?.toLocaleString("pt-BR", {
                                    minimumFractionDigits: item.permiteDecimais
                                      ? 4
                                      : 0,
                                  }) ?? "-"}
                                </TableCell>

                                <TableCell className="px-4 py-2.5 text-right align-middle">
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
                                </TableCell>

                                <TableCell className="px-4 py-2.5 align-middle">
                                  <div className="flex flex-col">
                                    <NumberInput
                                      inputSize="full"
                                      value={item.quantidade}
                                      decimals={item.permiteDecimais ? 4 : 0}
                                      disabled={readOnly}
                                      onNumberChange={(num) => {
                                        updateItemRow(index, "quantidade", num);
                                      }}
                                      className={cn(
                                        "h-8 text-sm font-bold text-right w-40",
                                        qtdErr &&
                                          "border-destructive focus-visible:ring-destructive",
                                      )}
                                    />
                                    {qtdErr && (
                                      <p className="text-xs text-red-500 text-right">
                                        {qtdErr}
                                      </p>
                                    )}
                                  </div>
                                </TableCell>

                                <TableCell className="px-4 py-2.5 text-right align-middle text-sm text-muted-foreground">
                                  {item.precoSugerido?.toLocaleString("pt-BR", {
                                    style: "currency",
                                    currency: "BRL",
                                  }) ?? "-"}
                                </TableCell>

                                {comCusto && (
                                  <TableCell className="px-4 py-2.5 text-right align-middle text-sm text-muted-foreground font-medium">
                                    <span className="min-w-30">
                                      {item.custoMedio?.toLocaleString(
                                        "pt-BR",
                                        {
                                          style: "currency",
                                          currency: "BRL",
                                        },
                                      ) ?? "-"}
                                    </span>
                                  </TableCell>
                                )}

                                {comCusto && (
                                  <TableCell className="px-4 py-2.5 align-middle">
                                    <div className="flex flex-col">
                                      <NumberInput
                                        inputSize="full"
                                        value={item.custoUnitario}
                                        decimals={2}
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
                                          "h-8 text-sm font-bold text-right w-40",
                                          custoErr &&
                                            "border-destructive focus-visible:ring-destructive",
                                        )}
                                      />
                                      {custoErr && (
                                        <p className="text-[10px] text-red-500 text-right">
                                          {custoErr}
                                        </p>
                                      )}
                                    </div>
                                  </TableCell>
                                )}

                                {comCusto && (
                                  <TableCell className="px-4 py-2.5 text-right font-bold align-middle text-sm text-primary/90">
                                    {itemTotal.toLocaleString("pt-BR", {
                                      style: "currency",
                                      currency: "BRL",
                                    })}
                                  </TableCell>
                                )}

                                {!readOnly && (
                                  <TableCell className="px-4 py-2.5 text-center align-middle">
                                    <Button
                                      type="button"
                                      variant="ghost"
                                      size="icon"
                                      className="h-7 w-7 text-destructive hover:bg-destructive/10"
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
                        <TableFooter className="bg-muted/30 font-semibold border-t">
                          <TableRow>
                            <TableCell
                              colSpan={readOnly ? 9 : 10}
                              className="px-4 py-3 text-left text-sm font-bold uppercase"
                            >
                              Total Geral
                            </TableCell>
                            <TableCell className="px-4 py-3 text-right text-base text-primary font-black">
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

          {globalError && (
            <Alert variant="destructive">
              <AlertDescription>{globalError}</AlertDescription>
            </Alert>
          )}
        </form>
      </UpsertDialog>

      <Dialog
        open={itemToRemoveIndex !== null}
        onOpenChange={(open) => {
          if (!open) setItemToRemoveIndex(null);
        }}
      >
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Remover Item?</DialogTitle>
            <DialogDescription>
              Deseja realmente remover o SKU{" "}
              <strong>
                {itemToRemoveIndex !== null
                  ? itens[itemToRemoveIndex]?.sku
                  : ""}
              </strong>{" "}
              desta movimentação?
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="mt-4 flex flex-wrap gap-2 sm:justify-between">
            <Button
              type="button"
              variant="outline"
              onClick={() => setItemToRemoveIndex(null)}
              className="mr-auto"
            >
              Cancelar <Kbd>Esc</Kbd>
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={confirmRemoveItem}
            >
              Remover Item
              <KbdGroup className="ml-2">
                <Kbd>Alt</Kbd>
                <Kbd>Enter</Kbd>
              </KbdGroup>
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={confirmCloseOpen} onOpenChange={setConfirmCloseOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Sair do Formulário?</DialogTitle>
            <DialogDescription>
              Você possui alterações não salvas. Se fechar o formulário agora,
              todos os dados digitados serão perdidos.
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="mt-4 flex gap-2 justify-end">
            <Button
              type="button"
              variant="outline"
              onClick={() => setConfirmCloseOpen(false)}
            >
              Permanecer <Kbd>Esc</Kbd>
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={() => {
                setConfirmCloseOpen(false);
                onClose();
              }}
            >
              Sair e Descartar
              <KbdGroup className="ml-2">
                <Kbd>Alt</Kbd>
                <Kbd>Enter</Kbd>
              </KbdGroup>
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={saveConfirmOpen}
        onOpenChange={(open) => {
          setSaveConfirmOpen(open);
          if (!open) {
            setPendingPayload(null);
          }
        }}
      >
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Salvar Movimentação?</DialogTitle>
            <DialogDescription>
              Deseja salvar a movimentação de estoque como rascunho ou efetivar
              imediatamente para atualizar o estoque físico?
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="mt-6 flex flex-wrap gap-2 sm:justify-between">
            <Button
              type="button"
              variant="outline"
              disabled={mutation.isPending}
              onClick={() => {
                setSaveConfirmOpen(false);
                setPendingPayload(null);
              }}
              className="mr-auto"
            >
              Cancelar <Kbd>Esc</Kbd>
            </Button>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="secondary"
                disabled={mutation.isPending}
                onClick={() => {
                  if (pendingPayload) {
                    mutation.mutate({
                      values: pendingPayload,
                      efetivar: false,
                    });
                    setSaveConfirmOpen(false);
                  }
                }}
              >
                Salvar Rascunho{" "}
                <KbdGroup>
                  <Kbd>Alt</Kbd>
                  <Kbd>S</Kbd>
                </KbdGroup>
              </Button>
              <Button
                type="button"
                disabled={mutation.isPending}
                onClick={() => {
                  if (pendingPayload) {
                    mutation.mutate({ values: pendingPayload, efetivar: true });
                    setSaveConfirmOpen(false);
                  }
                }}
              >
                Efetivar{" "}
                <KbdGroup>
                  <Kbd>Alt</Kbd>
                  <Kbd>Enter</Kbd>
                </KbdGroup>
              </Button>
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
