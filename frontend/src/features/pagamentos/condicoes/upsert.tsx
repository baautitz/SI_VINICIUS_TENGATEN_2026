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
import { MetodoPagamentoInput } from "@/components/entity-inputs/metodo-pagamento-input";
import { useForm } from "@tanstack/react-form";
import { useSelector } from "@tanstack/react-store";
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from "@/components/ui/table";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { Card, CardContent } from "@/components/ui/card";
import {
  Empty,
  EmptyHeader,
  EmptyTitle,
  EmptyDescription,
} from "@/components/ui/empty";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import {
  condicaoPagamentoSchema,
  condicaoPagamentoBaseSchema,
  CondicaoPagamento,
  CondicaoPagamentoParcela,
  CondicaoPagamentoFormValues,
} from "./types";
import { useQuery } from "@tanstack/react-query";
import { condicoesApi } from "@/api/pagamentos";
import { Plus, Trash2 } from "lucide-react";
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
      metodoPagamentoCodigo: editingItem?.metodoPagamento?.codigo ?? "",
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
        parcelas: value.entradaMinimaPercentual === 100 ? [] : parcelas,
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

  const entradaMinimaPercentual = useSelector(
    form.store,
    (state) => state.values.entradaMinimaPercentual,
  );

  const totalParcelasPercent = parcelas.reduce(
    (sum, p) => sum + p.percentual,
    0,
  );

  const handleAddParcela = () => {
    if (readOnly) return;
    const currentEntrada = entradaMinimaPercentual ?? 0;
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

  const handleCalcularPorcentagens = () => {
    if (readOnly || parcelas.length === 0) return;
    const remaining = 100 - (entradaMinimaPercentual ?? 0);
    if (remaining <= 0) return;

    const basePercent = parseFloat((remaining / parcelas.length).toFixed(2));
    const distributed = parcelas.map((p) => ({
      ...p,
      percentual: basePercent,
    }));

    const sumOfDistributed = basePercent * parcelas.length;
    const diff = remaining - sumOfDistributed;
    if (Math.abs(diff) > 0.001) {
      distributed[distributed.length - 1].percentual = parseFloat(
        (distributed[distributed.length - 1].percentual + diff).toFixed(2),
      );
    }

    setParcelas(distributed);
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
          enabled: open && !readOnly && entradaMinimaPercentual !== 100,
          ignoreInputs: false,
        },
      },
      {
        hotkey: "Alt+C",
        callback: (e: KeyboardEvent) => {
          e.preventDefault();
          handleCalcularPorcentagens();
        },
        options: {
          enabled: open && !readOnly && parcelas.length > 0 && entradaMinimaPercentual !== 100,
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

  const handleParcelaKeyDown = (
    e: React.KeyboardEvent<HTMLInputElement>,
    index: number,
    field: "percentual" | "prazo"
  ) => {
    if (e.key === "Enter") {
      e.preventDefault();
      const nextInput = document.getElementById(`parcela-${index + 1}-${field}`);
      if (nextInput) {
        nextInput.focus();
        (nextInput as HTMLInputElement).select?.();
      }
    } else if (e.key === "ArrowDown") {
      e.preventDefault();
      const nextInput = document.getElementById(`parcela-${index + 1}-${field}`);
      if (nextInput) {
        nextInput.focus();
        (nextInput as HTMLInputElement).select?.();
      }
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      const prevInput = document.getElementById(`parcela-${index - 1}-${field}`);
      if (prevInput) {
        prevInput.focus();
        (prevInput as HTMLInputElement).select?.();
      }
    }
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
            <div className="flex flex-col gap-2">
              <h3 className="text-sm font-semibold tracking-wider">
                Informações Gerais
              </h3>
              <Separator />
            </div>

            <div className="flex w-full flex-wrap items-start gap-4">
              {editingItem && (
                <div className="w-20 shrink-0">
                  <div className="flex flex-col gap-2">
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
                  name="metodoPagamentoCodigo"
                  validators={{
                    onChange:
                      condicaoPagamentoBaseSchema.shape.metodoPagamentoCodigo,
                  }}
                >
                  {(field) => {
                    const error = getFieldError(
                      field.name,
                      field.state.meta.errors,
                    );
                    return (
                      <MetodoPagamentoInput
                        name={field.name}
                        error={error}
                        initialItem={editingItem?.metodoPagamento ?? null}
                        onSelectCodigo={(codigo) =>
                          field.handleChange(codigo ?? "")
                        }
                        disabled={readOnly}
                      />
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
                        {(field) => {
                          const isChecked = field.state.value === 100;
                          return (
                            <div className="flex flex-col gap-2">
                              <FormFieldUI
                                field={field}
                                label="Entrada Mínima (%)"
                                type="number"
                                inputSize="full"
                                getFieldError={getFieldError}
                                disabled={readOnly || isChecked}
                                min={0}
                                max={100}
                              />
                              <div className="mt-1 flex items-center gap-2">
                                <Checkbox
                                  id="a-vista-checkbox"
                                  checked={isChecked}
                                  disabled={readOnly}
                                  onCheckedChange={(checked) => {
                                    field.handleChange(checked ? 100 : 0);
                                  }}
                                />
                                <FieldLabel
                                  htmlFor="a-vista-checkbox"
                                  className="cursor-pointer text-xs font-semibold select-none"
                                >
                                  À Vista
                                </FieldLabel>
                              </div>
                            </div>
                          );
                        }}
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

          {entradaMinimaPercentual === 100 ? null : (
            <div className="flex w-full flex-col gap-4">
              <Separator />
              <div className="flex flex-col gap-2">
                <div className="flex items-center justify-between">
                  <h3 className="text-sm font-semibold tracking-wider">
                    Parcelas
                  </h3>
                  {!readOnly && (
                    <div className="flex items-center gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        onClick={handleCalcularPorcentagens}
                        disabled={parcelas.length === 0}
                      >
                        Distribuir Porcentagens{" "}
                        <KbdGroup className="ml-1">
                          <Kbd>Alt</Kbd>
                          <Kbd>C</Kbd>
                        </KbdGroup>
                      </Button>
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
                    </div>
                  )}
                </div>
                <Separator />
              </div>

              <form.Field name="parcelas">
                {(field) => {
                  const error = getFieldError(
                    field.name,
                    field.state.meta.errors,
                  );
                  return (
                    <div className="flex min-h-0 w-full flex-1 flex-col">
                      <ScrollArea className="max-h-60 flex-1 pr-2">
                        {parcelas.length === 0 ? (
                          <Empty>
                            <EmptyHeader>
                              <EmptyTitle>
                                Nenhuma parcela adicionada
                              </EmptyTitle>
                              <EmptyDescription>
                                Clique no botão acima para adicionar uma
                                parcela.
                              </EmptyDescription>
                            </EmptyHeader>
                          </Empty>
                        ) : (
                          <Table>
                            <TableHeader>
                              <TableRow className="hover:bg-transparent">
                                <TableHead className="h-9 w-20 px-2 text-left">
                                  Nº
                                </TableHead>
                                <TableHead className="h-9 px-2 text-right">
                                  Percentual (%)
                                </TableHead>
                                <TableHead className="h-9 px-2 text-right">
                                  Prazo (Dias)
                                </TableHead>
                                {!readOnly && (
                                  <TableHead className="h-9 w-12 px-2 text-right" />
                                )}
                              </TableRow>
                            </TableHeader>
                            <TableBody>
                              {parcelas.map((p, index) => (
                                <TableRow
                                  key={index}
                                  className="hover:bg-transparent"
                                >
                                  <TableCell className="px-2 py-2 font-semibold">
                                    #{p.numeroParcela}
                                  </TableCell>
                                  <TableCell className="px-2 py-2">
                                    <div className="flex w-full flex-col gap-1">
                                      <NumberInput
                                        id={`parcela-${index}-percentual`}
                                        name={`parcelas.${index}.percentual`}
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
                                        onKeyDown={(e) =>
                                          handleParcelaKeyDown(e, index, "percentual")
                                        }
                                        onFocus={(e) => e.target.select()}
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
                                  </TableCell>
                                  <TableCell className="px-2 py-2">
                                    <div className="flex w-full flex-col gap-1">
                                      <NumberInput
                                        id={`parcela-${index}-prazo`}
                                        name={`parcelas.${index}.prazo`}
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
                                        onKeyDown={(e) =>
                                          handleParcelaKeyDown(e, index, "prazo")
                                        }
                                        onFocus={(e) => e.target.select()}
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
                                  </TableCell>
                                  {!readOnly && (
                                    <TableCell className="px-2 py-2 text-right">
                                      <Button
                                        type="button"
                                        size="icon"
                                        variant="ghost"
                                        tabIndex={-1}
                                        className="text-destructive hover:bg-destructive/10 h-8 w-8"
                                        onClick={() =>
                                          handleRemoveParcela(index)
                                        }
                                      >
                                        <Trash2 className="h-4 w-4" />
                                      </Button>
                                    </TableCell>
                                  )}
                                </TableRow>
                              ))}
                            </TableBody>
                          </Table>
                        )}
                      </ScrollArea>
                      {error && (
                        <FieldError className="mt-2 block">{error}</FieldError>
                      )}
                    </div>
                  );
                }}
              </form.Field>

              <Card size="sm" className="bg-muted/50 mt-auto">
                <CardContent className="flex flex-col gap-2">
                  <div className="flex items-center justify-between">
                    <span>Entrada Mínima:</span>
                    <span className="font-semibold">
                      {(entradaMinimaPercentual ?? 0).toFixed(2)}%
                    </span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Soma das Parcelas:</span>
                    <span
                      className={`font-semibold ${
                        totalParcelasPercent > 100 ? "text-destructive" : ""
                      }`}
                    >
                      {totalParcelasPercent.toFixed(2)}%
                    </span>
                  </div>
                  <Separator />
                  <div className="flex items-center justify-between text-sm font-semibold">
                    <span>Total (Entrada + Parcelas):</span>
                    <span
                      className={
                        (entradaMinimaPercentual ?? 0) +
                          totalParcelasPercent ===
                        100
                          ? "text-emerald-600"
                          : (entradaMinimaPercentual ?? 0) +
                                totalParcelasPercent >
                              100
                            ? "text-destructive"
                            : "text-amber-600"
                      }
                    >
                      {(
                        (entradaMinimaPercentual ?? 0) + totalParcelasPercent
                      ).toFixed(2)}
                      % / 100.00%
                    </span>
                  </div>
                </CardContent>
              </Card>
            </div>
          )}
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
