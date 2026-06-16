"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { useHotkeys } from "@tanstack/react-hotkeys";
import { useState } from "react";
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
  DialogClose,
} from "@/components/ui/dialog";
import {
  Field,
  FieldGroup,
  FieldLabel,
  FieldError,
} from "@/components/ui/field";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { Textarea } from "@/components/ui/textarea";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { toast } from "sonner";
import { UnidadeMedida } from "@/features/catalogo/unidades-medida/types";
import { Produto, ProdutoFormValues, produtoSchema } from "./types";
import { SkuFormValues, skuFormSchema } from "./types-sku";
import { useQuery } from "@tanstack/react-query";
import { produtosApi, atributosApi } from "@/api/catalogo";
import { Plus, Trash2 } from "lucide-react";
import { Input } from "@/components/ui/input";
import { NumberInput } from "@/components/ui/number-input";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { CategoriaInput } from "@/components/entity-inputs/categoria-input";
import { MarcaInput } from "@/components/entity-inputs/marca-input";
import { UnidadeMedidaInput } from "@/components/entity-inputs/unidade-medida-input";
import { AtributoChaveInput } from "@/components/entity-inputs/atributo-chave-input";
import { AtributoValorMultiInput } from "@/components/entity-inputs/atributo-valor-multi-input";
import { ItemLinha } from "../../estoque/movimentacoes/upsert";
import type { Resultado } from "@/api/types";

interface ProdutosUpsertProps {
  open: boolean;
  editingItem: Produto | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
  onSuccessWithAdjustment?: (items: ItemLinha[]) => void;
}

interface ProdutosUpsertFormProps {
  open: boolean;
  editingItem: Produto | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
  onSuccessWithAdjustment?: (items: ItemLinha[]) => void;
}

interface VariantOption {
  keyId: number;
  keyName: string;
  valores: Array<{ id: number; valor: string }>;
}

export function ProdutosUpsert(props: ProdutosUpsertProps) {
  const { open, editingItem, onClose, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["produtos", "detail", editingItem?.id],
    queryFn: () => produtosApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title={readOnly ? "Visualizar Produto" : "Editar Produto"}
        loading={true}
      />
    );
  }

  return (
    <ProdutosUpsertForm
      open={open}
      editingItem={isEditMode ? (fullItem ?? null) : null}
      onClose={onClose}
      readOnly={readOnly}
      onSuccess={props.onSuccess}
      onSuccessWithAdjustment={props.onSuccessWithAdjustment}
    />
  );
}

function ProdutosUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly = false,
  onSuccessWithAdjustment,
}: ProdutosUpsertFormProps) {
  const isEditMode = !!editingItem;

  const getCustoMedio = (skuCode: string) => {
    if (!editingItem || !editingItem.skus) return 0;
    const match = editingItem.skus.find((s) => s.sku === skuCode);
    return match ? match.custoMedio : 0;
  };

  const [selectedUM, setSelectedUM] = useState<UnidadeMedida | null>(
    editingItem?.unidadeMedida ?? null,
  );
  const [confirmUomTransitionOpen, setConfirmUomTransitionOpen] =
    useState(false);
  const [copyPriceDialogOpen, setCopyPriceDialogOpen] = useState(false);
  const [focusedSkuIndex, setFocusedSkuIndex] = useState<number | null>(null);
  const [pendingFormValue, setPendingFormValue] =
    useState<ProdutoFormValues | null>(null);

  const { data: atributosList } = useQuery({
    queryKey: ["atributos", "list-all"],
    queryFn: () => atributosApi.list(undefined, 1, 100),
  });

  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: ProdutoFormValues) => {
        return editingItem
          ? await produtosApi.update(editingItem.id, value)
          : await produtosApi.create(value);
      },
      queryKey: [["produtos"], ["skus"]],
      onSuccessCallback: (res) => {
        const isTransition =
          editingItem?.unidadeMedida.permiteDecimais &&
          selectedUM &&
          !selectedUM.permiteDecimais;

        const typedRes = res as Resultado<Produto>;

        if (isTransition && onSuccessWithAdjustment && typedRes.data) {
          const product = typedRes.data;
          const itemsToAdjust: ItemLinha[] = product.skus.map((s) => ({
            sku: s.sku,
            produtoNome: product.produto,
            quantidade: Math.floor(Number(s.estoque)),
            custoUnitario: Number(s.custoMedio),
            estoqueAtual: Number(s.estoque),
            precoSugerido: Number(s.preco),
            custoMedio: Number(s.custoMedio),
            custoUltimaCompra: Number(s.custoUltimaCompra),
            unidadeMedidaSigla: product.unidadeMedida.sigla,
            permiteDecimais: false,
          }));
          onSuccessWithAdjustment(itemsToAdjust);
        } else {
          onSuccess();
        }
      },
      onClose: onClose,
    });

  const [hasVariants, setHasVariants] = useState<boolean>(() => {
    if (editingItem && editingItem.skus) {
      return editingItem.skus.some(
        (s) => s.atributos && s.atributos.length > 0,
      );
    }
    return false;
  });

  const [options, setOptions] = useState<VariantOption[]>(() => {
    if (editingItem && editingItem.skus) {
      const skus = editingItem.skus;
      const tempOptionsMap: Record<
        number,
        { keyName: string; valuesMap: Record<number, string> }
      > = {};

      for (const sku of skus) {
        if (!sku.atributos) continue;
        for (const attr of sku.atributos) {
          if (!tempOptionsMap[attr.chaveId]) {
            tempOptionsMap[attr.chaveId] = {
              keyName: `Atributo #${attr.chaveId}`,
              valuesMap: {},
            };
          }
          tempOptionsMap[attr.chaveId].valuesMap[attr.id] = attr.valor;
        }
      }

      return Object.entries(tempOptionsMap).map(([keyIdStr, item]) => {
        const keyId = parseInt(keyIdStr, 10);
        return {
          keyId,
          keyName: item.keyName,
          valores: Object.entries(item.valuesMap).map(([vIdStr, valor]) => ({
            id: parseInt(vIdStr, 10),
            valor,
          })),
        };
      });
    }
    return [];
  });

  useHotkeys(
    [
      {
        hotkey: "Alt+O",
        callback: (e: KeyboardEvent) => {
          e.preventDefault();
          setHasVariants(true);
          setOptions((prev) => [
            ...prev,
            { keyId: 0, keyName: "", valores: [] },
          ]);
        },
        options: {
          enabled: open,
          ignoreInputs: false,
        },
      },
      {
        hotkey: "Alt+C",
        callback: (e: KeyboardEvent) => {
          e.preventDefault();
          const skus = form.getFieldValue("skus");
          if (skus && skus.length > 1) {
            setCopyPriceDialogOpen(true);
          }
        },
        options: {
          enabled: open && hasVariants && !readOnly,
          ignoreInputs: false,
        },
      },
    ],
    { conflictBehavior: "allow" },
  );

  const handleCopyPrice = () => {
    const skus = [...form.getFieldValue("skus")];
    const indexToCopy = focusedSkuIndex ?? 0;

    if (skus.length > 1) {
      const sourcePrice = skus[indexToCopy].preco;
      const updatedSkus = skus.map((s) => ({ ...s, preco: sourcePrice }));
      form.setFieldValue("skus", updatedSkus);
      toast.success(
        `Preço (R$ ${sourcePrice.toFixed(2)}) replicado para todas as variações.`,
      );
    }
    setCopyPriceDialogOpen(false);
  };

  const getDisplayKeyName = (option: VariantOption) => {
    const found = atributosList?.itens?.find(
      (item) => item.id === option.keyId,
    );
    return found?.chave || option.keyName || `Atributo #${option.keyId}`;
  };

  const getInitialSkus = (): SkuFormValues[] => {
    if (editingItem && editingItem.skus && editingItem.skus.length > 0) {
      return editingItem.skus.map((s) => ({
        sku: s.sku,
        preco: s.preco,
        gtinEan: s.gtinEan ?? "",
        ativo: s.ativo,
        atributoValorIds: s.atributos?.map((v) => v.id) ?? [],
        estoque: s.estoque,
      }));
    }
    return [
      {
        sku: "",
        preco: 0,
        gtinEan: "",
        ativo: true,
        atributoValorIds: [],
        estoque: 0,
      },
    ];
  };

  const form = useForm({
    defaultValues: {
      produto: editingItem?.produto ?? "",
      descricao: editingItem?.descricao ?? "",
      ativo: editingItem?.ativo ?? true,
      categoriaId: editingItem?.categoria?.id ?? 0,
      marcaId: editingItem?.marca?.id ?? 0,
      unidadeMedidaId: editingItem?.unidadeMedida?.id ?? 0,
      skus: getInitialSkus(),
    } as ProdutoFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();

      const isChangingToNonDecimal =
        editingItem?.unidadeMedida.permiteDecimais &&
        selectedUM &&
        !selectedUM.permiteDecimais;

      if (isChangingToNonDecimal) {
        setPendingFormValue(value);
        setConfirmUomTransitionOpen(true);
        return;
      }

      const submissionValue = { ...value };
      if (!hasVariants) {
        const singleSku = { ...submissionValue.skus[0], atributoValorIds: [] };
        submissionValue.skus = [singleSku];
      }
      mutation.mutate(submissionValue);
    },
  });

  const confirmUomTransition = () => {
    if (pendingFormValue) {
      const submissionValue = { ...pendingFormValue };
      if (!hasVariants) {
        const singleSku = { ...submissionValue.skus[0], atributoValorIds: [] };
        submissionValue.skus = [singleSku];
      }
      mutation.mutate(submissionValue);
      setConfirmUomTransitionOpen(false);
    }
  };

  const generateCartesianCombinations = (
    opts: VariantOption[],
  ): SkuFormValues[] => {
    const activeOpts = opts.filter((o) => o.valores && o.valores.length > 0);
    if (activeOpts.length === 0) return [];

    const combine = (
      index: number,
      current: Array<{ id: number; valor: string }>,
    ): Array<Array<{ id: number; valor: string }>> => {
      if (index === activeOpts.length) {
        return [current];
      }

      const results: Array<Array<{ id: number; valor: string }>> = [];
      const currentOpt = activeOpts[index];

      for (const val of currentOpt.valores) {
        results.push(...combine(index + 1, [...current, val]));
      }

      return results;
    };

    const combinations = combine(0, []);
    const productId = editingItem?.id || "";

    return combinations.map((combo, index) => {
      const currentSkus = form.getFieldValue("skus") || [];
      const valueIds = combo.map((c) => c.id).sort();

      const match = currentSkus.find((s) => {
        if (!s.atributoValorIds) return false;
        const sortedIds = [...s.atributoValorIds].sort();
        return JSON.stringify(sortedIds) === JSON.stringify(valueIds);
      });

      if (match) {
        return {
          ...match,
        };
      }

      // Pre-generate SKU based on product ID and sequential index
      const generatedSku = productId ? `${productId}${index + 1}` : "";

      return {
        sku: generatedSku,
        preco: 0,
        gtinEan: "",
        ativo: true,
        atributoValorIds: combo.map((c) => c.id),
      } as SkuFormValues;
    });
  };

  const handleUpdateOptions = (newOptions: VariantOption[]) => {
    setOptions(newOptions);
    const newCombinations = generateCartesianCombinations(newOptions);
    form.setFieldValue("skus", newCombinations);
  };

  const getVariationLabel = (valueIds: number[] | undefined) => {
    if (!valueIds || valueIds.length === 0) return "";

    const labels: string[] = [];

    for (const option of options) {
      const foundVal = option.valores.find((v) => valueIds.includes(v.id));
      if (foundVal) {
        labels.push(foundVal.valor);
      }
    }

    return labels.join(" / ");
  };

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Produto" : "Novo Produto"}
      footer={
        <>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancelar <Kbd>Esc</Kbd>
            </Button>
          </DialogClose>
          <form.Subscribe
            selector={(state) => [state.canSubmit, state.isSubmitting]}
          >
            {([canSubmit, isSubmitting]) => (
              <Button
                type="submit"
                form="upsert-produtos"
                disabled={!canSubmit || isSubmitting}
              >
                {isSubmitting ? (
                  "Salvando..."
                ) : (
                  <span className="flex items-center gap-2">
                    Salvar Produto{" "}
                    <KbdGroup>
                      <Kbd>Alt</Kbd>
                      <Kbd>Enter</Kbd>
                    </KbdGroup>
                  </span>
                )}
              </Button>
            )}
          </form.Subscribe>
        </>
      }
    >
      <form
        id="upsert-produtos"
        className="flex flex-col gap-6"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <FieldGroup className="gap-4">
          <div className="flex w-full flex-wrap items-start gap-4">
            {editingItem && (
              <div className="w-fit">
                <div className="flex flex-col gap-1.5">
                  <FieldLabel>Código</FieldLabel>
                  <Input
                    value={editingItem.id}
                    disabled
                    className="h-8 text-xs"
                    inputSize="small"
                  />
                </div>
              </div>
            )}
            <div className="min-w-48 flex-1">
              <form.Field
                name="produto"
                validators={{ onChange: produtoSchema.shape.produto }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Nome do Produto"
                    inputSize="full"
                    getFieldError={getFieldError}
                    maxLength={150}
                    placeholder="Ex: Camiseta"
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="descricao"
            validators={{ onChange: produtoSchema.shape.descricao }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors);
              return (
                <Field data-invalid={!!error} className="w-full">
                  <FieldLabel htmlFor={field.name}>Descrição</FieldLabel>
                  <Textarea
                    id={field.name}
                    rows={3}
                    value={field.state.value ?? ""}
                    onChange={(e) => field.handleChange(e.target.value)}
                    placeholder="Descrição do produto..."
                  />
                  {error && <FieldError>{error}</FieldError>}
                </Field>
              );
            }}
          </form.Field>

          <div className="flex flex-wrap items-start gap-4">
            <div className="min-w-50 flex-1">
              <form.Field
                name="categoriaId"
                validators={{
                  onChange: ({ value }) => {
                    const res =
                      produtoSchema.shape.categoriaId.safeParse(value);
                    return res.success
                      ? undefined
                      : res.error.errors[0]?.message;
                  },
                }}
              >
                {(field) => (
                  <CategoriaInput
                    name={field.name}
                    label="Categoria"
                    error={getFieldError(field.name, field.state.meta.errors)}
                    initialItem={editingItem?.categoria}
                    onSelectId={(id) => field.handleChange(id as number)}
                  />
                )}
              </form.Field>
            </div>

            <div className="min-w-50 flex-1">
              <form.Field
                name="marcaId"
                validators={{
                  onChange: ({ value }) => {
                    const res = produtoSchema.shape.marcaId.safeParse(value);
                    return res.success
                      ? undefined
                      : res.error.errors[0]?.message;
                  },
                }}
              >
                {(field) => (
                  <MarcaInput
                    name={field.name}
                    label="Marca"
                    error={getFieldError(field.name, field.state.meta.errors)}
                    initialItem={editingItem?.marca}
                    onSelectId={(id) => field.handleChange(id as number)}
                  />
                )}
              </form.Field>
            </div>

            <div className="min-w-50 flex-1">
              <form.Field
                name="unidadeMedidaId"
                validators={{
                  onChange: ({ value }) => {
                    const res =
                      produtoSchema.shape.unidadeMedidaId.safeParse(value);
                    return res.success
                      ? undefined
                      : res.error.errors[0]?.message;
                  },
                }}
              >
                {(field) => (
                  <UnidadeMedidaInput
                    name={field.name}
                    label="Unidade de Medida"
                    error={getFieldError(field.name, field.state.meta.errors)}
                    initialItem={editingItem?.unidadeMedida}
                    onSelectItem={(item) => {
                      setSelectedUM(item);
                      field.handleChange(item?.id as number);
                    }}
                    onSelectId={() => {}}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field name="ativo">
            {(field) => (
              <Field orientation="horizontal">
                <Checkbox
                  id={field.name}
                  checked={field.state.value}
                  onCheckedChange={(checked) => field.handleChange(!!checked)}
                />
                <FieldLabel
                  htmlFor={field.name}
                  className="cursor-pointer font-normal"
                >
                  Ativo
                </FieldLabel>
              </Field>
            )}
          </form.Field>

          <div className="flex items-center gap-2 pt-2">
            <Checkbox
              id="toggle-variantes"
              checked={hasVariants}
              onCheckedChange={(checked) => {
                const val = !!checked;
                setHasVariants(val);
                if (val) {
                  const combs = generateCartesianCombinations(options);
                  form.setFieldValue(
                    "skus",
                    combs.length > 0
                      ? combs.map((c) => ({ ...c, sku: c.sku ?? "" }))
                      : [],
                  );
                } else {
                  form.setFieldValue("skus", [
                    {
                      sku: "",
                      preco: 0,
                      gtinEan: "",
                      ativo: true,
                      atributoValorIds: [],
                    },
                  ]);
                }
              }}
            />
            <FieldLabel
              htmlFor="toggle-variantes"
              className="cursor-pointer text-sm font-medium"
            >
              Este produto possui variações
            </FieldLabel>
          </div>

          {!hasVariants ? (
            <div className="grid grid-cols-1 gap-4 md:grid-cols-5">
              <form.Field
                name="skus[0].sku"
                validators={{ onChange: skuFormSchema.shape.sku }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Código SKU"
                    inputSize="full"
                    getFieldError={getFieldError}
                    maxLength={50}
                    placeholder="Ex: 104082"
                  />
                )}
              </form.Field>

              <form.Field
                name="skus[0].preco"
                validators={{ onChange: skuFormSchema.shape.preco }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Preço de Venda (R$)"
                    inputSize="full"
                    type="number"
                    decimals={2}
                    getFieldError={getFieldError}
                    placeholder="0,00"
                  />
                )}
              </form.Field>

              <form.Field
                name="skus[0].gtinEan"
                validators={{ onChange: skuFormSchema.shape.gtinEan }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Código de Barras (EAN)"
                    inputSize="full"
                    getFieldError={getFieldError}
                    maxLength={14}
                    placeholder="EAN / GTIN"
                  />
                )}
              </form.Field>

              <form.Field name="skus[0].estoque">
                {(field) => (
                  <div className="flex flex-col gap-1.5">
                    <FieldLabel>Estoque Atual</FieldLabel>
                    <Input
                      value={field.state.value}
                      disabled
                      className="bg-muted/50 h-8 font-mono text-xs"
                      inputSize="full"
                    />
                  </div>
                )}
              </form.Field>

              {isEditMode && editingItem && editingItem.skus?.[0] && (
                <div className="flex flex-col gap-1.5">
                  <FieldLabel className="text-right font-semibold">Custo Médio</FieldLabel>
                  <Input
                    value={editingItem.skus[0].custoMedio.toLocaleString("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    })}
                    disabled
                    className="bg-muted/50 h-8 text-right font-semibold"
                    inputSize="full"
                  />
                </div>
              )}
            </div>
          ) : (
            <div className="flex flex-col gap-4">
              <div className="flex flex-col gap-4">
                <div className="flex items-center justify-between border-b pb-2">
                  <span className="text-sm font-medium">Opções do Produto</span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="h-8 gap-1.5"
                    onClick={() => {
                      const newOptions = [
                        ...options,
                        { keyId: 0, keyName: "", valores: [] },
                      ];
                      setOptions(newOptions);
                    }}
                  >
                    <Plus className="size-3.5" /> Adicionar Opção{" "}
                    <KbdGroup>
                      <Kbd>Alt</Kbd>
                      <Kbd>O</Kbd>
                    </KbdGroup>
                  </Button>
                </div>

                {options.length === 0 ? (
                  <div className="text-muted-foreground py-4 text-center text-sm">
                    Nenhuma opção adicionada.
                  </div>
                ) : (
                  <div className="flex flex-col gap-4">
                    {options.map((option, optIdx) => (
                      <div
                        key={optIdx}
                        className="flex w-full flex-col items-stretch gap-4 border-b pb-4 md:flex-row md:items-end md:border-0 md:pb-0"
                      >
                        <div className="w-full shrink-0 md:w-114">
                          <AtributoChaveInput
                            name={`option-key-${optIdx}`}
                            label={`Opção #${optIdx + 1}`}
                            initialItem={
                              option.keyId > 0
                                ? {
                                    id: option.keyId,
                                    chave: getDisplayKeyName(option),
                                    skuAtributosValores: [],
                                  }
                                : null
                            }
                            onSelectId={(id) => {
                              if (!id) {
                                const updated = [...options];
                                updated[optIdx] = {
                                  keyId: 0,
                                  keyName: "",
                                  valores: [],
                                };
                                handleUpdateOptions(updated);
                                return;
                              }
                              const found = atributosList?.itens?.find(
                                (item) => item.id === id,
                              );
                              if (found) {
                                const updated = [...options];
                                updated[optIdx] = {
                                  keyId: id,
                                  keyName: found.chave,
                                  valores: [],
                                };
                                handleUpdateOptions(updated);
                              }
                            }}
                          />
                        </div>

                        <div className="w-full md:flex-1">
                          {option.keyId === 0 ? (
                            <div className="text-muted-foreground flex h-8 items-center text-sm">
                              Selecione um atributo para listar os valores
                              possíveis.
                            </div>
                          ) : (
                            <AtributoValorMultiInput
                              chaveId={option.keyId}
                              selectedValues={option.valores}
                              onChange={(newVals) => {
                                const updated = [...options];
                                updated[optIdx].valores = newVals;
                                handleUpdateOptions(updated);
                              }}
                            />
                          )}
                        </div>

                        <div className="flex shrink-0 justify-end">
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="text-muted-foreground hover:text-destructive h-8 w-8"
                            onClick={() => {
                              const updated = options.filter(
                                (_, idx) => idx !== optIdx,
                              );
                              handleUpdateOptions(updated);
                            }}
                          >
                            <Trash2 className="size-4" />
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {form.getFieldValue("skus") &&
                form.getFieldValue("skus").length > 0 && (
                  <div className="flex flex-col gap-3">
                    <div className="flex items-center justify-between border-b pb-2">
                      <span className="text-sm font-medium">
                        Variações de SKU
                      </span>
                      {!readOnly && form.getFieldValue("skus").length > 1 && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          className="text-muted-foreground text-md h-7 gap-1.5 px-2"
                          onClick={() => setCopyPriceDialogOpen(true)}
                        >
                          Replicar Preço Focado
                          <KbdGroup>
                            <Kbd>Alt</Kbd>
                            <Kbd>C</Kbd>
                          </KbdGroup>
                        </Button>
                      )}
                    </div>

                    <div className="bg-card overflow-hidden rounded-lg border">
                      <Table>
                        <TableHeader className="bg-muted border-b">
                          <TableRow className="border-b hover:bg-transparent">
                            <TableHead className="text-foreground h-10 px-4 py-2 text-left text-sm font-semibold">
                              Código SKU
                            </TableHead>
                            <TableHead className="text-foreground h-10 px-4 py-2 text-left text-sm font-semibold">
                              Variação
                            </TableHead>
                            <TableHead className="text-foreground h-10 px-4 py-2 text-left text-sm font-semibold">
                              Preço (R$)
                            </TableHead>
                            <TableHead className="text-foreground h-10 px-4 py-2 text-left text-sm font-semibold">
                              Barras (EAN)
                            </TableHead>
                            <TableHead className="text-foreground h-10 px-4 py-2 text-right text-sm font-semibold">
                              Estoque
                            </TableHead>
                            {isEditMode && (
                              <TableHead className="text-foreground h-10 px-4 py-2 text-right text-sm font-semibold">
                                Custo Médio
                              </TableHead>
                            )}
                            <TableHead className="text-foreground h-10 px-4 py-2 text-center text-sm font-semibold">
                              Ativo
                            </TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {form.getFieldValue("skus").map((sku, index) => (
                            <TableRow
                              key={index}
                              className="group hover:bg-muted/10 border-b last:border-0"
                            >
                              <TableCell className="px-4 py-2.5 align-middle">
                                <form.Field
                                  name={`skus[${index}].sku`}
                                  validators={{
                                    onChange: skuFormSchema.shape.sku,
                                  }}
                                >
                                  {(field) => {
                                    const error = getFieldError(
                                      field.name,
                                      field.state.meta.errors,
                                    );
                                    return (
                                      <div className="flex flex-col gap-1">
                                        <Input
                                          inputSize="full"
                                          value={field.state.value}
                                          onChange={(e) =>
                                            field.handleChange(e.target.value)
                                          }
                                          onFocus={() =>
                                            setFocusedSkuIndex(index)
                                          }
                                          maxLength={50}
                                          placeholder="Ex: 104082"
                                          className={cn(
                                            "text-foreground/90 h-8 font-mono text-sm font-bold",
                                            error &&
                                              "border-destructive focus-visible:ring-destructive",
                                          )}
                                        />
                                        {error && (
                                          <span className="text-[10px] font-medium text-red-500">
                                            {error}
                                          </span>
                                        )}
                                      </div>
                                    );
                                  }}
                                </form.Field>
                              </TableCell>
                              <TableCell className="text-foreground/90 px-4 py-2.5 align-middle text-sm font-medium">
                                {getVariationLabel(sku.atributoValorIds) ||
                                  `Variante #${index + 1}`}
                              </TableCell>
                              <TableCell className="px-4 py-2.5 align-middle">
                                <form.Field
                                  name={`skus[${index}].preco`}
                                  validators={{
                                    onChange: skuFormSchema.shape.preco,
                                  }}
                                >
                                  {(field) => {
                                    const error = getFieldError(
                                      field.name,
                                      field.state.meta.errors,
                                    );
                                    return (
                                      <div className="flex flex-col gap-1">
                                        <NumberInput
                                          inputSize="full"
                                          decimals={2}
                                          value={field.state.value}
                                          onFocus={() =>
                                            setFocusedSkuIndex(index)
                                          }
                                          onNumberChange={(num) => {
                                            field.handleChange(num);
                                          }}
                                          placeholder="0,00"
                                          className={cn(
                                            "h-8 text-sm font-medium",
                                            error &&
                                              "border-destructive focus-visible:ring-destructive",
                                          )}
                                        />
                                        {error && (
                                          <span className="text-[10px] font-medium text-red-500">
                                            {error}
                                          </span>
                                        )}
                                      </div>
                                    );
                                  }}
                                </form.Field>
                              </TableCell>
                              <TableCell className="px-4 py-2.5 align-middle">
                                <form.Field
                                  name={`skus[${index}].gtinEan`}
                                  validators={{
                                    onChange: skuFormSchema.shape.gtinEan,
                                  }}
                                >
                                  {(field) => {
                                    const error = getFieldError(
                                      field.name,
                                      field.state.meta.errors,
                                    );
                                    return (
                                      <div className="flex flex-col gap-1">
                                        <Input
                                          inputSize="full"
                                          value={field.state.value || ""}
                                          onChange={(e) =>
                                            field.handleChange(e.target.value)
                                          }
                                          onFocus={() =>
                                            setFocusedSkuIndex(index)
                                          }
                                          maxLength={14}
                                          placeholder="EAN"
                                          className={cn(
                                            "h-8 text-sm",
                                            error &&
                                              "border-destructive focus-visible:ring-destructive",
                                          )}
                                        />
                                        {error && (
                                          <span className="text-[10px] font-medium text-red-500">
                                            {error}
                                          </span>
                                        )}
                                      </div>
                                    );
                                  }}
                                </form.Field>
                              </TableCell>
                              <TableCell className="px-4 py-2.5 text-right align-middle">
                                <form.Field name={`skus[${index}].estoque`}>
                                  {(field) => (
                                    <span className="text-foreground/90 font-mono text-sm font-bold">
                                      {field.state.value ?? 0}
                                    </span>
                                  )}
                                </form.Field>
                              </TableCell>
                              {isEditMode && (
                                <TableCell className="px-4 py-2.5 text-right align-middle">
                                  <span className="text-foreground/90 font-semibold">
                                    {getCustoMedio(sku.sku || "").toLocaleString("pt-BR", {
                                      style: "currency",
                                      currency: "BRL",
                                    })}
                                  </span>
                                </TableCell>
                              )}
                              <TableCell className="px-4 py-2.5 text-center align-middle">
                                <form.Field name={`skus[${index}].ativo`}>
                                  {(field) => (
                                    <Checkbox
                                      checked={field.state.value}
                                      onFocus={() => setFocusedSkuIndex(index)}
                                      onCheckedChange={(checked) =>
                                        field.handleChange(!!checked)
                                      }
                                    />
                                  )}
                                </form.Field>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </div>
                    <form.Field name="skus">
                      {(field) => {
                        const error = getFieldError(
                          field.name,
                          field.state.meta.errors,
                        );
                        return error ? (
                          <p className="text-destructive mt-2 text-xs font-medium">
                            {error}
                          </p>
                        ) : null;
                      }}
                    </form.Field>
                  </div>
                )}
            </div>
          )}
        </FieldGroup>

        {globalError && (
          <Alert variant="destructive">
            <AlertDescription>{globalError}</AlertDescription>
          </Alert>
        )}
      </form>

      <Dialog
        open={confirmUomTransitionOpen}
        onOpenChange={setConfirmUomTransitionOpen}
      >
        <DialogContent
          className="max-w-md"
          onKeyDown={(e) => {
            if (e.altKey && e.key === "Enter") {
              e.preventDefault();
              confirmUomTransition();
            }
          }}
        >
          <DialogHeader>
            <DialogTitle>Ajuste de Estoque Necessário</DialogTitle>
            <DialogDescription>
              Você está alterando a unidade de medida para uma que{" "}
              <strong>não permite quantidades decimais</strong> (ex: Unidade).
              <br />
              <br />
              Isso exige um ajuste de estoque obrigatório para garantir que
              todos os SKUs tenham quantidades inteiras. Uma movimentação de
              balanço será gerada automaticamente após salvar.
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="mt-4 flex justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setConfirmUomTransitionOpen(false)}
            >
              Cancelar <Kbd>Esc</Kbd>
            </Button>
            <Button
              type="button"
              variant="default"
              onClick={confirmUomTransition}
            >
              Entendi e Confirmar
              <KbdGroup className="ml-2">
                <Kbd>Alt</Kbd>
                <Kbd>Enter</Kbd>
              </KbdGroup>
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={copyPriceDialogOpen} onOpenChange={setCopyPriceDialogOpen}>
        <DialogContent
          className="max-w-md"
          onKeyDown={(e) => {
            if (e.altKey && e.key === "Enter") {
              e.preventDefault();
              handleCopyPrice();
            }
          }}
        >
          <DialogHeader>
            <DialogTitle>Replicar Preço?</DialogTitle>
            <DialogDescription>
              Deseja realmente copiar o preço da variação{" "}
              <strong>
                {getVariationLabel(
                  form.getFieldValue("skus")[focusedSkuIndex ?? 0]
                    ?.atributoValorIds,
                ) || "selecionada"}
              </strong>{" "}
              (
              <strong>
                {form
                  .getFieldValue("skus")
                  [focusedSkuIndex ?? 0]?.preco.toLocaleString("pt-BR", {
                    style: "currency",
                    currency: "BRL",
                  })}
              </strong>
              ) para todas as outras variações deste produto?
            </DialogDescription>
          </DialogHeader>

          <DialogFooter className="mt-4 flex justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setCopyPriceDialogOpen(false)}
            >
              Cancelar <Kbd>Esc</Kbd>
            </Button>
            <Button type="button" variant="default" onClick={handleCopyPrice}>
              Replicar Preço
              <KbdGroup className="ml-2">
                <Kbd>Alt</Kbd>
                <Kbd>Enter</Kbd>
              </KbdGroup>
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </UpsertDialog>
  );
}
