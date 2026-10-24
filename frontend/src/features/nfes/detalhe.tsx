"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, Copy, Download, ExternalLink, FileCode, Printer } from "lucide-react";
import { WindowActions } from "@/ui/imperative";
import { nfesApi } from "@/api/nfes";
import { contasPagarApi } from "@/api/financeiro";
import { comprasApi } from "@/api/compras";
import { CompraInput } from "@/components/entity-inputs/compra-input";
import { TextoWindow, type TextoWindowProps } from "@/components/texto-window";
import {
  Button, Field, FieldLabel, HotkeyKbd, Input, Kbd, Spinner, Table, TableBody, TableCell, TableHead, TableHeader, TableRow, Textarea,
} from "@/ui/primitives";
import { navigationCell } from "@/ui/keyboard-navigation";
import { useWindow, useWindowCommands, useUi, WindowCancelButton } from "@/ui/imperative";
import { formatDateToLocal, formatToLocal } from "@/utils/date-utils";
import { CompraWindow, type CompraWindowProps } from "@/features/compras/upsert";
import { ContasPagarUpsertForm, type ContasPagarUpsertProps } from "@/features/financeiro/contas-pagar/upsert";
import { CancelarNfeWindow } from "./cancelar";
import { Danfe } from "./danfe";
import { brl, chaveFormatada, modalidadeFreteLabels, statusLabels, tipoLabels, type Nfe } from "./types";

export function baixarXml(xml: string, nome: string) {
  const url = URL.createObjectURL(new Blob([xml], { type: "application/xml" }));
  const a = document.createElement("a");
  a.href = url;
  a.download = `${nome}.xml`;
  a.click();
  URL.revokeObjectURL(url);
}

const Bloco = ({ titulo, children }: { titulo: string; children: React.ReactNode }) => (
  <section className="rounded-lg border p-4">
    <h3 className="mb-2 text-sm font-semibold">{titulo}</h3>
    {children}
  </section>
);

/** Valor de leitura como input somente leitura: entra na ordem do foco e a seta percorre a tela inteira. */
const Dado = ({ rotulo, valor, className }: { rotulo: string; valor: string | number | null | undefined; className?: string }) => (
  <Field className={className}>
    <FieldLabel>{rotulo}</FieldLabel>
    <Input readOnly inputSize="full" aria-label={rotulo} value={valor == null || valor === "" ? "-" : String(valor)} />
  </Field>
);

const statusParcelaLabels: Record<string, string> = {
  ABERTO: "Em aberto", PARCIAL: "Parcial", PAGO: "Paga", CANCELADO: "Cancelada",
};

/** Tabela de valores de leitura: cabeçalho único e uma linha por registro, cada célula um input navegável. */
const GradeLeitura = ({ grade, colunas, linhas, onAbrir, rotuloAbrir, direita = [] }: {
  grade: string; colunas: string[]; linhas: string[][];
  /** Abre o registro da linha: Enter em qualquer célula ou o botão da última coluna. */
  onAbrir?: (row: number) => void; rotuloAbrir?: string;
  /** Índices das colunas numéricas/monetárias (alinhadas à direita). */
  direita?: number[];
}) => (
  <div className="rounded-lg border bg-card overflow-clip">
    <Table pageScroll>
      <TableHeader>
        <TableRow>
          {colunas.map((c, i) => <TableHead key={c} className={direita.includes(i) ? "text-right" : undefined}>{c}</TableHead>)}
          {onAbrir && <TableHead className="w-12" />}
        </TableRow>
      </TableHeader>
      <TableBody>
        {linhas.map((linha, row) => (
          <TableRow key={row}>
            {linha.map((valor, column) => (
              <TableCell key={column}>
                <Input
                  {...navigationCell({ grid: grade, row, column })}
                  readOnly
                  inputSize="full"
                  className={direita.includes(column) ? "text-right" : undefined}
                  aria-label={`${colunas[column]} ${row + 1}`}
                  value={valor}
                  onKeyDown={(e) => {
                    if (onAbrir && e.key === "Enter" && !e.altKey && !e.repeat) {
                      e.preventDefault();
                      onAbrir(row);
                    }
                  }}
                />
              </TableCell>
            ))}
            {onAbrir && (
              <TableCell className="text-center">
                <Button
                  {...navigationCell({ grid: grade, row, column: colunas.length })}
                  type="button"
                  size="icon"
                  variant="outline"
                  className="size-8"
                  title={rotuloAbrir ?? "Abrir"}
                  aria-label={`${rotuloAbrir ?? "Abrir"} ${row + 1}`}
                  onClick={() => onAbrir(row)}
                >
                  <ExternalLink className="size-4" />
                </Button>
              </TableCell>
            )}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  </div>
);

const Parte = ({ nome, doc, ie, endereco, bairro, tel }: {
  nome: string; doc: string; ie: string | null; endereco: string | null; bairro: string | null; tel: string | null;
}) => (
  <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
    <Dado rotulo="Nome / Razão social" valor={nome} className="md:col-span-2" />
    <Dado rotulo="CPF/CNPJ" valor={doc} />
    <Dado rotulo="IE" valor={ie} />
    <Dado rotulo="Endereço" valor={endereco} className="md:col-span-2" />
    <Dado rotulo="Bairro" valor={bairro} />
    <Dado rotulo="Telefone" valor={tel} />
  </div>
);

/** Detalhe da NF-e. Resolve com true quando algo mudou (cancelamento), para a lista recarregar. */
export function NfeDetalheWindow({ nfeId }: { nfeId: number }) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const [alterada, setAlterada] = React.useState(false);
  const { data: nfe, isLoading, error, refetch } = useQuery({
    queryKey: ["nfes", nfeId],
    queryFn: () => nfesApi.getById(nfeId),
  });

  if (isLoading)
    return <div className="flex justify-center py-10"><Spinner /></div>;
  if (error || !nfe)
    return (
      <div className="flex flex-col items-center gap-3 py-10 text-sm">
        <p className="text-destructive">Não foi possível carregar a NF-e.</p>
        <div className="flex gap-2">
          <Button onClick={() => void refetch()}>Tentar novamente</Button>
          <WindowCancelButton readOnly />
        </div>
      </div>
    );

  const podeCancelar = nfe.tipoOperacao === "SAIDA" && nfe.statusNfe === "EMITIDA";
  const motivoSemCancelar =
    nfe.tipoOperacao === "ENTRADA"
      ? nfe.statusNfe === "CANCELADA" ? "NF-e de entrada cancelada pelo estorno da compra." : "NF-e de entrada não é cancelada diretamente: estorne a compra."
      : nfe.statusNfe === "CANCELADA" ? "A NF-e já está cancelada." : "Somente NF-e emitida pode ser cancelada.";

  const copiarChave = async () => {
    await navigator.clipboard.writeText(nfe.chaveAcesso ?? "");
    ui.feedback.notify({ type: "success", title: "Chave de acesso copiada." });
  };

  const buscarXml = async () => {
    try {
      return await nfesApi.xml(nfe.id);
    } catch (e) {
      ui.feedback.notifyError(e, { fallbackTitle: "Não foi possível obter o XML." });
      return null;
    }
  };
  const verXml = async () => {
    const xml = await buscarXml();
    if (xml) await ui.windows.open<string, TextoWindowProps>({
      component: TextoWindow, props: { label: "XML da NF-e", value: xml }, title: `XML da NF-e ${nfe.numero}`, size: "large",
    });
  };
  const baixar = async () => {
    const xml = await buscarXml();
    if (xml) baixarXml(xml, nfe.chaveAcesso ?? `nfe-${nfe.id}`);
  };
  const cancelar = async () => {
    if (!podeCancelar) return;
    const r = await ui.windows.open<string, { numero: number }>({
      component: CancelarNfeWindow, props: { numero: nfe.numero }, title: `Cancelar NF-e ${nfe.numero}`, size: "small",
    });
    if (r.status !== "confirmed") return;
    try {
      await nfesApi.cancelar(nfe.id, r.value);
      await queryClient.invalidateQueries({ queryKey: ["nfes"] });
      setAlterada(true);
      ui.feedback.notify({ type: "success", title: "NF-e cancelada." });
    } catch (e) {
      ui.feedback.notifyError(e, { fallbackTitle: "Não foi possível cancelar a NF-e." });
    }
  };

  return <Corpo nfe={nfe} alterada={alterada} acoes={{ copiarChave, verXml, baixar, cancelar }} podeCancelar={podeCancelar} motivo={motivoSemCancelar} />;
}

function Corpo({ nfe, alterada, acoes, podeCancelar, motivo }: {
  nfe: Nfe; alterada: boolean; podeCancelar: boolean; motivo: string;
  acoes: Record<"copiarChave" | "verXml" | "baixar" | "cancelar", () => Promise<void>>;
}) {
  const activeWindow = useWindow<true>();
  const ui = useUi();
  const entrada = nfe.tipoOperacao === "ENTRADA";
  const t = nfe.nfesTransportes;
  // NF-e de entrada não traz pagamento próprio: o pagamento é o das parcelas das contas a pagar da compra.
  const { data: contasDaCompra } = useQuery({
    queryKey: ["contas-pagar", "origem", "COMPRA+FRETE_COMPRA", nfe.compraId],
    queryFn: async () => {
      const [compra, frete] = await Promise.all([
        contasPagarApi.listByOrigem("COMPRA", nfe.compraId!),
        contasPagarApi.listByOrigem("FRETE_COMPRA", nfe.compraId!),
      ]);
      return { itens: [...compra.itens, ...frete.itens] };
    },
    enabled: entrada && !!nfe.compraId,
  });
  const { data: compraDaNfe } = useQuery({
    queryKey: ["compras", "detail", nfe.compraId],
    queryFn: () => comprasApi.getById(nfe.compraId!),
    enabled: !!nfe.compraId,
  });
  const parcelasDaCompra = contasDaCompra?.itens ?? [];
  const abrirCompra = async () => {
    if (!nfe.compraId) return;
    await ui.windows.open<true, CompraWindowProps>({
      component: CompraWindow, props: { compraId: nfe.compraId }, title: `Compra #${nfe.compraId}`, size: "full",
    });
  };
  const abrirConta = async (contaId: number) => {
    try {
      const conta = await contasPagarApi.getById(contaId);
      await ui.windows.open<true, ContasPagarUpsertProps>({
        component: ContasPagarUpsertForm,
        props: { editingItem: conta, readOnly: true },
        title: "Detalhes da Conta a Pagar",
        size: "large",
      });
    } catch (error) {
      ui.feedback.notifyError(error, { fallbackTitle: "Não foi possível abrir a conta a pagar." });
    }
  };
  const fechar = () => (alterada ? activeWindow.resolve(true) : activeWindow.dismiss("cancel"));

  const imprimir = async () => {
    await ui.windows.open<true, { id: number }>({
      component: Danfe, props: { id: nfe.id }, title: `DANFE - NF-e ${nfe.numero}`, size: "full",
    });
  };

  useWindowCommands([
    { id: "nfe.xml", hotkey: "Alt+X", label: "Ver XML", run: (e) => { e.preventDefault(); void acoes.verXml(); } },
    { id: "nfe.baixar", hotkey: "Alt+D", label: "Baixar XML", run: (e) => { e.preventDefault(); void acoes.baixar(); } },
    { id: "nfe.imprimir", hotkey: "Alt+I", label: "Imprimir DANFE", run: (e) => { e.preventDefault(); void imprimir(); } },
  ]);

  return (
    <div className="flex h-full min-h-0 flex-col gap-4">
      <WindowActions>
        <div className="mr-auto flex gap-2">
          <Button type="button" variant="outline" onClick={() => void acoes.verXml()}><FileCode /> Ver XML <HotkeyKbd hotkey="Alt+X" /></Button>
          <Button type="button" variant="outline" onClick={() => void acoes.baixar()}><Download /> Baixar XML <HotkeyKbd hotkey="Alt+D" /></Button>
          <Button type="button" variant="outline" onClick={() => void imprimir()}><Printer /> Imprimir DANFE <HotkeyKbd hotkey="Alt+I" /></Button>
          {!podeCancelar && <span role="status" className="text-muted-foreground self-center text-xs">{motivo}</span>}
          {!podeCancelar && entrada && nfe.compraId && (
            <Button type="button" variant="link" className="self-center px-0 text-xs" onClick={() => void abrirCompra()}>
              Abrir compra #{nfe.compraId}
            </Button>
          )}
        </div>
        <Button type="button" variant="destructive" disabled={!podeCancelar} title={podeCancelar ? undefined : motivo} onClick={() => void acoes.cancelar()}>
          <Ban /> Cancelar NF-e
        </Button>
        <Button type="button" variant="outline" onClick={fechar}>Fechar <Kbd>Esc</Kbd></Button>
      </WindowActions>

      <div className="flex min-h-0 flex-1 flex-col gap-4 -mx-1 overflow-auto px-1">
        <Bloco titulo={`NF-e ${nfe.numero} · série ${nfe.serie} · ${tipoLabels[nfe.tipoOperacao]}`}>
          <div className="grid grid-cols-2 items-end gap-3 md:grid-cols-4">
            <Dado rotulo="Situação" valor={statusLabels[nfe.statusNfe]} />
            <Dado rotulo="Emissão" valor={formatToLocal(nfe.dataEmissao)} />
            {nfe.vendaId && <Dado rotulo="Venda" valor={`#${nfe.vendaId}`} />}
            {nfe.compraId && (
              <CompraInput
                key={compraDaNfe?.id ?? "carregando"}
                name="nfe-compra"
                initialItem={compraDaNfe ?? null}
              />
            )}
          </div>
          <Field className="mt-3">
            <FieldLabel>Chave de acesso</FieldLabel>
            <div className="relative w-full">
              <Input
                readOnly
                inputSize="full"
                aria-label="Chave de acesso"
                className="pr-9 font-mono"
                onKeyDown={(e) => {
                  if (e.key === "Enter" && !e.altKey && !e.repeat && nfe.chaveAcesso) {
                    e.preventDefault();
                    void acoes.copiarChave();
                  }
                }}
                value={chaveFormatada(nfe.chaveAcesso) || "Sem chave de acesso"}
              />
              {nfe.chaveAcesso && (
                <Button
                  type="button"
                  size="icon-xs"
                  variant="ghost"
                  title="Copiar chave de acesso"
                  aria-label="Copiar chave de acesso"
                  tabIndex={-1}
                  className="text-muted-foreground hover:text-foreground absolute top-1 right-1 opacity-50 hover:opacity-100"
                  onMouseDown={(e) => e.preventDefault()}
                  onClick={() => void acoes.copiarChave()}
                >
                  <Copy className="size-4" />
                </Button>
              )}
            </div>
          </Field>
          {nfe.statusNfe === "CANCELADA" && (
            <div className="mt-3 grid grid-cols-1 gap-3 md:grid-cols-3">
              <Dado rotulo="Cancelada em" valor={formatToLocal(nfe.dataCancelamento)} />
              <Dado className="md:col-span-2" rotulo="Justificativa" valor={nfe.justificativaCancelamento} />
            </div>
          )}
        </Bloco>

        <div className="grid gap-4 md:grid-cols-2">
          <Bloco titulo="Emitente">
            <Parte nome={nfe.emitenteNomeRazaosocial} doc={nfe.emitenteCpfCnpj} ie={nfe.emitenteRgIe} endereco={nfe.emitenteEndereco} bairro={nfe.emitenteBairro} tel={nfe.emitenteTelefone} />
          </Bloco>
          <Bloco titulo={entrada ? "Fornecedor" : "Destinatário"}>
            <Parte nome={nfe.clienteNomeRazaosocial} doc={nfe.clienteCpfCnpj} ie={nfe.clienteRgIe} endereco={nfe.clienteEndereco} bairro={nfe.clienteBairro} tel={nfe.clienteTelefone} />
          </Bloco>
        </div>

        <Bloco titulo="Itens">
          <div className="rounded-lg border bg-card overflow-clip">
          <Table pageScroll>
            <TableHeader>
              <TableRow>
                <TableHead>#</TableHead><TableHead>SKU</TableHead><TableHead>Descrição</TableHead>
                <TableHead className="text-right">Qtd.</TableHead><TableHead>Un.</TableHead>
                <TableHead className="text-right">Unitário</TableHead><TableHead className="text-right">Desconto</TableHead>
                <TableHead className="text-right">Total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {nfe.nfesItens.map((i, row) => {
                const campo = (coluna: number, rotulo: string, valor: string, direita = false) => (
                  <TableCell>
                    <Input
                      {...navigationCell({ grid: "nfe-itens", row, column: coluna })}
                      readOnly
                      inputSize="full"
                      aria-label={`${rotulo} do item ${i.numeroItem}`}
                      value={valor}
                      className={direita ? "text-right" : undefined}
                    />
                  </TableCell>
                );
                return (
                  <TableRow key={i.numeroItem}>
                    <TableCell>{i.numeroItem}</TableCell>
                    {campo(0, "SKU", i.sku.sku)}
                    {campo(1, "Descrição", i.descricaoItem)}
                    {campo(2, "Quantidade", Number(i.quantidade).toLocaleString("pt-BR"), true)}
                    {campo(3, "Unidade", i.unidadeMedida.sigla)}
                    {campo(4, "Unitário", brl(i.valorUnitario), true)}
                    {campo(5, "Desconto", brl(i.valorDesconto), true)}
                    {campo(6, "Total", brl(i.valorTotal), true)}
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
          </div>
        </Bloco>

        <div className="grid gap-4 md:grid-cols-2">
          <Bloco titulo="Transporte">
            {t ? (
              <div className="grid grid-cols-2 gap-3">
                <Dado className="col-span-2" rotulo="Modalidade do frete" valor={modalidadeFreteLabels[t.modalidadeFrete] ?? t.modalidadeFrete} />
                {t.transportadoraNomeRazaosocial && <Dado rotulo="Transportadora" valor={t.transportadoraNomeRazaosocial} />}
                {t.transportadoraCpfCnpj && <Dado rotulo="CPF/CNPJ" valor={t.transportadoraCpfCnpj} />}
                {t.veiculoPlaca && <Dado rotulo="Placa" valor={t.veiculoPlaca} />}
              </div>
            ) : (
              <Dado rotulo="Transporte" valor="Sem dados de transporte." />
            )}
          </Bloco>
          <Bloco titulo="Totais">
            <div className="grid grid-cols-2 gap-3">
              <Dado rotulo="Produtos" valor={brl(nfe.valorProdutos)} />
              <Dado rotulo="Desconto" valor={brl(nfe.valorDesconto)} />
              <Dado rotulo="Frete" valor={brl(nfe.valorFrete)} />
              <Dado rotulo="Seguro" valor={brl(nfe.valorSeguro)} />
              <Dado rotulo="Outras despesas" valor={brl(nfe.valorOutrasDespesas)} />
              <Dado rotulo="Total da nota" valor={brl(nfe.valorTotal)} />
            </div>
          </Bloco>
        </div>

        <Bloco titulo="Pagamentos">
          {nfe.nfesPagamentos.length === 0 && parcelasDaCompra.length > 0 ? (
            <GradeLeitura
              grade="nfe-pagamentos"
              onAbrir={(row) => void abrirConta(parcelasDaCompra[row].id)}
              rotuloAbrir="Abrir conta a pagar"
              colunas={["Parcela", "Vencimento", "Valor", "Situação"]}
              direita={[2]}
              linhas={parcelasDaCompra.map((p) => [
                `${p.numeroDocumento} · ${p.numeroParcela}/${p.totalParcelas}`,
                formatDateToLocal(p.dataVencimento),
                brl(p.valor),
                statusParcelaLabels[p.status] ?? p.status,
              ])}
            />
          ) : nfe.nfesPagamentos.length === 0 ? (
            <Dado rotulo="Pagamentos" valor={`Sem pagamentos informados${entrada ? " (NF-e de entrada)" : ""}.`} />
          ) : (
            <GradeLeitura
              grade="nfe-pagamentos"
              colunas={["Método", "Forma", "Valor"]}
              direita={[2]}
              linhas={nfe.nfesPagamentos.map((p) => [
                p.metodosPagamento.descricao,
                p.indicadorPagamento === "A_VISTA" ? "À vista" : "A prazo",
                brl(p.valorPagamento),
              ])}
            />
          )}
        </Bloco>
        {nfe.nfesInformacoesAdicionais?.informacoesComplementares && (
          <Bloco titulo="Informações complementares">
            <Textarea readOnly aria-label="Informações complementares" value={nfe.nfesInformacoesAdicionais.informacoesComplementares} />
          </Bloco>
        )}
      </div>
    </div>
  );
}
