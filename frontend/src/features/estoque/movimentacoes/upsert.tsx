"use client";

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
import { Input } from "@/components/ui/input";
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
import { useQuery } from "@tanstack/react-query";
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
} from "./types";

interface MovimentacoesUpsertProps {
  open: boolean;
  editingItem: MovimentacaoEstoque | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function MovimentacoesUpsert(props: MovimentacoesUpsertProps) {
  const { open, editingItem, onClose, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["movimentacoes", "detail", editingItem?.id],
    queryFn: () => estoqueApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Visualizar Movimentação"
        loading={true}
      />
    );
  }

  const resolvedReadOnly =
    readOnly || (fullItem ? fullItem.status !== "RASCUNHO" : false);

  return (
    <MovimentacoesUpsertForm
      {...props}
      editingItem={isEditMode ? (fullItem ?? null) : null}
      readOnly={resolvedReadOnly}
    />
  );
}

function MovimentacoesUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly,
}: MovimentacoesUpsertProps) {
  const isEditMode = !!editingItem;

  interface ItemLinha {
    sku: string;
    quantidade: number;
    custoUnitario: number;
    estoqueAtual?: number;
    precoSugerido?: number;
  }

  const [itens, setItens] = useState<ItemLinha[]>(
    editingItem?.movimentacoesEstoquesItens.map((i) => ({
      sku: i.sku.sku,
      quantidade: Number(i.quantidade),
      custoUnitario: Number(i.custoUnitario),
      estoqueAtual: i.sku.estoque,
      precoSugerido: i.sku.preco,
    })) ?? [],
  );

  const [skuInputKey, setSkuInputKey] = useState(0);
  const [validationErrors, setValidationErrors] = useState<
    Record<string, string>
  >({});
  const [confirmCloseOpen, setConfirmCloseOpen] = useState(false);
  const [saveConfirmOpen, setSaveConfirmOpen] = useState(false);
  const [pendingPayload, setPendingPayload] =
    useState<MovimentacaoEstoqueFormValues | null>(null);

  // Tracks the ID created on first save when creating a new movimentacao.
  // Prevents creating a duplicate if efetivar fails and user retries.
  const createdIdRef = useRef<number | null>(null);

  const { mutation, globalError, getFieldError, resetErrors, backendFieldErrors } =
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
      onSuccessCallback: onSuccess,
      onClose: () => {
        createdIdRef.current = null;
        onClose();
      },
    });

  const form = useForm({
    defaultValues: {
      tipoMovimentacao: editingItem?.tipoMovimentacao ?? "ENTRADA",
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

  const totalGeral = itens.reduce(
    (sum, item) => sum + (item.quantidade * item.custoUnitario || 0),
    0,
  );

  const isDirty = form.state.isDirty || itens.length > 0;

  const handleCloseAttempt = () => {
    if (isDirty && !readOnly) {
      setConfirmCloseOpen(true);
    } else {
      onClose();
    }
  };

  const handleSkuAdded = (skuRes: SkuResumo | null, qtdeAdicionada: number = 1) => {
    if (!skuRes) return;

    const existingIndex = itens.findIndex((i) => i.sku === skuRes.sku);
    if (existingIndex > -1) {
      const updated = [...itens];
      updated[existingIndex].quantidade += qtdeAdicionada;
      setItens(updated);
      toast.success(`Quantidade do SKU "${skuRes.sku}" incrementada (+${qtdeAdicionada}).`);
    } else {
      setItens([
        ...itens,
        {
          sku: skuRes.sku,
          quantidade: qtdeAdicionada,
          custoUnitario: Number(skuRes.preco),
          estoqueAtual: Number(skuRes.estoque),
          precoSugerido: Number(skuRes.preco),
        },
      ]);
      toast.success(`SKU "${skuRes.sku}" adicionado (Qtde: ${qtdeAdicionada}).`);
    }
    setSkuInputKey((k) => k + 1);
  };

  const removeItemRow = (index: number) => {
    const itemToRemove = itens[index];
    setItens(itens.filter((_, i) => i !== index));
    if (itemToRemove) {
      toast.info(`SKU "${itemToRemove.sku}" removido.`);
    }
  };

  const updateItemRow = (
    index: number,
    key: keyof ItemLinha,
    val: string | number | undefined,
  ) => {
    const updated = [...itens];
    updated[index] = {
      ...updated[index],
      [key]: val,
    } as ItemLinha;
    setItens(updated);
  };

  let title = "Nova Movimentação de Estoque";
  if (editingItem) {
    if (editingItem.status !== "RASCUNHO") {
      title = `Visualizar Movimentação #${editingItem.id} [${editingItem.status}]`;
    } else {
      title = `Editar Movimentação Rascunho #${editingItem.id}`;
    }
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
              {readOnly ? "Fechar" : "Cancelar"}
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
                    {isSubmitting || mutation.isPending
                      ? "Salvando..."
                      : "Salvar"}
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
              <div className="flex flex-col gap-1.5 w-24">
                <FieldLabel>Código</FieldLabel>
                <Input
                  value={editingItem.id}
                  disabled
                  className="h-8 text-xs font-mono"
                  inputSize="small"
                />
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
                            onValueChange={(val) =>
                              field.handleChange(
                                val as MovimentacaoEstoqueFormValues["tipoMovimentacao"],
                              )
                            }
                            disabled={readOnly || isEditMode}
                          >
                            <SelectTrigger
                              id={field.name}
                              className="w-full h-8 text-xs rounded-lg"
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
                              disabled={readOnly || isEditMode}
                              placeholder="ID comercial da Venda..."
                              getFieldError={() => err}
                              onChange={(val) =>
                                field.handleChange(val ? Number(val) : null)
                              }
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
                          disabled={readOnly}
                          placeholder="Opcional..."
                          getFieldError={getFieldError}
                          onChange={(val) =>
                            field.handleChange(val ? Number(val) : null)
                          }
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
              const colSpanVazio = readOnly ? (comCusto ? 4 : 3) : (comCusto ? 5 : 4);

              return (
                <div className="flex flex-col gap-3 border-t pt-4">
                  <div className="flex flex-col gap-2">
                    <h3 className="text-base font-semibold">Itens da Movimentação</h3>
                    {!readOnly && (
                      <div className="max-w-md">
                        <SkuInput
                          key={skuInputKey}
                          name="add-sku"
                          label="Pesquisar/Adicionar Produto (SKU)"
                          onSelectSku={handleSkuAdded}
                          disabled={readOnly}
                        />
                      </div>
                    )}
                  </div>

                  {validationErrors["itens"] && (
                    <Alert variant="destructive" className="py-2">
                      <AlertDescription className="text-xs">
                        {validationErrors["itens"]}
                      </AlertDescription>
                    </Alert>
                  )}

                  <div className="border rounded-lg overflow-hidden bg-card">
                    <Table>
                      <TableHeader className="bg-muted border-b">
                        <TableRow className="hover:bg-transparent border-b">
                          <TableHead className="px-4 py-2 text-left h-10">SKU</TableHead>
                          <TableHead className="px-4 py-2 text-left w-36 h-10">Quantidade</TableHead>
                          {comCusto && (
                            <TableHead className="px-4 py-2 text-left w-36 h-10">Custo Unitário</TableHead>
                          )}
                          {comCusto && (
                            <TableHead className="px-4 py-2 text-right w-36 h-10">Valor Total</TableHead>
                          )}
                          {!readOnly && (
                            <TableHead className="px-4 py-2 text-center w-16 h-10">Ação</TableHead>
                          )}
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {itens.length === 0 ? (
                          <TableRow>
                            <TableCell
                              colSpan={colSpanVazio}
                              className="px-4 py-8 text-center"
                            >
                              Nenhum item adicionado ainda.
                            </TableCell>
                          </TableRow>
                        ) : (
                          itens.map((item, index) => {
                            const itemTotal =
                              (item.quantidade || 0) * (item.custoUnitario || 0);
                            const skuErr =
                              validationErrors[`itens.${index}.sku`] ||
                              backendFieldErrors[`itens.${index}.sku`.toLowerCase()];
                            const qtdErr =
                              validationErrors[`itens.${index}.quantidade`] ||
                              backendFieldErrors[`itens.${index}.quantidade`.toLowerCase()];
                            const custoErr =
                              validationErrors[`itens.${index}.custoUnitario`] ||
                              backendFieldErrors[`itens.${index}.custoUnitario`.toLowerCase()];

                            return (
                              <TableRow key={index}>
                                <TableCell className="px-4 py-2 align-middle">
                                  <div className="flex flex-col">
                                    <span className="font-mono font-medium text-xs">
                                      {item.sku}
                                    </span>
                                    {(item.estoqueAtual !== undefined ||
                                      item.precoSugerido !== undefined) && (
                                      <span className="text-[10px] text-muted-foreground mt-0.5">
                                        {item.estoqueAtual !== undefined &&
                                          `Estoque: ${item.estoqueAtual}`}
                                        {item.estoqueAtual !== undefined &&
                                          item.precoSugerido !== undefined &&
                                          " | "}
                                        {item.precoSugerido !== undefined &&
                                          !comCusto === false &&
                                          `Preço Base: ${item.precoSugerido.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}`}
                                      </span>
                                    )}
                                  </div>
                                  {skuErr && (
                                    <p className="text-[10px] text-red-500 mt-1">
                                      {skuErr}
                                    </p>
                                  )}
                                </TableCell>
                                <TableCell className="px-4 py-2 align-top pt-3">
                                  <Input
                                    type="number"
                                    value={
                                      Number.isNaN(item.quantidade)
                                        ? ""
                                        : item.quantidade
                                    }
                                    disabled={readOnly}
                                    onChange={(e) => {
                                      const parsed = parseFloat(e.target.value);
                                      updateItemRow(
                                        index,
                                        "quantidade",
                                        Number.isNaN(parsed) ? 0 : parsed,
                                      );
                                    }}
                                    className={cn(
                                      "h-8 text-xs",
                                      qtdErr && "border-destructive focus-visible:ring-destructive"
                                    )}
                                    min={0.0001}
                                    step="any"
                                  />
                                  {qtdErr && (
                                    <p className="text-[10px] text-red-500 mt-1">
                                      {qtdErr}
                                    </p>
                                  )}
                                </TableCell>
                                {comCusto && (
                                  <TableCell className="px-4 py-2 align-top pt-3">
                                    <Input
                                      type="number"
                                      value={
                                        Number.isNaN(item.custoUnitario)
                                          ? ""
                                          : item.custoUnitario
                                      }
                                      disabled={readOnly}
                                      onChange={(e) => {
                                        const parsed = parseFloat(e.target.value);
                                        updateItemRow(
                                          index,
                                          "custoUnitario",
                                          Number.isNaN(parsed) ? 0 : parsed,
                                        );
                                      }}
                                      className={cn(
                                        "h-8 text-xs",
                                        custoErr && "border-destructive focus-visible:ring-destructive"
                                      )}
                                      min={0}
                                      step="any"
                                    />
                                    {custoErr && (
                                      <p className="text-[10px] text-red-500 mt-1">
                                        {custoErr}
                                      </p>
                                    )}
                                  </TableCell>
                                )}
                                {comCusto && (
                                  <TableCell className="px-4 py-2 text-right font-medium align-middle">
                                    {itemTotal.toLocaleString("pt-BR", {
                                      style: "currency",
                                      currency: "BRL",
                                    })}
                                  </TableCell>
                                )}
                                {!readOnly && (
                                  <TableCell className="px-4 py-2 text-center align-middle">
                                    <Button
                                      type="button"
                                      variant="ghost"
                                      size="icon"
                                      className="h-8 w-8 text-destructive hover:bg-destructive/10"
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
                            <TableCell colSpan={2} className="px-4 py-3 text-left">
                              Total Geral
                            </TableCell>
                            <TableCell className="px-4 py-3"></TableCell>
                            <TableCell className="px-4 py-3 text-right text-base text-primary">
                              {totalGeral.toLocaleString("pt-BR", {
                                style: "currency",
                                currency: "BRL",
                              })}
                            </TableCell>
                            {!readOnly && (
                              <TableCell className="px-4 py-3"></TableCell>
                            )}
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
              Permanecer
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
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={saveConfirmOpen} onOpenChange={setSaveConfirmOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Salvar Movimentação?</DialogTitle>
            <DialogDescription>
              Deseja salvar a movimentação de estoque como rascunho ou efetivar
              imediatamente para atualizar o estoque físico?
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="mt-6 flex flex-col sm:flex-row gap-2 justify-end">
            <Button
              type="button"
              variant="outline"
              disabled={mutation.isPending}
              onClick={() => {
                setSaveConfirmOpen(false);
                setPendingPayload(null);
              }}
            >
              Cancelar
            </Button>
            <Button
              type="button"
              variant="secondary"
              disabled={mutation.isPending}
              onClick={() => {
                if (pendingPayload) {
                  mutation.mutate({ values: pendingPayload, efetivar: false });
                  setSaveConfirmOpen(false);
                }
              }}
            >
              Salvar Rascunho
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
              Efetivar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
