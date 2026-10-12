"use client";
import { useOrigemAction } from "@/hooks/use-origem-action";
import { RelatedActions } from "@/components/related-actions";
import { FileText } from "lucide-react";

import React, { useState, useCallback } from "react";
import { WindowActions, WindowCancelButton } from "@/ui/imperative";
import { useForm } from "@tanstack/react-form";
import { useSelector } from "@tanstack/react-store";
import { Button } from "@/ui/primitives";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { FieldLabel, FieldError } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { NumberInput } from "@/ui/composites";
import { DatePicker } from "@/ui/composites";
import { TextoWindow, type TextoWindowProps } from "@/components/texto-window";
import { Separator } from "@/ui/primitives";
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from "@/ui/primitives";
import { ScrollArea } from "@/ui/primitives";
import {
  Empty,
  EmptyHeader,
  EmptyTitle,
  EmptyDescription,
} from "@/ui/primitives";
import { ClienteInput } from "@/components/entity-inputs/cliente-input";
import { CondicaoPagamentoInput } from "@/components/entity-inputs/condicao-pagamento-input";
import {
  useUpsertMutation,
  type BackendResult,
} from "@/hooks/use-upsert-mutation";
import type { UseMutationResult } from "@tanstack/react-query";
import { contasReceberApi } from "@/api/financeiro";
import { Cliente } from "@/features/parceiros/clientes/types";
import { CondicaoPagamento } from "@/features/financeiro/condicoes/types";
import {
  contasReceberSchema,
  contasReceberBaseSchema,
  ContasReceber,
  ContasReceberParcela,
  ContasReceberFormValues,
} from "./types";
import { Plus, Trash2, Check, RotateCcw } from "lucide-react";
import { cn } from "@/lib/utils";
import { useUi, useWindow, useWindowCommands } from "@/ui/imperative";
import { navigationCell } from "@/ui/keyboard-navigation";
import { toLocalISODate, todayLocalISODate } from "@/utils/date-utils";

export interface ContasReceberUpsertProps {
  editingItem: ContasReceber | null;
  readOnly?: boolean;
  onBaixa?: (
    contaId: number,
    parcela: ContasReceberParcela,
  ) => void | Promise<void>;
  onEstorno?: (
    contaId: number,
    parcela: ContasReceberParcela,
  ) => void | Promise<void>;
}

export function ContasReceberUpsertForm({
  editingItem: initialItem,
  readOnly = false,
  onBaixa: onBaixaProp,
  onEstorno: onEstornoProp,
}: ContasReceberUpsertProps) {
  const activeWindow = useWindow<true>();
  const [editingItem, setEditingItem] = useState(initialItem);
  const [version, setVersion] = useState(0);

  // Recarrega a conta apos baixa/estorno para nao reenviar parcelas antigas no PUT.
  const withRefresh =
    (
      fn?: (
        contaId: number,
        parcela: ContasReceberParcela,
      ) => void | Promise<void>,
    ) =>
    async (contaId: number, parcela: ContasReceberParcela) => {
      await fn?.(contaId, parcela);
      try {
        setEditingItem(await contasReceberApi.getById(contaId));
        setVersion((v) => v + 1);
      } catch {
        // mantem dados atuais; o PUT usa o ultimo estado carregado
      }
    };
  const onBaixa = onBaixaProp ? withRefresh(onBaixaProp) : undefined;
  const onEstorno = onEstornoProp ? withRefresh(onEstornoProp) : undefined;
  const isEditMode = !!editingItem;

  const {
    mutation,
    getFieldError: originalGetFieldError,
    resetErrors,
  } = useUpsertMutation({
    mutationFn: async (value: ContasReceberFormValues) => {
      const payload = {
        ...value,
        parcelas: value.parcelas.map((p) => ({
          ...p,
          dataVencimento: new Date(
            p.dataVencimento + "T12:00:00",
          ).toISOString(),
        })),
      };
      return isEditMode
        ? await contasReceberApi.update(editingItem.id, payload)
        : await contasReceberApi.create(payload);
    },
    queryKey: ["contas-receber"],
    onSuccessCallback: () => activeWindow.resolve(true),
  });

  // Documento que gerou o título (venda, devolução, compra): botão dinâmico, só em leitura.
  const origemActions = useOrigemAction(editingItem?.origemTipo, editingItem?.origemId);
  return (
    <div className="flex flex-col gap-4">
      <WindowActions>
        {readOnly && <RelatedActions actions={origemActions} />}
        <WindowCancelButton readOnly={readOnly} />
        {!readOnly && (
          <Button
            type="submit"
            form="upsert-contas-receber"
            disabled={mutation.isPending}
          >
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
      </WindowActions>
      <ContasReceberFormBody
        key={editingItem ? `${editingItem.id}-${version}` : "new"}
        editingItem={editingItem}
        readOnly={readOnly}
        onBaixa={onBaixa}
        onEstorno={onEstorno}
        mutation={mutation}
        originalGetFieldError={originalGetFieldError}
        resetErrors={resetErrors}
      />
    </div>
  );
}

interface ContasReceberFormBodyProps {
  editingItem: ContasReceber | null;
  readOnly: boolean;
  onBaixa?: (
    contaId: number,
    parcela: ContasReceberParcela,
  ) => void | Promise<void>;
  onEstorno?: (
    contaId: number,
    parcela: ContasReceberParcela,
  ) => void | Promise<void>;
  mutation: UseMutationResult<
    BackendResult<ContasReceber>,
    unknown,
    ContasReceberFormValues,
    unknown
  >;
  originalGetFieldError: (
    name: string,
    formErrors: unknown[],
  ) => string | undefined;
  resetErrors: () => void;
}

function ContasReceberFormBody({
  editingItem,
  readOnly,
  onBaixa,
  onEstorno,
  mutation,
  originalGetFieldError,
  resetErrors,
}: ContasReceberFormBodyProps) {
  const activeWindow = useWindow<true>();
  const ui = useUi();
  const parcelasEditadas = React.useRef(false);
  const isEditMode = !!editingItem;
  // Sem ações disponíveis (ex.: conta aberta em leitura a partir de outro documento) a coluna não aparece.
  const temColunaAcoes = isEditMode && (!!onBaixa || !!onEstorno);

  const [parcelas, setParcelas] = useState<ContasReceberParcela[]>(
    editingItem?.contasReceberParcelas?.map((p) => ({
      numeroParcela: p.numeroParcela,
      dataVencimento: p.dataVencimento ? p.dataVencimento.split("T")[0] : "",
      valorParcela: p.valorParcela,
      valorRecebido: p.valorRecebido,
      status: p.status,
    })) ?? [],
  );

  const [cliente, setCliente] = useState<Cliente | null>(
    editingItem?.cliente ?? null,
  );
  const [condicao, setCondicao] = useState<CondicaoPagamento | null>(
    editingItem?.condicaoPagamento ?? null,
  );

  const [localErrors, setLocalErrors] = useState<Record<string, string>>({});

  const temParcelaPagaOuParcial = parcelas.some(
    (p) => p.status === "PAGO" || p.status === "PARCIAL",
  );

  const getFieldError = (name: string, formErrors: unknown[]) => {
    return localErrors[name] || originalGetFieldError(name, formErrors);
  };

  const form = useForm({
    defaultValues: {
      descricao: editingItem?.descricao ?? "",
      clienteId: editingItem?.cliente?.id ?? 0,
      dataEmissao: editingItem?.dataEmissao
        ? editingItem.dataEmissao.split("T")[0]
        : todayLocalISODate(),
      valorOriginal: editingItem?.valorOriginal ?? 0,
      condicaoPagamentoId: editingItem?.condicaoPagamento?.id ?? null,
      observacao: editingItem?.observacao ?? "",
      parcelas: parcelas,
    } as ContasReceberFormValues,
    onSubmit: async ({ value, formApi }) => {
      // Barreira final: botão, Enter e atalho passam por aqui, então nada inválido é salvo.
      if (!contasReceberSchema.safeParse(value).success)
        return void formApi.validateAllFields("submit");
      resetErrors();
      setLocalErrors({});
      const payload = {
        ...value,
        parcelas,
      };

      const validationResult = contasReceberSchema.safeParse(payload);
      if (!validationResult.success) {
        const errorsMap: Record<string, string> = {};
        validationResult.error.errors.forEach((err) => {
          const path = err.path.join(".");
          errorsMap[path] = err.message;
        });
        setLocalErrors(errorsMap);
        return;
      }

      try {
        await mutation.mutateAsync(payload as ContasReceberFormValues);
      } catch {
        // O hook central já apresenta o erro operacional em um toast.
      }
    },
  });

  const valorGerado = React.useRef<number | null>(
    form.state.values.valorOriginal,
  );

  const gerarSugeridas = async (
    valor: number,
    cond: CondicaoPagamento | null,
    emissaoStr?: string | null,
  ) => {
    if (temParcelaPagaOuParcial) return;
    if (!cond || valor <= 0) return;
    if (parcelasEditadas.current && parcelas.length > 0) {
      const confirmado = await ui.windows.confirm({
        title: "Substituir parcelas",
        description:
          "As parcelas foram editadas manualmente. Deseja gerá-las novamente e descartar as alterações?",
        confirmLabel: "Substituir",
      });
      if (!confirmado) return;
    }
    valorGerado.current = valor;
    const baseDate = emissaoStr
      ? new Date(emissaoStr + "T12:00:00")
      : new Date();
    const sugeridas: ContasReceberParcela[] = [];
    let count = 1;

    const entradaPercent = cond.entradaMinimaPercentual ?? 0;
    if (entradaPercent > 0) {
      const valEntrada = parseFloat(
        (valor * (entradaPercent / 100)).toFixed(2),
      );
      sugeridas.push({
        numeroParcela: count++,
        dataVencimento: toLocalISODate(baseDate),
        valorParcela: valEntrada,
        valorRecebido: 0,
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
        dataVencimento: toLocalISODate(venc),
        valorParcela: valParcela,
        valorRecebido: 0,
        status: "ABERTO",
      });
    });

    if (sugeridas.length > 0) {
      const totalSugerido = sugeridas.reduce(
        (sum, p) => sum + p.valorParcela,
        0,
      );
      const diff = valor - totalSugerido;
      if (Math.abs(diff) > 0.001) {
        sugeridas[sugeridas.length - 1].valorParcela = parseFloat(
          (sugeridas[sugeridas.length - 1].valorParcela + diff).toFixed(2),
        );
      }
    }

    parcelasEditadas.current = false;
    setParcelas(sugeridas);
  };

  const handleAddParcela = () => {
    if (readOnly || temParcelaPagaOuParcial) return;
    parcelasEditadas.current = true;
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
        dataVencimento: toLocalISODate(baseDate),
        valorParcela: parseFloat(rem.toFixed(2)),
        valorRecebido: 0,
        status: "ABERTO",
      },
    ]);
  };

  const handleRemoveParcela = (index: number) => {
    if (readOnly || temParcelaPagaOuParcial) return;
    parcelasEditadas.current = true;
    const filtered = parcelas.filter((_, i) => i !== index);
    const reindexed = filtered.map((p, idx) => ({
      ...p,
      numeroParcela: idx + 1,
    }));
    setParcelas(reindexed);
  };

  const handleUpdateParcela = (
    index: number,
    key: keyof ContasReceberParcela,
    value: string | number,
  ) => {
    if (readOnly || temParcelaPagaOuParcial) return;
    parcelasEditadas.current = true;
    const updated = [...parcelas];
    updated[index] = {
      ...updated[index],
      [key]: value,
    } as ContasReceberParcela;
    setParcelas(updated);
  };

  const totalParcelasSum = parcelas.reduce((sum, p) => sum + p.valorParcela, 0);
  const valorOriginalForm = useSelector(
    form.store,
    (s) => s.values.valorOriginal,
  );

  const [initialParcelasJson] = useState(() => JSON.stringify(parcelas));
  const registerDirty = useCallback(() => {
    activeWindow.setDirtyCheck(
      () =>
        form.state.isDirty || JSON.stringify(parcelas) !== initialParcelasJson,
    );
    return () => activeWindow.setDirtyCheck(null);
  }, [activeWindow, form, parcelas, initialParcelasJson]);

  useWindowCommands([
    {
      id: "contas-receber.save",
      hotkey: "Alt+Enter",
      label: "Salvar",
      enabled: !readOnly && !mutation.isPending,
      run: async (event) => {
        event.preventDefault();
        await form.handleSubmit();
      },
    },
    {
      id: "contas-receber.add-parcela",
      hotkey: "Alt+P",
      label: "Adicionar parcela",
      enabled: !readOnly && !temParcelaPagaOuParcial,
      run: async (event) => {
        event.preventDefault();
        handleAddParcela();
      },
    },
  ]);

  return (
    <form
      id="upsert-contas-receber"
      ref={registerDirty}
      className="flex flex-col gap-6"
      onSubmit={async (e) => {
        e.preventDefault();
        e.stopPropagation();
        await form.handleSubmit();
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

          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <form.Field
              name="descricao"
              validators={{ onChange: contasReceberBaseSchema.shape.descricao }}
            >
              {(field) => (
                <div className="col-span-1 flex flex-col gap-2 md:col-span-2">
                  <FieldLabel htmlFor={field.name}>Descrição</FieldLabel>
                  <Input
                    id={field.name}
                    name={field.name}
                    value={field.state.value}
                    onChange={(e) => field.handleChange(e.target.value)}
                    onBlur={field.handleBlur}
                    readOnly={readOnly}
                    inputSize="full"
                    maxLength={150}
                    className={cn(
                      getFieldError(field.name, field.state.meta.errors) &&
                        "border-destructive focus-visible:ring-destructive",
                    )}
                  />
                  {getFieldError(field.name, field.state.meta.errors) && (
                    <FieldError>
                      {getFieldError(field.name, field.state.meta.errors)}
                    </FieldError>
                  )}
                </div>
              )}
            </form.Field>

            <form.Field
              name="clienteId"
              validators={{ onChange: contasReceberBaseSchema.shape.clienteId }}
            >
              {(field) => {
                const error = getFieldError(
                  field.name,
                  field.state.meta.errors,
                );
                return (
                  <ClienteInput
                    name={field.name}
                    error={error}
                    initialItem={cliente}
                    onSelectId={(id) => field.handleChange(id ?? 0)}
                    onSelectItem={setCliente}
                    disabled={readOnly}
                  />
                );
              }}
            </form.Field>
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
            <form.Field
              name="valorOriginal"
              validators={{
                onChange: contasReceberBaseSchema.shape.valorOriginal,
              }}
            >
              {(field) => (
                <div className="flex flex-col gap-2">
                  <FieldLabel htmlFor={field.name}>
                    Valor Original (R$)
                  </FieldLabel>
                  <NumberInput
                    id={field.name}
                    name={field.name}
                    onBlur={() => {
                      field.handleBlur();
                      if (field.state.value !== valorGerado.current) {
                        gerarSugeridas(
                          field.state.value,
                          condicao,
                          form.getFieldValue("dataEmissao") as string | null,
                        );
                      }
                    }}
                    inputSize="full"
                    value={field.state.value}
                    decimals={2}
                    inputMode="decimal"
                    onNumberChange={(num) => {
                      if (num !== field.state.value) {
                        field.handleChange(num);
                      }
                    }}
                    readOnly={readOnly || temParcelaPagaOuParcial}
                    className={cn(
                      "text-right font-semibold",
                      getFieldError(field.name, field.state.meta.errors) &&
                        "border-destructive focus-visible:ring-destructive",
                    )}
                  />
                  {getFieldError(field.name, field.state.meta.errors) && (
                    <FieldError>
                      {getFieldError(field.name, field.state.meta.errors)}
                    </FieldError>
                  )}
                </div>
              )}
            </form.Field>

            <form.Field name="condicaoPagamentoId">
              {(field) => {
                const error = getFieldError(
                  field.name,
                  field.state.meta.errors,
                );
                return (
                  <div className="col-span-1 md:col-span-2">
                    <CondicaoPagamentoInput
                      name={field.name}
                      error={error}
                      initialItem={condicao}
                      onSelectId={(id) => field.handleChange(id)}
                      onSelectItem={(cond) => {
                        if (cond?.id !== condicao?.id) {
                          setCondicao(cond);
                          gerarSugeridas(
                            form.getFieldValue("valorOriginal") as number,
                            cond,
                            form.getFieldValue("dataEmissao") as string | null,
                          );
                        }
                      }}
                      disabled={readOnly || temParcelaPagaOuParcial}
                    />
                  </div>
                );
              }}
            </form.Field>
          </div>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <form.Field name="dataEmissao">
              {(field) => (
                <div className="flex flex-col gap-2">
                  <FieldLabel htmlFor={field.name}>Data Emissão</FieldLabel>
                  <DatePicker
                    id={field.name}
                    name={field.name}
                    onBlur={field.handleBlur}
                    value={field.state.value}
                    onChange={(val) => {
                      if (val !== field.state.value) {
                        field.handleChange(val);
                        gerarSugeridas(
                          form.getFieldValue("valorOriginal") as number,
                          condicao,
                          val,
                        );
                      }
                    }}
                    readOnly={readOnly || temParcelaPagaOuParcial}
                  />
                </div>
              )}
            </form.Field>
          </div>

          <form.Field name="observacao">
            {(field) => (
              <div className="flex flex-col gap-2">
                <FieldLabel htmlFor={field.name}>Observações</FieldLabel>
                <Button
                  id={field.name}
                  type="button"
                  variant="outline"
                  className="justify-start font-normal"
                  onClick={async () => {
                    const r = await ui.windows.open<string, TextoWindowProps>({
                      component: TextoWindow,
                      props: {
                        label: "Observações",
                        value: field.state.value ?? "",
                        readOnly,
                      },
                      title: "Observações",
                      size: "large",
                    });
                    if (r.status === "confirmed") field.handleChange(r.value);
                  }}
                >
                  <FileText />
                  <span className="truncate">
                    {field.state.value ||
                      (readOnly ? "Sem observações" : "Adicionar observações")}
                  </span>
                </Button>
              </div>
            )}
          </form.Field>
        </div>

        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold tracking-wider">
              Parcelas de Recebimento
            </h3>
            {!readOnly && !temParcelaPagaOuParcial && (
              <Button
                type="button"
                variant="outline"
                onClick={handleAddParcela}
              >
                <Plus /> Adicionar Parcela{" "}
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
                <div className="flex w-full flex-col">
                  <ScrollArea className="max-h-60 flex-1 pr-2">
                    {parcelas.length === 0 ? (
                      <Empty>
                        <EmptyHeader>
                          <EmptyTitle>Nenhuma parcela definida</EmptyTitle>
                          <EmptyDescription>
                            Adicione uma parcela manualmente ou selecione uma
                            condição de pagamento para calculá-las.
                          </EmptyDescription>
                        </EmptyHeader>
                      </Empty>
                    ) : (
                      <div className="bg-card overflow-clip rounded-lg border">
                        <Table pageScroll>
                          <TableHeader>
                            <TableRow className="hover:bg-transparent">
                              <TableHead className="w-16">Parcela</TableHead>
                              <TableHead>Vencimento</TableHead>
                              <TableHead className="text-right">
                                Valor Parcela (R$)
                              </TableHead>
                              <TableHead className="text-right">
                                Valor Recebido (R$)
                              </TableHead>
                              <TableHead className="text-center">
                                Status
                              </TableHead>
                              {temColunaAcoes && (
                                <TableHead className="w-24 text-right">
                                  Ações
                                </TableHead>
                              )}
                              {!readOnly && !temParcelaPagaOuParcial && (
                                <TableHead className="w-12 text-right" />
                              )}
                            </TableRow>
                          </TableHeader>
                          <TableBody>
                            {parcelas.map((p, index) => (
                              <TableRow
                                key={`parcela-${p.numeroParcela}`}
                                className="hover:bg-transparent"
                              >
                                <TableCell className="font-semibold">
                                  #{p.numeroParcela}
                                </TableCell>
                                <TableCell>
                                  <div className="flex w-full flex-col gap-1">
                                    <div
                                      className="contents"
                                      {...navigationCell({
                                        grid: "contas-receber-parcelas",
                                        row: index,
                                        column: 0,
                                      })}
                                    >
                                      <DatePicker
                                        id={`parcela-${index}-dataVencimento`}
                                        name={`parcelas.${index}.dataVencimento`}
                                        value={p.dataVencimento}
                                        onChange={(val) =>
                                          handleUpdateParcela(
                                            index,
                                            "dataVencimento",
                                            val ?? "",
                                          )
                                        }
                                        onFocus={(e) => e.target.select()}
                                        readOnly={
                                          readOnly ||
                                          temParcelaPagaOuParcial ||
                                          p.status === "PAGO"
                                        }
                                        className={cn(
                                          "h-8 text-xs",
                                          localErrors[
                                            `parcelas.${index}.dataVencimento`
                                          ] &&
                                            "border-destructive focus-visible:ring-destructive",
                                        )}
                                        aria-invalid={
                                          !!localErrors[
                                            `parcelas.${index}.dataVencimento`
                                          ]
                                        }
                                      />
                                    </div>
                                    {localErrors[
                                      `parcelas.${index}.dataVencimento`
                                    ] && (
                                      <span className="text-destructive mt-1 block text-right text-xs">
                                        {
                                          localErrors[
                                            `parcelas.${index}.dataVencimento`
                                          ]
                                        }
                                      </span>
                                    )}
                                  </div>
                                </TableCell>
                                <TableCell>
                                  <div className="flex w-full flex-col gap-1">
                                    <NumberInput
                                      {...navigationCell({
                                        grid: "contas-receber-parcelas",
                                        row: index,
                                        column: 1,
                                      })}
                                      id={`parcela-${index}-valorParcela`}
                                      name={`parcelas.${index}.valorParcela`}
                                      inputSize="full"
                                      value={p.valorParcela}
                                      decimals={2}
                                      inputMode="decimal"
                                      onNumberChange={(num) =>
                                        handleUpdateParcela(
                                          index,
                                          "valorParcela",
                                          num,
                                        )
                                      }
                                      onFocus={(e) => e.target.select()}
                                      readOnly={
                                        readOnly ||
                                        temParcelaPagaOuParcial ||
                                        p.status === "PAGO"
                                      }
                                      className={cn(
                                        "h-8 text-right font-semibold",
                                        localErrors[
                                          `parcelas.${index}.valorParcela`
                                        ] &&
                                          "border-destructive focus-visible:ring-destructive",
                                      )}
                                      aria-invalid={
                                        !!localErrors[
                                          `parcelas.${index}.valorParcela`
                                        ]
                                      }
                                    />
                                    {localErrors[
                                      `parcelas.${index}.valorParcela`
                                    ] && (
                                      <span className="text-destructive mt-1 block text-right text-xs">
                                        {
                                          localErrors[
                                            `parcelas.${index}.valorParcela`
                                          ]
                                        }
                                      </span>
                                    )}
                                  </div>
                                </TableCell>
                                <TableCell>
                                  <div
                                    {...navigationCell({
                                      grid: "contas-receber-parcelas",
                                      row: index,
                                      column: 2,
                                    })}
                                  >
                                    <NumberInput
                                      value={p.valorRecebido || 0}
                                      decimals={2}
                                      inputSize="full"
                                      inputMode="decimal"
                                      aria-label={`Parcela ${p.numeroParcela} valor`}
                                      readOnly
                                      className="h-8 text-right font-medium"
                                      onNumberChange={() => {}}
                                    />
                                  </div>
                                </TableCell>
                                <TableCell>
                                  <div
                                    {...navigationCell({
                                      grid: "contas-receber-parcelas",
                                      row: index,
                                      column: 3,
                                    })}
                                  >
                                    <Input
                                      value={p.status ?? ""}
                                      aria-label={`Parcela ${p.numeroParcela} status`}
                                      readOnly
                                      inputSize="full"
                                      className="h-8 text-center text-xs font-semibold uppercase"
                                    />
                                  </div>
                                </TableCell>
                                {temColunaAcoes && (
                                  <TableCell className="text-right">
                                    <div className="flex justify-end gap-2">
                                      {(p.status === "ABERTO" ||
                                        p.status === "PARCIAL") &&
                                        onBaixa && (
                                          <Button
                                            {...navigationCell({
                                              grid: "contas-receber-parcelas",
                                              row: index,
                                              column: 4,
                                            })}
                                            type="button"
                                            size="icon-sm"
 variant="ghost"
                                            onClick={() =>
                                              onBaixa(editingItem!.id, p)
                                            }
                                            title="Receber parcela"
 aria-label="Receber parcela"
                                          >
                                            <Check />
                                          </Button>
                                        )}
                                      {(p.status === "PAGO" ||
                                        p.status === "PARCIAL") &&
                                        onEstorno && (
                                          <Button
                                            {...navigationCell({
                                              grid: "contas-receber-parcelas",
                                              row: index,
                                              column: 5,
                                            })}
                                            type="button"
                                            size="icon-sm"
 variant="destructive"
                                            onClick={() =>
                                              onEstorno(editingItem!.id, p)
                                            }
                                            title="Estornar Recebimento"
 aria-label="Estornar Recebimento"
                                          >
                                            <RotateCcw />
                                          </Button>
                                        )}
                                    </div>
                                  </TableCell>
                                )}
                                {!readOnly && !temParcelaPagaOuParcial && (
                                  <TableCell className="text-right">
                                    <Button
                                      {...navigationCell({
                                        grid: "contas-receber-parcelas",
                                        row: index,
                                        column: 6,
                                      })}
                                      type="button"
                                      size="icon-sm"
 variant="ghost"
 aria-label="Remover"
                                      onClick={() => handleRemoveParcela(index)}
                                      disabled={
                                        p.status === "PAGO" ||
                                        p.status === "PARCIAL"
                                      }
                                    >
                                      <Trash2 />
                                    </Button>
                                  </TableCell>
                                )}
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </div>
                    )}
                  </ScrollArea>

                  {error && <FieldError className="mt-2">{error}</FieldError>}
                </div>
              );
            }}
          </form.Field>

          <div className="mt-2 grid grid-cols-1 gap-4 md:grid-cols-3">
            {[
              ["Valor Original da Conta (R$)", valorOriginalForm],
              ["Soma das Parcelas (R$)", totalParcelasSum],
              ["Diferença (R$)", valorOriginalForm - totalParcelasSum],
            ].map(([rotulo, v]) => (
              <div key={rotulo as string} className="flex flex-col gap-2">
                <FieldLabel>{rotulo}</FieldLabel>
                <NumberInput
                  value={v as number}
                  decimals={2}
                  inputSize="full"
                  inputMode="decimal"
                  aria-label={rotulo as string}
                  readOnly
                  className="h-8 text-right font-semibold"
                  onNumberChange={() => {}}
                />
              </div>
            ))}
          </div>
        </div>
      </div>
    </form>
  );
}
