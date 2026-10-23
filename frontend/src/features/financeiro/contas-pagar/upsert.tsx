"use client";

import React, { useState } from "react";
import { WindowActions, WindowCancelButton } from "@/ui/imperative";
import { Button } from "@/ui/primitives";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { FieldLabel, FieldError } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { NumberInput } from "@/ui/composites";
import { DatePicker } from "@/ui/composites";
import { TextoWindow, type TextoWindowProps } from "@/components/texto-window";
import { Separator } from "@/ui/primitives";
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from "@/ui/primitives";
import { FornecedorInput } from "@/components/entity-inputs/fornecedor-input";
import { CondicaoPagamentoInput } from "@/components/entity-inputs/condicao-pagamento-input";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { contasPagarApi } from "@/api/financeiro";
import type { Fornecedor } from "@/features/parceiros/fornecedores/types";
import type { CondicaoPagamento } from "@/features/financeiro/condicoes/types";
import { origemTituloLabels } from "../origem";
import { contasPagarSchema, contasPagarEdicaoSchema, statusTituloLabels, type ContasPagar } from "./types";
import { FileText, Plus, Trash2 } from "lucide-react";
import { useUi, useWindow, useWindowCommands } from "@/ui/imperative";
import { RelatedActions } from "@/components/related-actions";
import { useOrigemAction } from "@/hooks/use-origem-action";
import { toLocalISODate, todayLocalISODate } from "@/utils/date-utils";

export interface ContasPagarUpsertProps {
  editingItem: ContasPagar | null;
  readOnly?: boolean;
}

interface ParcelaForm {
  dataVencimento: string;
  valor: number;
}

const dia = (v?: string | null) => (v ? v.split("T")[0] : "");
// O backend recebe o instante ao meio-dia para não mudar de dia por fuso.
const paraIso = (d: string) => new Date(d + "T12:00:00").toISOString();

export function ContasPagarUpsertForm({ editingItem, readOnly = false }: ContasPagarUpsertProps) {
  const activeWindow = useWindow<true>();
  const ui = useUi();
  const isEditMode = !!editingItem;
  // Edição de uma parcela existente só vale para lançamento manual; título de outro documento é leitura.
  const somenteLeitura = readOnly || (isEditMode && editingItem.origemTipo !== "MANUAL");
  const origemActions = useOrigemAction(editingItem?.origemTipo, editingItem?.origemId);

  const [descricao, setDescricao] = useState(editingItem?.descricao ?? "");
  const [observacao, setObservacao] = useState(editingItem?.observacao ?? "");
  const [fornecedor, setFornecedor] = useState<Fornecedor | null>(editingItem?.fornecedor ?? null);
  const [condicao, setCondicao] = useState<CondicaoPagamento | null>(editingItem?.condicaoPagamento ?? null);
  const [numeroDocumento, setNumeroDocumento] = useState("");
  const [dataEmissao, setDataEmissao] = useState(dia(editingItem?.dataEmissao) || todayLocalISODate());
  const [valorTotal, setValorTotal] = useState(0);
  const [parcelas, setParcelas] = useState<ParcelaForm[]>([]);
  // Edição: uma linha só.
  const [vencimento, setVencimento] = useState(dia(editingItem?.dataVencimento));
  const [valor, setValor] = useState(editingItem?.valor ?? 0);
  const [erros, setErros] = useState<Record<string, string>>({});

  const { mutation, getFieldError, resetErrors } = useUpsertMutation<void>({
    mutationFn: async () => {
      if (isEditMode) {
        return contasPagarApi.update(editingItem.id, {
          descricao,
          dataVencimento: paraIso(vencimento),
          valor,
          observacao: observacao || null,
        });
      }
      return contasPagarApi.create({
        descricao,
        fornecedorId: fornecedor?.id ?? 0,
        numeroDocumento: numeroDocumento.trim() || null,
        dataEmissao: paraIso(dataEmissao),
        condicaoPagamentoId: condicao?.id ?? null,
        observacao: observacao || null,
        parcelas: parcelas.map((p) => ({ ...p, dataVencimento: paraIso(p.dataVencimento) })),
      });
    },
    queryKey: ["contas-pagar"],
    onSuccessCallback: () => activeWindow.resolve(true),
  });

  const gerar = (total: number, cond: CondicaoPagamento | null, emissao: string) => {
    if (!cond || total <= 0) return;
    const base = new Date(emissao + "T12:00:00");
    const gerado: ParcelaForm[] = [];
    const entrada = cond.entradaMinimaPercentual ?? 0;
    if (entrada > 0) gerado.push({ dataVencimento: toLocalISODate(base), valor: parseFloat((total * (entrada / 100)).toFixed(2)) });
    (cond.condicoesPagamentosParcelas || []).forEach((it) => {
      const venc = new Date(base);
      venc.setDate(base.getDate() + it.prazoDias);
      gerado.push({ dataVencimento: toLocalISODate(venc), valor: parseFloat((total * (it.percentual / 100)).toFixed(2)) });
    });
    if (gerado.length > 0) {
      const diff = total - gerado.reduce((s, p) => s + p.valor, 0);
      gerado[gerado.length - 1].valor = parseFloat((gerado[gerado.length - 1].valor + diff).toFixed(2));
    }
    setParcelas(gerado);
  };

  const addParcela = () => {
    const base = new Date(dataEmissao + "T12:00:00");
    base.setDate(base.getDate() + parcelas.length * 30 + 30);
    const resto = Math.max(0, valorTotal - parcelas.reduce((s, p) => s + p.valor, 0));
    setParcelas([...parcelas, { dataVencimento: toLocalISODate(base), valor: parseFloat(resto.toFixed(2)) }]);
  };
  const updParcela = (i: number, patch: Partial<ParcelaForm>) =>
    setParcelas(parcelas.map((p, idx) => (idx === i ? { ...p, ...patch } : p)));

  const somaParcelas = parcelas.reduce((s, p) => s + p.valor, 0);

  const salvar = async () => {
    resetErrors();
    const r = isEditMode
      ? contasPagarEdicaoSchema.safeParse({ descricao, dataVencimento: vencimento, valor, observacao: observacao || null })
      : contasPagarSchema.safeParse({
          descricao,
          fornecedorId: fornecedor?.id ?? 0,
          numeroDocumento: numeroDocumento || null,
          dataEmissao,
          condicaoPagamentoId: condicao?.id ?? null,
          observacao: observacao || null,
          parcelas,
        });
    const map: Record<string, string> = {};
    if (!r.success) r.error.errors.forEach((e) => (map[e.path.join(".")] = e.message));
    else if (!isEditMode && Math.abs(somaParcelas - valorTotal) > 0.01)
      map.parcelas = `A soma das parcelas (R$ ${somaParcelas.toFixed(2)}) deve ser igual ao valor total (R$ ${valorTotal.toFixed(2)}).`;
    setErros(map);
    if (Object.keys(map).length > 0) return;
    await mutation.mutateAsync().catch(() => undefined);
  };

  const commands = React.useMemo(
    () => [
      {
        id: "contas-pagar.save",
        hotkey: "Alt+Enter" as const,
        label: "Salvar",
        enabled: !somenteLeitura && !mutation.isPending,
        run: async (event: KeyboardEvent) => {
          event.preventDefault();
          await salvar();
        },
      },
      {
        id: "contas-pagar.add-parcela",
        hotkey: "Alt+P" as const,
        label: "Adicionar parcela",
        enabled: !somenteLeitura && !isEditMode,
        run: async (event: KeyboardEvent) => {
          event.preventDefault();
          addParcela();
        },
      },
    ],
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [somenteLeitura, isEditMode, mutation.isPending, descricao, observacao, fornecedor, condicao, numeroDocumento, dataEmissao, valorTotal, parcelas, vencimento, valor],
  );
  useWindowCommands(commands);

  const erro = (n: string) => erros[n] ?? getFieldError(n, []);
  const campo = (n: string, rotulo: string, el: React.ReactNode) => (
    <div className="flex flex-col gap-2">
      <FieldLabel>{rotulo}</FieldLabel>
      {el}
      {erro(n) && <FieldError>{erro(n)}</FieldError>}
    </div>
  );
  const brl = (v: number) => new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(v);

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(e) => {
        e.preventDefault();
        void salvar();
      }}
    >
      <WindowActions>
        {readOnly && <RelatedActions actions={origemActions} />}
        <WindowCancelButton readOnly={somenteLeitura} />
        {!somenteLeitura && (
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? "Salvando..." : (
              <span className="flex items-center gap-2">
                Salvar <KbdGroup><Kbd>Alt</Kbd><Kbd>Enter</Kbd></KbdGroup>
              </span>
            )}
          </Button>
        )}
      </WindowActions>

      <div className="flex flex-col gap-4">
        <div className="flex flex-col gap-2">
          <h3 className="text-sm font-semibold tracking-wider">{isEditMode ? "Parcela" : "Documento"}</h3>
          <Separator />
        </div>

        {isEditMode && (
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
            {[
              ["Documento", `${origemTituloLabels[editingItem.origemTipo] ?? editingItem.origemTipo} ${editingItem.numeroDocumento}`],
              ["Parcela", `${editingItem.numeroParcela}/${editingItem.totalParcelas}`],
              ["Fornecedor / Cliente", editingItem.fornecedor?.nomeRazaosocial || editingItem.clienteNome || "-"],
              ["Status", statusTituloLabels[editingItem.status] ?? editingItem.status],
              ["Pago", brl(editingItem.valorPago)],
              ["Saldo", brl(editingItem.valorSaldo)],
            ].map(([r, v]) => (
              <div key={r} className="flex flex-col gap-1">
                <span className="text-muted-foreground text-xs">{r}</span>
                <span className="font-semibold">{v}</span>
              </div>
            ))}
          </div>
        )}

        <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
          <div className="md:col-span-2">
            {campo("descricao", "Descrição",
              <Input value={descricao} onChange={(e) => setDescricao(e.target.value)} readOnly={somenteLeitura} inputSize="full" maxLength={150} />)}
          </div>
          {!isEditMode &&
            campo("numeroDocumento", "Nº do documento",
              <Input value={numeroDocumento} onChange={(e) => setNumeroDocumento(e.target.value)} inputSize="full" maxLength={30} placeholder="Gerado automaticamente" />)}
        </div>

        {!isEditMode && (
          <>
            <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
              <FornecedorInput
                name="fornecedorId"
                error={erro("fornecedorId")}
                initialItem={fornecedor}
                onSelectId={() => undefined}
                onSelectItem={setFornecedor}
              />
              {campo("dataEmissao", "Data Emissão",
                <DatePicker
                  value={dataEmissao}
                  onChange={(v) => {
                    if (!v || v === dataEmissao) return;
                    setDataEmissao(v);
                    gerar(valorTotal, condicao, v);
                  }}
                />)}
              {campo("valorTotal", "Valor Total (R$)",
                <NumberInput
                  inputSize="full"
                  value={valorTotal}
                  decimals={2}
                  inputMode="decimal"
                  onNumberChange={setValorTotal}
                  onBlur={() => gerar(valorTotal, condicao, dataEmissao)}
                  className="text-right font-semibold"
                />)}
            </div>
            <CondicaoPagamentoInput
              name="condicaoPagamentoId"
              initialItem={condicao}
              onSelectId={() => undefined}
              onSelectItem={(c) => {
                if (c?.id === condicao?.id) return;
                setCondicao(c);
                gerar(valorTotal, c, dataEmissao);
              }}
            />
          </>
        )}

        {isEditMode && (
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            {campo("dataVencimento", "Vencimento",
              <DatePicker value={vencimento} onChange={(v) => setVencimento(v ?? "")} readOnly={somenteLeitura} />)}
            {campo("valor", "Valor (R$)",
              <NumberInput
                inputSize="full"
                value={valor}
                decimals={2}
                inputMode="decimal"
                onNumberChange={setValor}
                readOnly={somenteLeitura}
                className="text-right font-semibold"
              />)}
          </div>
        )}

        <div className="flex flex-col gap-2">
          <FieldLabel>Observações</FieldLabel>
          <Button
            type="button"
            variant="outline"
            className="justify-start font-normal"
            onClick={async () => {
              const r = await ui.windows.open<string, TextoWindowProps>({
                component: TextoWindow,
                props: { label: "Observações", value: observacao, readOnly: somenteLeitura },
                title: "Observações",
                size: "large",
              });
              if (r.status === "confirmed") setObservacao(r.value);
            }}
          >
            <FileText />
            <span className="truncate">{observacao || (somenteLeitura ? "Sem observações" : "Adicionar observações")}</span>
          </Button>
        </div>
      </div>

      {!isEditMode && (
        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold tracking-wider">Parcelas</h3>
            <Button type="button" variant="outline" onClick={addParcela}>
              <Plus /> Adicionar Parcela <KbdGroup><Kbd>Alt</Kbd><Kbd>P</Kbd></KbdGroup>
            </Button>
          </div>
          <Separator />
          {parcelas.length === 0 ? (
            <p className="text-muted-foreground text-sm">Selecione uma condição de pagamento ou adicione parcelas manualmente.</p>
          ) : (
            <div className="bg-card overflow-clip rounded-lg border">
              <Table pageScroll>
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="w-16">Parcela</TableHead>
                    <TableHead>Vencimento</TableHead>
                    <TableHead className="text-right">Valor (R$)</TableHead>
                    <TableHead className="w-12" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {parcelas.map((p, i) => (
                    <TableRow key={i} className="hover:bg-transparent">
                      <TableCell className="font-semibold">{i + 1}/{parcelas.length}</TableCell>
                      <TableCell>
                        <DatePicker value={p.dataVencimento} onChange={(v) => updParcela(i, { dataVencimento: v ?? "" })} className="h-8 text-xs" />
                      </TableCell>
                      <TableCell>
                        <NumberInput
                          inputSize="full"
                          value={p.valor}
                          decimals={2}
                          inputMode="decimal"
                          onNumberChange={(n) => updParcela(i, { valor: n })}
                          className="h-8 text-right font-semibold"
                        />
                      </TableCell>
                      <TableCell className="text-right">
                        <Button type="button" size="icon-sm" variant="ghost" aria-label="Remover" onClick={() => setParcelas(parcelas.filter((_, x) => x !== i))}>
                          <Trash2 />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
          {erro("parcelas") && <FieldError>{erro("parcelas")}</FieldError>}
          <div className="mt-2 flex justify-end gap-6 text-sm">
            <span>Soma das parcelas: <b>{brl(somaParcelas)}</b></span>
            <span>Diferença: <b>{brl(valorTotal - somaParcelas)}</b></span>
          </div>
        </div>
      )}
    </form>
  );
}
