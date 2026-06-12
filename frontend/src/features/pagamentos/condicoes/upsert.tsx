"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { useState, useEffect } from "react";
import { useHotkeys } from "@tanstack/react-hotkeys";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { NumberInput } from "@/components/ui/number-input";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import {
  condicaoPagamentoSchema,
  condicaoPagamentoBaseSchema,
  CondicaoPagamento,
  CondicaoPagamentoParcela,
  CondicaoPagamentoFormValues,
} from "./types";
import { useQuery } from "@tanstack/react-query";
import { condicoesApi, metodosApi } from "@/api/pagamentos";
import { Plus, Trash2, HelpCircle } from "lucide-react";
import { cn } from "@/lib/utils";

interface CondicoesUpsertProps {
  open: boolean;
  editingItem: CondicaoPagamento | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function CondicoesUpsert(props: CondicoesUpsertProps) {
  const { open, editingItem, onClose, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["condicoesPagamento", "detail", editingItem?.id],
    queryFn: () => condicoesApi.getById(editingItem!.id),
    enabled: isEditMode && open,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Condição de Pagamento"
        loading={true}
      />
    );
  }

  return (
    <CondicoesUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function CondicoesUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly = false,
}: CondicoesUpsertProps) {
  const [parcelas, setParcelas] = useState<CondicaoPagamentoParcela[]>(
    editingItem?.condicoesPagamentosParcelas?.map((p) => ({
      numeroParcela: p.numeroParcela,
      percentual: p.percentual,
      prazoDias: p.prazoDias,
    })) ?? [],
  );

  const { data: metodosData } = useQuery({
    queryKey: ["metodosPagamento", "active-list"],
    queryFn: async () => {
      const res = await metodosApi.list(undefined, 1, 1000);
      return res?.itens?.filter((m) => m.ativo) ?? [];
    },
    enabled: open,
  });

  const {
    mutation,
    globalError,
    getFieldError: originalGetFieldError,
    resetErrors,
  } = useUpsertMutation({
    mutationFn: async (value: CondicaoPagamentoFormValues) => {
      return editingItem
        ? await condicoesApi.update(editingItem.id, value)
        : await condicoesApi.create(value);
    },
    queryKey: ["condicoesPagamento"],
    onSuccessCallback: onSuccess,
    onClose: onClose,
  });

  const [localErrors, setLocalErrors] = useState<Record<string, string>>({});

  const getFieldError = (name: string, formErrors: unknown[]) => {
    return localErrors[name] || originalGetFieldError(name, formErrors);
  };

  const form = useForm({
    defaultValues: {
      descricao: editingItem?.descricao ?? "",
      metodoPagamentoId: editingItem?.metodoPagamento?.id ?? 0,
      entradaMinimaPercentual: editingItem?.entradaMinimaPercentual ?? 0,
      descontoPercentual: editingItem?.descontoPercentual ?? 0,
      acrescimoPercentual: editingItem?.acrescimoPercentual ?? 0,
      multaPercentual: editingItem?.multaPercentual ?? 0,
      taxaJurosPercentual: editingItem?.taxaJurosPercentual ?? 0,
      ativo: editingItem?.ativo ?? true,
      parcelas: parcelas,
    } as CondicaoPagamentoFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      setLocalErrors({});
      const payload = {
        ...value,
        parcelas: parcelas,
      };

      const validationResult = condicaoPagamentoSchema.safeParse(payload);
      if (!validationResult.success) {
        const errorsMap: Record<string, string> = {};
        validationResult.error.errors.forEach((err) => {
          const path = err.path.join(".");
          errorsMap[path] = err.message;
        });
        setLocalErrors(errorsMap);
        return;
      }

      mutation.mutate(payload as CondicaoPagamentoFormValues);
    },
  });

  useEffect(() => {
    form.setFieldValue("parcelas", parcelas);
  }, [parcelas, form]);

  const totalParcelasPercent = parcelas.reduce(
    (sum, p) => sum + p.percentual,
    0,
  );

  const handleAddParcela = () => {
    if (readOnly) return;
    const currentEntrada = form.state.values.entradaMinimaPercentual ?? 0;
    const currentTotalPercent = currentEntrada + totalParcelasPercent;
    const nextNum = parcelas.length + 1;
    const remaining = Math.max(0, 100 - currentTotalPercent);
    const defaultPercent = remaining > 0 ? remaining : 0;
    const defaultDays =
      parcelas.length > 0 ? parcelas[parcelas.length - 1].prazoDias + 30 : 30;

    setParcelas([
      ...parcelas,
      {
        numeroParcela: nextNum,
        percentual: defaultPercent,
        prazoDias: defaultDays,
      },
    ]);
  };

  useHotkeys(
    [
      {
        hotkey: "Alt+P",
        callback: (e: KeyboardEvent) => {
          e.preventDefault();
          handleAddParcela();
        },
        options: {
          enabled: open && !readOnly,
          ignoreInputs: false,
        },
      },
    ],
    { conflictBehavior: "allow" },
  );

  const handleRemoveParcela = (index: number) => {
    if (readOnly) return;
    const updated = parcelas.filter((_, i) => i !== index);
    const reindexed = updated.map((p, idx) => ({
      ...p,
      numeroParcela: idx + 1,
    }));

    setParcelas(reindexed);
  };

  const handleUpdateParcelaField = (
    index: number,
    fieldName: keyof CondicaoPagamentoParcela,
    value: number,
  ) => {
    if (readOnly) return;
    const updated = [...parcelas];
    updated[index] = {
      ...updated[index],
      [fieldName]: value,
    };
    setParcelas(updated);
  };

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={
        editingItem
          ? "Editar Condição de Pagamento"
          : "Nova Condição de Pagamento"
      }
      footer={
        <>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancelar <Kbd>Esc</Kbd>
            </Button>
          </DialogClose>
          {!readOnly && (
            <form.Subscribe
              selector={(state) => [state.canSubmit, state.isSubmitting]}
            >
              {([canSubmit, isSubmitting]) => (
                <Button
                  type="submit"
                  form="upsert-condicoes"
                  disabled={!canSubmit || isSubmitting}
                >
                  {isSubmitting ? (
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
        id="upsert-condicoes"
        className="flex flex-col gap-6"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <div className="flex w-full flex-col gap-6">
          <div className="flex flex-col gap-4">
            <h3 className="border-b pb-2 text-sm font-semibold tracking-wider">
              Informações Gerais
            </h3>

            <div className="flex w-full flex-wrap items-end gap-4">
              {editingItem && (
                <div className="w-20 shrink-0">
                  <div className="flex flex-col gap-1.5">
                    <FieldLabel>ID</FieldLabel>
                    <Input
                      value={editingItem.id}
                      disabled
                      className="h-9 text-xs"
                      inputSize="full"
                    />
                  </div>
                </div>
              )}

              <div className="min-w-48 flex-1">
                <form.Field
                  name="descricao"
                  validators={{
                    onChange: condicaoPagamentoBaseSchema.shape.descricao,
                  }}
                >
                  {(field) => (
                    <FormFieldUI
                      field={field}
                      label="Descrição da Condição"
                      inputSize="full"
                      getFieldError={getFieldError}
                      maxLength={150}
                      disabled={readOnly}
                    />
                  )}
                </form.Field>
              </div>

              <div className="w-72 min-w-48 flex-1 shrink-0 md:flex-initial">
                <form.Field
                  name="metodoPagamentoId"
                  validators={{
                    onChange:
                      condicaoPagamentoBaseSchema.shape.metodoPagamentoId,
                  }}
                >
                  {(field) => {
                    const error = getFieldError(
                      field.name,
                      field.state.meta.errors,
                    );
                    return (
                      <div className="flex w-full flex-col gap-1.5">
                        <FieldLabel htmlFor={field.name}>
                          Método de Pagamento
                        </FieldLabel>
                        <Select
                          value={
                            field.state.value
                              ? field.state.value.toString()
                              : ""
                          }
                          onValueChange={(val) =>
                            field.handleChange(Number(val))
                          }
                          disabled={readOnly}
                        >
                          <SelectTrigger id={field.name} className="h-9 w-full">
                            <SelectValue placeholder="Selecione um método..." />
                          </SelectTrigger>
                          <SelectContent>
                            {metodosData?.map((m) => (
                              <SelectItem key={m.id} value={m.id.toString()}>
                                {m.codigo} - {m.descricao}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        {error && (
                          <span className="text-destructive text-xs">
                            {error}
                          </span>
                        )}
                      </div>
                    );
                  }}
                </form.Field>
              </div>
            </div>

            <form.Subscribe
              selector={(state) => [
                state.values.descontoPercentual,
                state.values.acrescimoPercentual,
              ]}
            >
              {([desconto, acrescimo]) => {
                const hasDesconto = (desconto ?? 0) > 0;
                const hasAcrescimo = (acrescimo ?? 0) > 0;

                return (
                  <>
                    <div className="grid grid-cols-2 gap-4">
                      <form.Field
                        name="entradaMinimaPercentual"
                        validators={{
                          onChange:
                            condicaoPagamentoBaseSchema.shape
                              .entradaMinimaPercentual,
                        }}
                      >
                        {(field) => (
                          <FormFieldUI
                            field={field}
                            label="Entrada Mínima (%)"
                            type="number"
                            inputSize="full"
                            getFieldError={getFieldError}
                            disabled={readOnly}
                          />
                        )}
                      </form.Field>

                      <form.Field
                        name="descontoPercentual"
                        validators={{
                          onChange:
                            condicaoPagamentoBaseSchema.shape
                              .descontoPercentual,
                        }}
                      >
                        {(field) => (
                          <FormFieldUI
                            field={field}
                            label="Desconto (%)"
                            type="number"
                            inputSize="full"
                            getFieldError={getFieldError}
                            disabled={readOnly || hasAcrescimo}
                          />
                        )}
                      </form.Field>
                    </div>

                    <div className="grid grid-cols-3 gap-4">
                      <form.Field
                        name="acrescimoPercentual"
                        validators={{
                          onChange:
                            condicaoPagamentoBaseSchema.shape
                              .acrescimoPercentual,
                        }}
                      >
                        {(field) => (
                          <FormFieldUI
                            field={field}
                            label="Acréscimo (%)"
                            type="number"
                            inputSize="full"
                            getFieldError={getFieldError}
                            disabled={readOnly || hasDesconto}
                          />
                        )}
                      </form.Field>

                      <form.Field
                        name="multaPercentual"
                        validators={{
                          onChange:
                            condicaoPagamentoBaseSchema.shape.multaPercentual,
                        }}
                      >
                        {(field) => (
                          <FormFieldUI
                            field={field}
                            label="Multa (%)"
                            type="number"
                            inputSize="full"
                            getFieldError={getFieldError}
                            disabled={readOnly}
                          />
                        )}
                      </form.Field>

                      <form.Field
                        name="taxaJurosPercentual"
                        validators={{
                          onChange:
                            condicaoPagamentoBaseSchema.shape
                              .taxaJurosPercentual,
                        }}
                      >
                        {(field) => (
                          <FormFieldUI
                            field={field}
                            label="Juros (%)"
                            type="number"
                            inputSize="full"
                            getFieldError={getFieldError}
                            disabled={readOnly}
                          />
                        )}
                      </form.Field>
                    </div>
                  </>
                );
              }}
            </form.Subscribe>

            <form.Field name="ativo">
              {(field) => {
                const error = getFieldError(
                  field.name,
                  field.state.meta.errors,
                );
                return (
                  <Field
                    orientation="horizontal"
                    data-invalid={!!error}
                    className="mt-2"
                  >
                    <Checkbox
                      id={field.name}
                      name={field.name}
                      checked={field.state.value}
                      onCheckedChange={(checked) =>
                        field.handleChange(!!checked)
                      }
                      disabled={readOnly}
                    />
                    <FieldLabel htmlFor={field.name}>Ativo</FieldLabel>
                  </Field>
                );
              }}
            </form.Field>
          </div>

          <div className="border-muted-foreground/10 flex w-full flex-col gap-4 border-t pt-6">
            <div className="flex items-center justify-between border-b pb-2">
              <h3 className="text-sm font-semibold tracking-wider">Parcelas</h3>
              {!readOnly && (
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleAddParcela}
                >
                  <Plus className="mr-1 h-4 w-4" /> Adicionar Parcela{" "}
                  <KbdGroup>
                    <Kbd>Alt</Kbd>
                    <Kbd>P</Kbd>
                  </KbdGroup>
                </Button>
              )}
            </div>

            <form.Field name="parcelas">
              {(field) => {
                const error = getFieldError(
                  field.name,
                  field.state.meta.errors,
                );
                return (
                  <div className="flex min-h-0 w-full flex-1 flex-col">
                    <div className="max-h-62.5 flex-1 overflow-y-auto pr-2">
                      {parcelas.length === 0 ? (
                        <div className="flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-8 text-center">
                          <HelpCircle className="mb-2 h-8 w-8 opacity-50" />
                          <span className="text-sm font-medium">
                            Nenhuma parcela cadastrada
                          </span>
                          <span className="text-xs">
                            Clique no botão acima para adicionar uma parcela.
                          </span>
                        </div>
                      ) : (
                        <table className="w-full text-sm">
                          <thead>
                            <tr className="border-b text-xs font-medium">
                              <th className="w-20 py-2 text-left">Nº</th>
                              <th className="py-2 pr-4 text-right">
                                Percentual (%)
                              </th>
                              <th className="py-2 pr-4 text-right">
                                Prazo (Dias)
                              </th>
                              {!readOnly && (
                                <th className="w-12 py-2 text-right"></th>
                              )}
                            </tr>
                          </thead>
                          <tbody>
                            {parcelas.map((p, index) => (
                              <tr
                                key={index}
                                className="border-b align-middle last:border-0"
                              >
                                <td className="py-2 font-semibold">
                                  #{p.numeroParcela}
                                </td>
                                <td className="py-2 pr-2">
                                  <div className="flex w-full flex-col gap-1">
                                    <NumberInput
                                      inputSize="full"
                                      value={p.percentual}
                                      decimals={2}
                                      inputMode="decimal"
                                      onNumberChange={(num) =>
                                        handleUpdateParcelaField(
                                          index,
                                          "percentual",
                                          num,
                                        )
                                      }
                                      className={cn(
                                        "h-8 text-right font-semibold",
                                        localErrors[
                                          `parcelas.${index}.percentual`
                                        ] &&
                                          "border-destructive focus-visible:ring-destructive",
                                      )}
                                      aria-invalid={
                                        !!localErrors[
                                          `parcelas.${index}.percentual`
                                        ]
                                      }
                                      disabled={readOnly}
                                    />
                                    {localErrors[
                                      `parcelas.${index}.percentual`
                                    ] && (
                                      <span className="text-destructive mt-1 block text-right text-xs">
                                        {
                                          localErrors[
                                            `parcelas.${index}.percentual`
                                          ]
                                        }
                                      </span>
                                    )}
                                  </div>
                                </td>
                                <td className="py-2 pr-2">
                                  <div className="flex w-full flex-col gap-1">
                                    <NumberInput
                                      inputSize="full"
                                      value={p.prazoDias}
                                      decimals={0}
                                      inputMode="numeric"
                                      onNumberChange={(num) =>
                                        handleUpdateParcelaField(
                                          index,
                                          "prazoDias",
                                          num,
                                        )
                                      }
                                      className={cn(
                                        "h-8 text-right font-semibold",
                                        localErrors[
                                          `parcelas.${index}.prazoDias`
                                        ] &&
                                          "border-destructive focus-visible:ring-destructive",
                                      )}
                                      aria-invalid={
                                        !!localErrors[
                                          `parcelas.${index}.prazoDias`
                                        ]
                                      }
                                      disabled={readOnly}
                                    />
                                    {localErrors[
                                      `parcelas.${index}.prazoDias`
                                    ] && (
                                      <span className="text-destructive mt-1 block text-right text-xs">
                                        {
                                          localErrors[
                                            `parcelas.${index}.prazoDias`
                                          ]
                                        }
                                      </span>
                                    )}
                                  </div>
                                </td>
                                {!readOnly && (
                                  <td className="py-2 text-right">
                                    <Button
                                      type="button"
                                      size="icon"
                                      variant="ghost"
                                      className="text-destructive hover:bg-destructive/10 h-8 w-8"
                                      onClick={() => handleRemoveParcela(index)}
                                    >
                                      <Trash2 className="h-4 w-4" />
                                    </Button>
                                  </td>
                                )}
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      )}
                    </div>
                    {error && (
                      <FieldError className="mt-2 block">{error}</FieldError>
                    )}
                  </div>
                );
              }}
            </form.Field>

            <form.Subscribe
              selector={(state) => state.values.entradaMinimaPercentual}
            >
              {(entradaVal) => {
                const entrada = entradaVal ?? 0;
                const totalPercent = entrada + totalParcelasPercent;
                return (
                  <div className="bg-muted/50 mt-auto flex flex-col gap-2 rounded-lg border p-4">
                    <div className="flex items-center justify-between">
                      <span>Entrada Mínima:</span>
                      <span className="font-semibold">
                        {entrada.toFixed(2)}%
                      </span>
                    </div>
                    <div className="flex items-center justify-between">
                      <span>Soma das Parcelas:</span>
                      <span
                        className={`font-semibold ${totalParcelasPercent > 100 ? "text-destructive" : ""}`}
                      >
                        {totalParcelasPercent.toFixed(2)}%
                      </span>
                    </div>
                    <div className="mt-1 flex items-center justify-between border-t pt-2 text-sm font-semibold">
                      <span>Total (Entrada + Parcelas):</span>
                      <span
                        className={
                          totalPercent === 100
                            ? "text-emerald-600"
                            : totalPercent > 100
                              ? "text-destructive"
                              : "text-amber-600"
                        }
                      >
                        {totalPercent.toFixed(2)}% / 100.00%
                      </span>
                    </div>
                  </div>
                );
              }}
            </form.Subscribe>
          </div>
        </div>

        {globalError && (
          <Alert variant="destructive" className="mt-4">
            <AlertDescription>{globalError}</AlertDescription>
          </Alert>
        )}
      </form>
    </UpsertDialog>
  );
}
