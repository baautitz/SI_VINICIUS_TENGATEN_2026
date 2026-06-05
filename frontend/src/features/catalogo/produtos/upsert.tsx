"use client";

import React from "react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import {
  Field,
  FieldGroup,
  FieldLabel,
  FieldError,
} from "@/components/ui/field";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import {
  produtoBaseSchema,
  skuFormSchema,
  Produto,
  ProdutoResumo,
  ProdutoFormValues,
  VariantOption,
  SkuFormValues,
} from "./types";
import type { CategoriaResumo } from "@/features/catalogo/categorias/types";
import type { MarcaResumo } from "@/features/catalogo/marcas/types";
import type { UnidadeMedidaResumo } from "@/features/catalogo/unidades-medida/types";
import { useQuery } from "@tanstack/react-query";
import { produtosApi, atributosApi } from "@/api/catalogo";
import { Plus, Trash2 } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { CategoriaInput } from "@/components/entity-inputs/categoria-input";
import { MarcaInput } from "@/components/entity-inputs/marca-input";
import { UnidadeMedidaInput } from "@/components/entity-inputs/unidade-medida-input";
import { AtributoChaveInput } from "@/components/entity-inputs/atributo-chave-input";
import { AtributoValorMultiInput } from "@/components/entity-inputs/atributo-valor-multi-input";

interface ProdutosUpsertProps {
  open: boolean;
  editingItem: ProdutoResumo | null;
  onClose: () => void;
  onSuccess: () => void;
}

interface ProdutosUpsertFormProps {
  open: boolean;
  editingItem: Produto | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function ProdutosUpsert(props: ProdutosUpsertProps) {
  const { open, editingItem, onClose } = props;
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
        title="Editar Produto"
        loading={true}
      />
    );
  }

  return (
    <ProdutosUpsertForm
      open={open}
      editingItem={isEditMode ? (fullItem ?? null) : null}
      onClose={onClose}
      onSuccess={props.onSuccess}
    />
  );
}

function ProdutosUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: ProdutosUpsertFormProps) {
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
      queryKey: ["produtos"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const [hasVariants, setHasVariants] = React.useState<boolean>(() => {
    if (editingItem && editingItem.skus) {
      return editingItem.skus.some(
        (s) => s.skuAtributosValores && s.skuAtributosValores.length > 0,
      );
    }
    return false;
  });

  const [options, setOptions] = React.useState<VariantOption[]>(() => {
    if (editingItem && editingItem.skus) {
      const skus = editingItem.skus;
      const tempOptionsMap: Record<
        number,
        { keyName: string; valuesMap: Record<number, string> }
      > = {};

      for (const sku of skus) {
        for (const attr of sku.skuAtributosValores) {
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

  const getDisplayKeyName = (option: VariantOption) => {
    const found = atributosList?.itens?.find(
      (item) => item.id === option.keyId,
    );
    return found?.chave || option.keyName || `Atributo #${option.keyId}`;
  };

  const getInitialSkus = (): SkuFormValues[] => {
    if (editingItem && editingItem.skus && editingItem.skus.length > 0) {
      return editingItem.skus.map((s) => {
        const attributeIds = s.skuAtributosValores?.map((v) => v.id) ?? [];
        const variantLabel =
          s.skuAtributosValores?.map((v) => v.valor).join(" / ") ?? "";
        return {
          sku: s.sku,
          preco: s.preco,
          estoque: s.estoque,
          gtinEan: s.gtinEan ?? "",
          ativo: s.ativo,
          atributoValorIds: attributeIds,
          variantLabel,
        };
      });
    }
    return [
      {
        sku: "",
        preco: 0,
        estoque: 0,
        gtinEan: "",
        ativo: true,
        atributoValorIds: [],
        variantLabel: "",
      },
    ];
  };

  const form = useForm({
    defaultValues: {
      produto: editingItem?.produto ?? "",
      descricao: editingItem?.descricao ?? "",
      categoriaId: editingItem?.categoria?.id ?? null,
      marcaId: editingItem?.marca?.id ?? null,
      unidadeMedidaId: editingItem?.unidadeMedida?.id ?? null,
      ativo: editingItem?.ativo ?? true,
      skus: getInitialSkus(),
    } as ProdutoFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const submissionValue = { ...value };
      if (!hasVariants) {
        const singleSku = { ...submissionValue.skus[0], atributoValorIds: [] };
        submissionValue.skus = [singleSku];
      }
      mutation.mutate(submissionValue);
    },
  });

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

    return combinations.map((combo) => {
      const label = combo.map((c) => c.valor).join(" / ");

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
          variantLabel: label,
        };
      }

      return {
        sku: "",
        preco: 0,
        estoque: 0,
        gtinEan: "",
        ativo: true,
        atributoValorIds: combo.map((c) => c.id),
        variantLabel: label,
      };
    });
  };

  const handleUpdateOptions = (newOptions: VariantOption[]) => {
    setOptions(newOptions);
    const newCombinations = generateCartesianCombinations(newOptions);
    form.setFieldValue("skus", newCombinations);
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
              Cancelar
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
                {isSubmitting ? "Salvando..." : "Salvar Produto"}
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
          <div className="flex flex-wrap gap-4 items-start w-full">
            {editingItem && (
              <div className="w-24 shrink-0">
                <div className="flex flex-col gap-1.5">
                  <FieldLabel>Código</FieldLabel>
                  <Input
                    value={editingItem.id}
                    disabled
                    className="h-8 text-xs font-mono"
                    inputSize="small"
                  />
                </div>
              </div>
            )}
            <div className="flex-1 min-w-48">
              <form.Field
                name="produto"
                validators={{ onChange: produtoBaseSchema.shape.produto }}
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
            validators={{ onChange: produtoBaseSchema.shape.descricao }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors);
              return (
                <Field data-invalid={!!error} className="w-full">
                  <FieldLabel htmlFor={field.name}>Descrição</FieldLabel>
                  <textarea
                    id={field.name}
                    rows={3}
                    className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
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
            <div className="flex-1 min-w-50">
              <form.Field
                name="categoriaId"
                validators={{
                  onChange: ({ value }) => {
                    const res = produtoBaseSchema.shape.categoriaId.safeParse(value);
                    return res.success ? undefined : res.error.errors[0]?.message;
                  },
                }}
              >
                {(field) => (
                  <CategoriaInput
                    name={field.name}
                    label="Categoria"
                    error={getFieldError(field.name, field.state.meta.errors)}
                    initialItem={
                      editingItem?.categoria
                        ? (editingItem.categoria as unknown as CategoriaResumo)
                        : null
                    }
                    onSelectId={(id) => field.handleChange(id as unknown as number)}
                  />
                )}
              </form.Field>
            </div>

            <div className="flex-1 min-w-50">
              <form.Field
                name="marcaId"
                validators={{
                  onChange: ({ value }) => {
                    const res = produtoBaseSchema.shape.marcaId.safeParse(value);
                    return res.success ? undefined : res.error.errors[0]?.message;
                  },
                }}
              >
                {(field) => (
                  <MarcaInput
                    name={field.name}
                    label="Marca"
                    error={getFieldError(field.name, field.state.meta.errors)}
                    initialItem={
                      editingItem?.marca
                        ? (editingItem.marca as unknown as MarcaResumo)
                        : null
                    }
                    onSelectId={(id) => field.handleChange(id as unknown as number)}
                  />
                )}
              </form.Field>
            </div>

            <div className="flex-1 min-w-50">
              <form.Field
                name="unidadeMedidaId"
                validators={{
                  onChange: ({ value }) => {
                    const res = produtoBaseSchema.shape.unidadeMedidaId.safeParse(value);
                    return res.success ? undefined : res.error.errors[0]?.message;
                  },
                }}
              >
                {(field) => (
                  <UnidadeMedidaInput
                    name={field.name}
                    label="Unidade de Medida"
                    error={getFieldError(field.name, field.state.meta.errors)}
                    initialItem={
                      editingItem?.unidadeMedida
                        ? (editingItem.unidadeMedida as unknown as UnidadeMedidaResumo)
                        : null
                    }
                    onSelectId={(id) => field.handleChange(id as unknown as number)}
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
                  form.setFieldValue("skus", combs.length > 0 ? combs : []);
                } else {
                  form.setFieldValue("skus", [
                    {
                      sku: "",
                      preco: 0,
                      estoque: 0,
                      gtinEan: "",
                      ativo: true,
                      atributoValorIds: [],
                      variantLabel: "",
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
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
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
                    getFieldError={getFieldError}
                    placeholder="0,00"
                  />
                )}
              </form.Field>

              <form.Field
                name="skus[0].estoque"
                validators={{ onChange: skuFormSchema.shape.estoque }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label={editingItem ? "Estoque Atual" : "Estoque Inicial"}
                    inputSize="full"
                    type="number"
                    getFieldError={getFieldError}
                    placeholder="0"
                    disabled={!!editingItem}
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
                    <Plus className="size-3.5" /> Adicionar Opção
                  </Button>
                </div>

                {options.length === 0 ? (
                  <div className="text-center py-4 text-sm text-muted-foreground">
                    Nenhuma opção adicionada.
                  </div>
                ) : (
                  <div className="flex flex-col gap-4">
                    {options.map((option, optIdx) => (
                      <div
                        key={optIdx}
                        className="flex flex-col md:flex-row gap-4 items-stretch md:items-end w-full border-b pb-4 md:border-0 md:pb-0"
                      >
                        <div className="w-full md:w-114 shrink-0">
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
                            <div className="text-sm text-muted-foreground h-8 flex items-center">
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

                        <div className="flex justify-end shrink-0">
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
                    </div>

                    <div className="overflow-x-auto border rounded-lg">
                      <table className="w-full text-sm text-left border-collapse">
                        <thead>
                          <tr className="bg-muted/40 border-b text-xs font-semibold text-muted-foreground uppercase">
                            <th className="p-3">Código SKU</th>
                            <th className="p-3">Variação</th>
                            <th className="p-3">Preço (R$)</th>
                            <th className="p-3">
                              {editingItem
                                ? "Estoque Atual"
                                : "Estoque Inicial"}
                            </th>
                            <th className="p-3">Barras (EAN)</th>
                            <th className="p-3 text-center">Ativo</th>
                          </tr>
                        </thead>
                        <tbody>
                          {form.getFieldValue("skus").map((sku, index) => (
                            <tr
                              key={index}
                              className="border-b hover:bg-muted/10 last:border-0"
                            >
                              <td className="p-3">
                                <form.Field
                                  name={`skus[${index}].sku`}
                                  validators={{
                                    onChange: ({ value }) => {
                                      const val = value?.trim().toLowerCase();
                                      if (!val) return undefined;
                                      const allSkus =
                                        form.getFieldValue("skus") || [];
                                      const isDuplicate = allSkus.some(
                                        (s, idx) =>
                                          idx !== index &&
                                          s.sku?.trim().toLowerCase() === val,
                                      );
                                      return isDuplicate
                                        ? "SKU duplicado"
                                        : undefined;
                                    },
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
                                          maxLength={50}
                                          placeholder="Ex: 104082"
                                          className={cn(
                                            "h-8 text-xs font-mono",
                                            error &&
                                              "border-destructive focus-visible:ring-destructive",
                                          )}
                                        />
                                        {error && (
                                          <span className="text-[10px] font-medium text-destructive">
                                            {error}
                                          </span>
                                        )}
                                      </div>
                                    );
                                  }}
                                </form.Field>
                              </td>
                              <td className="p-3 font-medium text-foreground">
                                {sku.variantLabel || `Variante #${index + 1}`}
                              </td>
                              <td className="p-3">
                                <form.Field
                                  name={`skus[${index}].preco`}
                                  validators={{ onChange: skuFormSchema.shape.preco }}
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
                                          type="number"
                                          value={field.state.value}
                                          onChange={(e) =>
                                            field.handleChange(
                                              e.target.value === "" ? 0 : Number(e.target.value),
                                            )
                                          }
                                          placeholder="0,00"
                                          className={cn(
                                            "h-8 text-xs",
                                            error &&
                                              "border-destructive focus-visible:ring-destructive",
                                          )}
                                        />
                                        {error && (
                                          <span className="text-[10px] font-medium text-destructive">
                                            {error}
                                          </span>
                                        )}
                                      </div>
                                    );
                                  }}
                                </form.Field>
                              </td>
                              <td className="p-3">
                                <form.Field
                                  name={`skus[${index}].estoque`}
                                  validators={{ onChange: skuFormSchema.shape.estoque }}
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
                                          type="number"
                                          value={field.state.value}
                                          onChange={(e) =>
                                            field.handleChange(
                                              e.target.value === "" ? 0 : Number(e.target.value),
                                            )
                                          }
                                          placeholder="0"
                                          className={cn(
                                            "h-8 text-xs",
                                            error &&
                                              "border-destructive focus-visible:ring-destructive",
                                          )}
                                          disabled={!!editingItem}
                                        />
                                        {error && (
                                          <span className="text-[10px] font-medium text-destructive">
                                            {error}
                                          </span>
                                        )}
                                      </div>
                                    );
                                  }}
                                </form.Field>
                              </td>
                              <td className="p-3">
                                <form.Field
                                  name={`skus[${index}].gtinEan`}
                                  validators={{ onChange: skuFormSchema.shape.gtinEan }}
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
                                          maxLength={14}
                                          placeholder="EAN"
                                          className={cn(
                                            "h-8 text-xs",
                                            error &&
                                              "border-destructive focus-visible:ring-destructive",
                                          )}
                                        />
                                        {error && (
                                          <span className="text-[10px] font-medium text-destructive">
                                            {error}
                                          </span>
                                        )}
                                      </div>
                                    );
                                  }}
                                </form.Field>
                              </td>
                              <td className="p-3 text-center">
                                <form.Field name={`skus[${index}].ativo`}>
                                  {(field) => (
                                    <Checkbox
                                      checked={field.state.value}
                                      onCheckedChange={(checked) =>
                                        field.handleChange(!!checked)
                                      }
                                    />
                                  )}
                                </form.Field>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    <form.Field name="skus">
                      {(field) => {
                        const error = getFieldError(
                          field.name,
                          field.state.meta.errors,
                        );
                        return error ? (
                          <p className="text-xs font-medium text-destructive mt-2">
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
    </UpsertDialog>
  );
}
