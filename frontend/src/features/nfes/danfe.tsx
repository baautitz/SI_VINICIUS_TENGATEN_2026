"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { Printer } from "lucide-react";
import { nfesApi } from "@/api/nfes";
import { Button, Kbd, Spinner } from "@/ui/primitives";
import { useWindowCommands } from "@/ui/imperative";
import { formatToLocal } from "@/utils/date-utils";
import { brl, chaveFormatada, modalidadeFreteLabels, statusLabels } from "./types";

const Campo = ({ rotulo, children }: { rotulo: string; children: React.ReactNode }) => (
  <div className="border border-black p-1">
    <div className="text-[9px] uppercase">{rotulo}</div>
    <div className="text-xs font-medium">{children}</div>
  </div>
);

/**
 * Imprime só o DANFE: copia o documento para fora da janela (modal ou página), esconde o resto do body
 * durante a impressão e remove a cópia depois. Assim a caixa da janela (altura, rolagem, transform)
 * nunca recorta a folha.
 */
function imprimirDanfe() {
  const origem = document.getElementById("danfe");
  if (!origem) return;
  const copia = document.createElement("div");
  copia.id = "danfe-print";
  copia.innerHTML = origem.outerHTML.replace('id="danfe"', "");
  document.body.appendChild(copia);
  document.body.classList.add("imprimindo-danfe");
  const limpar = () => {
    document.body.classList.remove("imprimindo-danfe");
    copia.remove();
    window.removeEventListener("afterprint", limpar);
  };
  window.addEventListener("afterprint", limpar);
  window.print();
}

/** DANFE simplificado (sem valor fiscal). Impressão pelo navegador: o CSS @media print em globals.css esconde o resto. */
export function Danfe({ id }: { id: number }) {
  const { data: nfe, isLoading, error } = useQuery({ queryKey: ["nfes", id], queryFn: () => nfesApi.getById(id) });
  useWindowCommands([{ id: "danfe.imprimir", hotkey: "Alt+I", label: "Imprimir", run: (e) => { e.preventDefault(); imprimirDanfe(); } }]);

  if (isLoading) return <div className="flex justify-center py-10"><Spinner /></div>;
  if (error || !nfe) return <p className="p-6 text-destructive">Não foi possível carregar a NF-e para impressão.</p>;

  const t = nfe.nfesTransportes;
  return (
    <div className="p-4">
      <div className="mb-4 flex justify-end print:hidden">
        <Button onClick={imprimirDanfe}><Printer /> Imprimir <Kbd>Alt+I</Kbd></Button>
      </div>
      <div id="danfe" className="mx-auto max-w-[800px] bg-white p-3 text-black">
        <div className="border-2 border-black bg-yellow-200 p-2 text-center text-sm font-bold">
          SEM VALOR FISCAL - EMITIDO PARA TESTE LOCAL
        </div>
        {nfe.statusNfe === "CANCELADA" && (
          <div className="border-2 border-t-0 border-black p-2 text-center text-sm font-bold">
            NF-e CANCELADA{nfe.justificativaCancelamento ? `: ${nfe.justificativaCancelamento}` : ""}
          </div>
        )}
        <div className="grid grid-cols-3 gap-0">
          <Campo rotulo="Emitente">{nfe.emitenteNomeRazaosocial}<br />CNPJ/CPF: {nfe.emitenteCpfCnpj}<br />{nfe.emitenteEndereco} {nfe.emitenteBairro}</Campo>
          <Campo rotulo="DANFE">NF-e Nº {nfe.numero} · Série {nfe.serie}<br />{nfe.tipoOperacao === "ENTRADA" ? "0 - Entrada" : "1 - Saída"}<br />{statusLabels[nfe.statusNfe]}</Campo>
          <Campo rotulo="Emissão">{formatToLocal(nfe.dataEmissao)}</Campo>
        </div>
        <Campo rotulo="Chave de acesso"><span className="font-mono">{chaveFormatada(nfe.chaveAcesso) || "—"}</span></Campo>
        <Campo rotulo={nfe.tipoOperacao === "ENTRADA" ? "Fornecedor" : "Destinatário"}>
          {nfe.clienteNomeRazaosocial} · CNPJ/CPF: {nfe.clienteCpfCnpj}<br />{nfe.clienteEndereco} {nfe.clienteBairro}
        </Campo>
        <table className="mt-2 w-full border-collapse text-[11px]">
          <thead>
            <tr className="[&>th]:border [&>th]:border-black [&>th]:px-1 [&>th]:text-left">
              <th>Cód.</th><th>Descrição</th><th>Un.</th><th>Qtd.</th><th>Unit.</th><th>Desc.</th><th>Total</th>
            </tr>
          </thead>
          <tbody>
            {nfe.nfesItens.map((i) => (
              <tr key={i.numeroItem} className="[&>td]:border [&>td]:border-black [&>td]:px-1">
                <td>{i.sku.sku}</td><td>{i.descricaoItem}</td><td>{i.unidadeMedida.sigla}</td>
                <td>{Number(i.quantidade).toLocaleString("pt-BR")}</td><td>{brl(i.valorUnitario)}</td><td>{brl(i.valorDesconto)}</td><td>{brl(i.valorTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="mt-2 grid grid-cols-3">
          <Campo rotulo="Produtos">{brl(nfe.valorProdutos)}</Campo>
          <Campo rotulo="Desconto">{brl(nfe.valorDesconto)}</Campo>
          <Campo rotulo="Frete">{brl(nfe.valorFrete)}</Campo>
          <Campo rotulo="Seguro">{brl(nfe.valorSeguro)}</Campo>
          <Campo rotulo="Outras despesas">{brl(nfe.valorOutrasDespesas)}</Campo>
          <Campo rotulo="Total da nota"><b>{brl(nfe.valorTotal)}</b></Campo>
        </div>
        <Campo rotulo="Transporte">{t ? (modalidadeFreteLabels[t.modalidadeFrete] ?? t.modalidadeFrete) : "Sem dados de transporte"}</Campo>
        {nfe.nfesPagamentos.length > 0 && (
          <Campo rotulo="Pagamentos">
            {nfe.nfesPagamentos.map((p) => `${p.metodosPagamento.descricao}: ${brl(p.valorPagamento)}`).join(" · ")}
          </Campo>
        )}
        {nfe.nfesInformacoesAdicionais?.informacoesComplementares && (
          <Campo rotulo="Informações complementares">{nfe.nfesInformacoesAdicionais.informacoesComplementares}</Campo>
        )}
      </div>
    </div>
  );
}
