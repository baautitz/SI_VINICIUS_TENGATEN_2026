"use client";

import React, { useState, useEffect } from "react";
import { useForm } from "@tanstack/react-form";
import { useSelector } from "@tanstack/react-store";
import { useHotkeys } from "@tanstack/react-hotkeys";
import { Button } from "@/components/ui/button";
import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { FieldLabel, FieldError } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { NumberInput } from "@/components/ui/number-input";
import { DatePicker } from "@/components/ui/date-picker";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from "@/components/ui/table";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Empty, EmptyHeader, EmptyTitle, EmptyDescription } from "@/components/ui/empty";
import { FornecedorInput } from "@/components/entity-inputs/fornecedor-input";
import { CondicaoPagamentoInput } from "@/components/entity-inputs/condicao-pagamento-input";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { contasPagarApi } from "@/api/financeiro";
import { Fornecedor } from "@/features/parceiros/fornecedores/types";
import { CondicaoPagamento } from "@/features/pagamentos/condicoes/types";
import {
  contasPagarSchema,
  contasPagarBaseSchema,
  ContasPagar,
  ContasPagarParcela,
  ContasPagarFormValues,
} from "./types";
import { Plus, Trash2 } from "lucide-react";
import { cn } from "@/lib/utils";

interface ContasPagarUpsertProps {
  open: boolean;
  editingItem: ContasPagar | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function ContasPagarUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly = false,
}: ContasPagarUpsertProps) {
  const isEditMode = !!editingItem;
  const [parcelas, setParcelas] = useState<ContasPagarParcela[]>(
    editingItem?.contasPagarParcelas?.map((p) => ({
      numeroParcela: p.numeroParcela,
      dataVencimento: p.dataVencimento ? p.dataVencimento.split("T")[0] : "",
      valorParcela: p.valorParcela,
      valorPago: p.valorPago,
      status: p.status,
    })) ?? []
  );

  const [fornecedor, setFornecedor] = useState<Fornecedor | null>(
    editingItem?.fornecedor ?? null
  );
  const [condicao, setCondicao] = useState<CondicaoPagamento | null>(
    editingItem?.condicaoPagamento ?? null
  );

  const {
    mutation,
    globalError,
    getFieldError: originalGetFieldError,
    resetErrors,
  } = useUpsertMutation({
    mutationFn: async (value: ContasPagarFormValues) => {
      const payload = {
        ...value,
        parcelas: value.parcelas.map((p) => ({
          ...p,
          dataVencimento: new Date(p.dataVencimento + "T12:00:00").toISOString(),
        })),
      };
      return isEditMode
        ? await contasPagarApi.update(editingItem.id, payload)
        : await contasPagarApi.create(payload);
    },
    queryKey: ["contas-pagar"],
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
      fornecedorId: editingItem?.fornecedor?.id ?? 0,
      nfeId: editingItem?.nfeId ?? null,
      dataEmissao: editingItem?.dataEmissao ? editingItem.dataEmissao.split("T")[0] : new Date().toISOString().split("T")[0],
      dataVencimento: editingItem?.dataVencimento ? editingItem.dataVencimento.split("T")[0] : null,
      valorOriginal: editingItem?.valorOriginal ?? 0,
      condicaoPagamentoId: editingItem?.condicaoPagamento?.id ?? null,
      observacao: editingItem?.observacao ?? "",
      parcelas: parcelas,
    } as ContasPagarFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      setLocalErrors({});
      const payload = {
        ...value,
        parcelas,
      };

      const validationResult = contasPagarSchema.safeParse(payload);
      if (!validationResult.success) {
        const errorsMap: Record<string, string> = {};
        validationResult.error.errors.forEach((err) => {
          const path = err.path.join(".");
          errorsMap[path] = err.message;
        });
        setLocalErrors(errorsMap);
        return;
      }

      mutation.mutate(payload as ContasPagarFormValues);
    },
  });

  useEffect(() => {
    form.setFieldValue("parcelas", parcelas);
  }, [parcelas, form]);

  const gerarSugeridas = (
    valor: number,
    cond: CondicaoPagamento | null,
    emissaoStr?: string | null
  ) => {
    if (!cond || valor <= 0) return;
    const baseDate = emissaoStr ? new Date(emissaoStr + "T12:00:00") : new Date();
    const sugeridas: ContasPagarParcela[] = [];
    let count = 1;

    const entradaPercent = cond.entradaMinimaPercentual ?? 0;
    if (entradaPercent > 0) {
      const valEntrada = parseFloat((valor * (entradaPercent / 100)).toFixed(2));
      sugeridas.push({
        numeroParcela: count++,
        dataVencimento: baseDate.toISOString().split("T")[0],
        valorParcela: valEntrada,
        valorPago: 0,
        status: "ABERTO",
      });
    }

    const items = cond.condicoesPagamentosParcelas || [];
    items.forEach((it) => {
      const venc = new Date(baseDate);
      venc.setDate(baseDate.getDate() + it.prazoDias);
      const valParcela = parseFloat((valor * (it.percentual / 100)).toFixed(2));
      sugeridas.push({
        numeroParcela: count++,
        dataVencimento: venc.toISOString().split("T")[0],
        valorParcela: valParcela,
        valorPago: 0,
        status: "ABERTO",
      });
    });

    if (sugeridas.length > 0) {
      const totalSugerido = sugeridas.reduce((sum, p) => sum + p.valorParcela, 0);
      const diff = valor - totalSugerido;
      if (Math.abs(diff) > 0.001) {
        sugeridas[sugeridas.length - 1].valorParcela = parseFloat(
          (sugeridas[sugeridas.length - 1].valorParcela + diff).toFixed(2)
        );
      }
    }

    setParcelas(sugeridas);
  };

  const handleAddParcela = () => {
    if (readOnly) return;
    const nextNum = parcelas.length + 1;
    const valorOriginal = form.getFieldValue("valorOriginal") || 0;
    const totalAtual = parcelas.reduce((sum, p) => sum + p.valorParcela, 0);
    const rem = Math.max(0, valorOriginal - totalAtual);

    const baseDate = form.getFieldValue("dataEmissao")
      ? new Date(form.getFieldValue("dataEmissao") + "T12:00:00")
      : new Date();
    baseDate.setDate(baseDate.getDate() + (parcelas.length * 30 + 30));

    setParcelas([
      ...parcelas,
      {
        numeroParcela: nextNum,
        dataVencimento: baseDate.toISOString().split("T")[0],
        valorParcela: parseFloat(rem.toFixed(2)),
        valorPago: 0,
        status: "ABERTO",
      },
    ]);
  };

  const handleRemoveParcela = (index: number) => {
    if (readOnly) return;
    const filtered = parcelas.filter((_, i) => i !== index);
    const reindexed = filtered.map((p, idx) => ({
      ...p,
      numeroParcela: idx + 1,
    }));
    setParcelas(reindexed);
  };

  const handleUpdateParcela = (
    index: number,
    key: keyof ContasPagarParcela,
    value: string | number
  ) => {
    if (readOnly) return;
    const updated = [...parcelas];
    updated[index] = {
      ...updated[index],
      [key]: value,
    } as ContasPagarParcela;
    setParcelas(updated);
  };

  const totalParcelasSum = parcelas.reduce((sum, p) => sum + p.valorParcela, 0);
  const valorOriginalForm = useSelector(form.store, (s) => s.values.valorOriginal);

  useHotkeys(
    [
      {
        hotkey: "Alt+P",
        callback: (e) => {
          e.preventDefault();
          handleAddParcela();
        },
        options: {
          enabled: open && !readOnly,
          ignoreInputs: false,
        },
      },
    ],
    { conflictBehavior: "allow" }
  );

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={isEditMode ? "Detalhes da Conta a Pagar" : "Nova Conta a Pagar"}
      footer={
        <>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancelar <Kbd>Esc</Kbd>
            </Button>
          </DialogClose>
          {!readOnly && (
            <Button type="submit" form="upsert-contas-pagar" disabled={mutation.isPending}>
              {mutation.isPending ? (
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
        </>
      }
    >
      <form
        id="upsert-contas-pagar"
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
              <h3 className="text-sm font-semibold tracking-wider">Informações Gerais</h3>
              <Separator />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <form.Field
                name="descricao"
                validators={{ onChange: contasPagarBaseSchema.shape.descricao }}
              >
                {(field) => (
                  <div className="flex flex-col gap-1.5 col-span-1 md:col-span-2">
                    <FieldLabel>Descrição</FieldLabel>
                    <Input
                      name={field.name}
                      value={field.state.value}
                      onChange={(e) => field.handleChange(e.target.value)}
                      disabled={readOnly}
                      inputSize="full"
                      maxLength={150}
                      className={cn(
                        getFieldError(field.name, field.state.meta.errors) &&
                          "border-destructive focus-visible:ring-destructive"
                      )}
                    />
                    {getFieldError(field.name, field.state.meta.errors) && (
                      <FieldError>{getFieldError(field.name, field.state.meta.errors)}</FieldError>
                    )}
                  </div>
                )}
              </form.Field>

              <form.Field
                name="fornecedorId"
                validators={{ onChange: contasPagarBaseSchema.shape.fornecedorId }}
              >
                {(field) => {
                  const error = getFieldError(field.name, field.state.meta.errors);
                  return (
                    <FornecedorInput
                      name={field.name}
                      error={error}
                      initialItem={fornecedor}
                      onSelectId={(id) => field.handleChange(id ?? 0)}
                      onSelectItem={setFornecedor}
                      disabled={readOnly}
                    />
                  );
                }}
              </form.Field>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <form.Field
                name="valorOriginal"
                validators={{ onChange: contasPagarBaseSchema.shape.valorOriginal }}
              >
                {(field) => (
                  <div className="flex flex-col gap-1.5">
                    <FieldLabel>Valor Original (R$)</FieldLabel>
                    <NumberInput
                      inputSize="full"
                      value={field.state.value}
                      decimals={2}
                      inputMode="decimal"
                      onNumberChange={(num) => {
                        field.handleChange(num);
                        gerarSugeridas(num, condicao, form.getFieldValue("dataEmissao") as string | null);
                      }}
                      disabled={readOnly}
                      className={cn(
                        "font-semibold text-right",
                        getFieldError(field.name, field.state.meta.errors) &&
                          "border-destructive focus-visible:ring-destructive"
                      )}
                    />
                    {getFieldError(field.name, field.state.meta.errors) && (
                      <FieldError>{getFieldError(field.name, field.state.meta.errors)}</FieldError>
                    )}
                  </div>
                )}
              </form.Field>

              <form.Field name="condicaoPagamentoId">
                {(field) => {
                  const error = getFieldError(field.name, field.state.meta.errors);
                  return (
                    <div className="col-span-1 md:col-span-2">
                      <CondicaoPagamentoInput
                        name={field.name}
                        error={error}
                        initialItem={condicao}
                        onSelectId={(id) => field.handleChange(id)}
                        onSelectItem={(cond) => {
                          setCondicao(cond);
                          gerarSugeridas(form.getFieldValue("valorOriginal") as number, cond, form.getFieldValue("dataEmissao") as string | null);
                        }}
                        disabled={readOnly}
                      />
                    </div>
                  );
                }}
              </form.Field>

              <form.Field name="nfeId">
                {(field) => (
                  <div className="flex flex-col gap-1.5">
                    <FieldLabel>ID Nota Fiscal</FieldLabel>
                    <NumberInput
                      inputSize="full"
                      value={field.state.value ?? 0}
                      decimals={0}
                      inputMode="numeric"
                      onNumberChange={(num) => field.handleChange(num || null)}
                      disabled={readOnly}
                      className="text-right"
                    />
                  </div>
                )}
              </form.Field>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <form.Field name="dataEmissao">
                {(field) => (
                  <div className="flex flex-col gap-1.5">
                    <FieldLabel>Data Emissão</FieldLabel>
                    <DatePicker
                      value={field.state.value}
                      onChange={(val) => {
                        field.handleChange(val);
                        gerarSugeridas(form.getFieldValue("valorOriginal") as number, condicao, val);
                      }}
                      disabled={readOnly}
                    />
                  </div>
                )}
              </form.Field>

              <form.Field name="dataVencimento">
                {(field) => (
                  <div className="flex flex-col gap-1.5">
                    <FieldLabel>Data Vencimento Geral (Opcional)</FieldLabel>
                    <DatePicker
                      value={field.state.value}
                      onChange={(val) => field.handleChange(val)}
                      disabled={readOnly}
                    />
                  </div>
                )}
              </form.Field>
            </div>

            <form.Field name="observacao">
              {(field) => (
                <div className="flex flex-col gap-1.5">
                  <FieldLabel>Observações</FieldLabel>
                  <Textarea
                    value={field.state.value ?? ""}
                    onChange={(e) => field.handleChange(e.target.value)}
                    disabled={readOnly}
                    maxLength={500}
                    className="h-20"
                  />
                </div>
              )}
            </form.Field>
          </div>

          <div className="flex flex-col gap-4">
            <div className="flex justify-between items-center">
              <h3 className="text-sm font-semibold tracking-wider">Parcelas de Pagamento</h3>
              {!readOnly && (
                <Button type="button" variant="outline" onClick={handleAddParcela}>
                  <Plus className="mr-1 h-4 w-4" /> Adicionar Parcela{" "}
                  <KbdGroup>
                    <Kbd>Alt</Kbd>
                    <Kbd>P</Kbd>
                  </KbdGroup>
                </Button>
              )}
            </div>
            <Separator />

            <form.Field name="parcelas">
              {(field) => {
                const error = getFieldError(field.name, field.state.meta.errors);
                return (
                  <div className="flex flex-col w-full">
                    <ScrollArea className="max-h-60 flex-1 pr-2">
                      {parcelas.length === 0 ? (
                        <Empty>
                          <EmptyHeader>
                            <EmptyTitle>Nenhuma parcela definida</EmptyTitle>
                            <EmptyDescription>
                              Adicione uma parcela manualmente ou selecione uma condição de pagamento para calculá-las.
                            </EmptyDescription>
                          </EmptyHeader>
                        </Empty>
                      ) : (
                        <Table>
                          <TableHeader>
                            <TableRow className="hover:bg-transparent">
                              <TableHead className="w-16 px-2 text-left">Parcela</TableHead>
                              <TableHead className="px-2 text-left">Vencimento</TableHead>
                              <TableHead className="px-2 text-right">Valor Parcela (R$)</TableHead>
                              <TableHead className="px-2 text-right">Valor Pago (R$)</TableHead>
                              <TableHead className="px-2 text-center">Status</TableHead>
                              {!readOnly && <TableHead className="w-12 px-2 text-right" />}
                            </TableRow>
                          </TableHeader>
                          <TableBody>
                            {parcelas.map((p, index) => (
                              <TableRow key={index} className="hover:bg-transparent">
                                <TableCell className="px-2 py-2 font-semibold align-middle">
                                  #{p.numeroParcela}
                                </TableCell>
                                <TableCell className="px-2 py-2 align-middle">
                                  <DatePicker
                                    value={p.dataVencimento}
                                    onChange={(val) =>
                                      handleUpdateParcela(index, "dataVencimento", val ?? "")
                                    }
                                    disabled={readOnly || p.status === "PAGO"}
                                    className={cn(
                                      "h-8 text-xs",
                                      localErrors[`parcelas.${index}.dataVencimento`] &&
                                        "border-destructive focus-visible:ring-destructive"
                                    )}
                                  />
                                </TableCell>
                                <TableCell className="px-2 py-2 align-middle">
                                  <NumberInput
                                    inputSize="full"
                                    value={p.valorParcela}
                                    decimals={2}
                                    inputMode="decimal"
                                    onNumberChange={(num) =>
                                      handleUpdateParcela(index, "valorParcela", num)
                                    }
                                    disabled={readOnly || p.status === "PAGO"}
                                    className={cn(
                                      "h-8 text-right font-semibold",
                                      localErrors[`parcelas.${index}.valorParcela`] &&
                                        "border-destructive focus-visible:ring-destructive"
                                    )}
                                  />
                                </TableCell>
                                <TableCell className="px-2 py-2 text-right align-middle font-medium text-muted-foreground">
                                  {new Intl.NumberFormat("pt-BR", {
                                    style: "currency",
                                    currency: "BRL",
                                  }).format(p.valorPago || 0)}
                                </TableCell>
                                <TableCell className="px-2 py-2 text-center align-middle">
                                  <span className="text-xs font-semibold uppercase">{p.status}</span>
                                </TableCell>
                                {!readOnly && (
                                  <TableCell className="px-2 py-2 text-right align-middle">
                                    <Button
                                      type="button"
                                      size="icon"
                                      variant="ghost"
                                      className="text-destructive hover:bg-destructive/10 h-8 w-8"
                                      onClick={() => handleRemoveParcela(index)}
                                      disabled={p.status === "PAGO"}
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

                    {error && <FieldError className="mt-2">{error}</FieldError>}
                  </div>
                );
              }}
            </form.Field>

            <Card className="bg-muted/50 mt-2">
              <CardContent className="flex flex-col gap-2 py-3">
                <div className="flex justify-between items-center text-sm">
                  <span>Valor Original da Conta:</span>
                  <span className="font-semibold text-foreground">
                    {new Intl.NumberFormat("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    }).format(valorOriginalForm)}
                  </span>
                </div>
                <div className="flex justify-between items-center text-sm">
                  <span>Soma das Parcelas:</span>
                  <span
                    className={cn(
                      "font-semibold",
                      Math.abs(totalParcelasSum - valorOriginalForm) > 0.01
                        ? "text-amber-600"
                        : "text-emerald-600"
                    )}
                  >
                    {new Intl.NumberFormat("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    }).format(totalParcelasSum)}
                  </span>
                </div>
                {Math.abs(totalParcelasSum - valorOriginalForm) > 0.01 && (
                  <Alert variant="destructive" className="mt-1 py-2">
                    <AlertDescription className="text-xs text-right">
                      Diferença: R$ {(valorOriginalForm - totalParcelasSum).toFixed(2)}
                    </AlertDescription>
                  </Alert>
                )}
              </CardContent>
            </Card>
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
