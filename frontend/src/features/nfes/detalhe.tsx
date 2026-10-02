"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, Copy, Download, FileCode, Printer } from "lucide-react";
import { WindowActions } from "@/imperative-ui";
import { nfesApi } from "@/api/nfes";
import { TextoWindow, type TextoWindowProps } from "@/components/texto-window";
import {
  Button, Kbd, Spinner, Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/ui/primitives";
import { useWindow, useWindowCommands, useUi } from "@/ui/imperative";
import { formatToLocal } from "@/utils/date-utils";
import { CompraWindow, type CompraWindowProps } from "@/features/compras/upsert";
import { CancelarNfeWindow } from "./cancelar";
import { NfeStatusBadge } from "./status-badge";
import { brl, chaveFormatada, modalidadeFreteLabels, tipoLabels, type Nfe } from "./types";

export const abrirDanfe = (id: number) => {
  // Sem a feature "noopener" (retornaria sempre null) para poder detectar bloqueio de popup; o opener é zerado em seguida.
  const w = window.open(`/nfes/${id}/danfe`, "_blank");
  if (w) w.opener = null;
  return w;
};

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

const Parte = ({ nome, doc, ie, endereco, bairro, tel }: {
  nome: string; doc: string; ie: string | null; endereco: string | null; bairro: string | null; tel: string | null;
}) => (
  <div className="text-sm">
    <div className="font-medium">{nome}</div>
    <div className="text-muted-foreground">CPF/CNPJ: {doc}{ie ? ` · IE: ${ie}` : ""}</div>
    <div className="text-muted-foreground">{[endereco, bairro, tel].filter(Boolean).join(" · ")}</div>
  </div>
);

/** Detalhe da NF-e. Resolve com true quando algo mudou (cancelamento), para a lista recarregar. */
export function NfeDetalheWindow({ nfeId }: { nfeId: number }) {
  const ui = useUi();
  const queryClient = useQueryClient();
  const activeWindow = useWindow<true>();
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
          <Button variant="outline" onClick={() => activeWindow.dismiss("cancel")}>Fechar <Kbd>Esc</Kbd></Button>
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
  const abrirCompra = async () => {
    if (!nfe.compraId) return;
    await ui.windows.open<true, CompraWindowProps>({
      component: CompraWindow, props: { compraId: nfe.compraId }, title: `Compra #${nfe.compraId}`, size: "full",
    });
  };
  const fechar = () => (alterada ? activeWindow.resolve(true) : activeWindow.dismiss("cancel"));

  const imprimir = () => {
    if (!abrirDanfe(nfe.id))
      ui.feedback.notify({ type: "error", title: "O navegador bloqueou a janela do DANFE. Libere pop-ups para este site." });
  };

  useWindowCommands([
    { id: "nfe.xml", hotkey: "Alt+X", label: "Ver XML", run: (e) => { e.preventDefault(); void acoes.verXml(); } },
    { id: "nfe.baixar", hotkey: "Alt+B", label: "Baixar XML", run: (e) => { e.preventDefault(); void acoes.baixar(); } },
    { id: "nfe.imprimir", hotkey: "Alt+I", label: "Imprimir DANFE", run: (e) => { e.preventDefault(); imprimir(); } },
    { id: "nfe.cancelar", hotkey: "Alt+K", label: "Cancelar NF-e", enabled: podeCancelar, run: (e) => { e.preventDefault(); void acoes.cancelar(); } },
  ]);

  return (
    <div className="flex h-full min-h-0 flex-col gap-4">
      <WindowActions>
        <div className="mr-auto flex gap-2">
          <Button type="button" variant="outline" onClick={() => void acoes.verXml()}><FileCode /> Ver XML <Kbd>Alt+X</Kbd></Button>
          <Button type="button" variant="outline" onClick={() => void acoes.baixar()}><Download /> Baixar XML <Kbd>Alt+B</Kbd></Button>
          <Button type="button" variant="outline" onClick={imprimir}><Printer /> Imprimir DANFE <Kbd>Alt+I</Kbd></Button>
          <Button type="button" variant="destructive" disabled={!podeCancelar} title={podeCancelar ? undefined : motivo} onClick={() => void acoes.cancelar()}>
            <Ban /> Cancelar NF-e <Kbd>Alt+K</Kbd>
          </Button>
          {!podeCancelar && <span role="status" className="text-muted-foreground self-center text-xs">{motivo}</span>}
          {!podeCancelar && entrada && nfe.compraId && (
            <Button type="button" variant="link" className="self-center px-0 text-xs" onClick={() => void abrirCompra()}>
              Abrir compra #{nfe.compraId}
            </Button>
          )}
        </div>
        <Button type="button" variant="outline" onClick={fechar}>Fechar <Kbd>Esc</Kbd></Button>
      </WindowActions>

      <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-auto">
        <Bloco titulo={`NF-e ${nfe.numero} · série ${nfe.serie} · ${tipoLabels[nfe.tipoOperacao]}`}>
          <div className="flex flex-wrap items-center gap-3 text-sm">
            <NfeStatusBadge status={nfe.statusNfe} />
            <span>Emissão: {formatToLocal(nfe.dataEmissao)}</span>
            {nfe.vendaId && <span>Venda #{nfe.vendaId}</span>}
            {nfe.compraId && (
              <Button type="button" variant="link" className="h-auto p-0" onClick={() => void abrirCompra()}>
                Compra #{nfe.compraId}
              </Button>
            )}
          </div>
          <div className="mt-2 flex items-center gap-2">
            <code className="break-all rounded bg-muted px-2 py-1 text-xs">{chaveFormatada(nfe.chaveAcesso) || "Sem chave de acesso"}</code>
            {nfe.chaveAcesso && (
              <Button type="button" size="icon-sm" variant="outline" title="Copiar chave de acesso" onClick={() => void acoes.copiarChave()}>
                <Copy className="h-4 w-4" />
              </Button>
            )}
          </div>
          {nfe.statusNfe === "CANCELADA" && (
            <p className="mt-2 text-sm text-destructive">
              Cancelada em {formatToLocal(nfe.dataCancelamento)}: {nfe.justificativaCancelamento}
            </p>
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
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>#</TableHead><TableHead>SKU</TableHead><TableHead>Descrição</TableHead>
                <TableHead className="text-right">Qtd.</TableHead><TableHead>Un.</TableHead>
                <TableHead className="text-right">Unitário</TableHead><TableHead className="text-right">Desconto</TableHead>
                <TableHead className="text-right">Total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {nfe.nfesItens.map((i) => (
                <TableRow key={i.numeroItem}>
                  <TableCell>{i.numeroItem}</TableCell><TableCell>{i.sku.sku}</TableCell><TableCell>{i.descricaoItem}</TableCell>
                  <TableCell className="text-right">{Number(i.quantidade).toLocaleString("pt-BR")}</TableCell><TableCell>{i.unidadeMedida.sigla}</TableCell>
                  <TableCell className="text-right">{brl(i.valorUnitario)}</TableCell>
                  <TableCell className="text-right">{brl(i.valorDesconto)}</TableCell>
                  <TableCell className="text-right">{brl(i.valorTotal)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Bloco>

        <div className="grid gap-4 md:grid-cols-2">
          <Bloco titulo="Totais">
            <dl className="grid grid-cols-2 gap-1 text-sm">
              <dt>Produtos</dt><dd className="text-right">{brl(nfe.valorProdutos)}</dd>
              <dt>Desconto</dt><dd className="text-right">{brl(nfe.valorDesconto)}</dd>
              <dt>Frete</dt><dd className="text-right">{brl(nfe.valorFrete)}</dd>
              <dt>Seguro</dt><dd className="text-right">{brl(nfe.valorSeguro)}</dd>
              <dt>Outras despesas</dt><dd className="text-right">{brl(nfe.valorOutrasDespesas)}</dd>
              <dt className="font-semibold">Total da nota</dt><dd className="text-right font-semibold">{brl(nfe.valorTotal)}</dd>
            </dl>
          </Bloco>
          <div className="flex flex-col gap-4">
            <Bloco titulo="Pagamentos">
              {nfe.nfesPagamentos.length === 0 ? (
                <p className="text-sm text-muted-foreground">Sem pagamentos informados{entrada ? " (NF-e de entrada)" : ""}.</p>
              ) : (
                <ul className="text-sm">
                  {nfe.nfesPagamentos.map((p, i) => (
                    <li key={i} className="flex justify-between">
                      <span>{p.metodosPagamento.descricao} ({p.indicadorPagamento === "A_VISTA" ? "à vista" : "a prazo"})</span>
                      <span>{brl(p.valorPagamento)}</span>
                    </li>
                  ))}
                </ul>
              )}
            </Bloco>
            <Bloco titulo="Transporte">
              {t ? (
                <div className="text-sm">
                  <div>{modalidadeFreteLabels[t.modalidadeFrete] ?? t.modalidadeFrete}</div>
                  {t.transportadoraNomeRazaosocial && <div>{t.transportadoraNomeRazaosocial} {t.transportadoraCpfCnpj}</div>}
                  {t.veiculoPlaca && <div>Placa: {t.veiculoPlaca}</div>}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">Sem dados de transporte.</p>
              )}
            </Bloco>
          </div>
        </div>
        {nfe.nfesInformacoesAdicionais?.informacoesComplementares && (
          <Bloco titulo="Informações complementares">
            <p className="whitespace-pre-wrap text-sm">{nfe.nfesInformacoesAdicionais.informacoesComplementares}</p>
          </Bloco>
        )}
      </div>
    </div>
  );
}
