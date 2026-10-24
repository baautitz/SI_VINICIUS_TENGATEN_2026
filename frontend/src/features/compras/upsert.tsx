"use client";

import { combinacaoDe } from "@/lib/atalhos";
import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, RefreshCw, Trash2, TriangleAlert, Undo2 } from "lucide-react";
import { WindowActions, WindowCancelButton } from "@/ui/imperative";
import { fornecedoresApi, transportadorasApi } from "@/api/parceiros";
import { FornecedoresUpsert } from "@/features/parceiros/fornecedores/upsert";
import { TransportadorasUpsert } from "@/features/parceiros/transportadoras/upsert";
import type { FornecedorFormValues } from "@/features/parceiros/fornecedores/types";
import type { TransportadoraFormValues } from "@/features/parceiros/transportadoras/types";
import { CadastroSugeridoCard, valoresDoCadastroSugerido, erroDoCadastro } from "@/components/entity-inputs/cadastro-sugerido";
import { fornecedorSchema, type ItensSelecionados } from "@/features/parceiros/fornecedores/types";
import { transportadoraSchema } from "@/features/parceiros/transportadoras/types";
import { comprasApi } from "@/api/compras";
import { condicoesApi } from "@/api/financeiro";
import { SkuInput } from "@/components/entity-inputs/sku-input";
import { SkuViewInput } from "@/components/entity-inputs/sku-view-input";
import { EmitenteInput } from "@/components/entity-inputs/emitente-input";
import { FornecedorInput } from "@/components/entity-inputs/fornecedor-input";
import { TransportadoraInput } from "@/components/entity-inputs/transportadora-input";
import { CondicaoPagamentoInput } from "@/components/entity-inputs/condicao-pagamento-input";
import { getFullSkuName, type Sku } from "@/features/catalogo/skus/types";
import type { Emitente } from "@/features/parceiros/emitentes/types";
import type { Fornecedor } from "@/features/parceiros/fornecedores/types";
import type { CondicaoPagamento } from "@/features/financeiro/condicoes/types";
import {
  useUpsertMutation,
  type BackendResult,
} from "@/hooks/use-upsert-mutation";
import { useWindow, useWindowCommands, useUi } from "@/ui/imperative";
import { formatToLocal } from "@/utils/date-utils";
import { EstornarCompraWindow, type EstornarCompraWindowProps } from "./estornar";
import { Alert, AlertDescription, Badge, Button, Field, FieldError, FieldLabel, Input, HotkeyKbd, Spinner, Textarea } from "@/ui/primitives";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/ui/primitives";
import { DatePicker, NumberInput } from "@/ui/composites";
import { navigationCell } from "@/ui/keyboard-navigation";
import { todayLocalISODate, todayBrasiliaISODate, toLocalISODate } from "@/utils/date-utils";
import { cn } from "@/lib/utils";
import { ItensXml } from "./itens-xml";
import {
  moeda,
  ratearCompra,
  round2,
  type Compra,
  type ParcelaCompra,
  type PreviaNfe,
} from "./types";

export interface ImportacaoXml {
  xml: string;
  previa: PreviaNfe;
  emitente: Emitente;
}

export interface CompraWindowProps {
  /** Compra já gravada: abre em leitura. */
  compraId?: number | null;
  /** Entrada vinda da importação de XML. */
  importacao?: ImportacaoXml;
}

interface Linha {
  key: number;
  sku: string | null;
  produtoNome: string;
  /** Importação: item do XML. */
  numeroItem?: number;
  codigoProduto?: string | null;
  ean?: string | null;
  descricao?: string;
  skuOriginal?: string | null;
  sugestao?: string | null;
  quantidade: number;
  valorUnitario: number;
  valorDesconto: number;
  permiteDecimais: boolean;
  /** Leitura: valores gravados. */
  rateioFixo?: number;
  custoFixo?: number;
}

export function CompraWindow(props: CompraWindowProps) {
  const { data: compra, isLoading } = useQuery({
    queryKey: ["compras", "detail", props.compraId],
    queryFn: () => comprasApi.getById(props.compraId!),
    enabled: !!props.compraId,
    gcTime: 0,
  });

  if (props.compraId && (isLoading || !compra)) {
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );
  }
  return <CompraForm compra={compra ?? null} importacao={props.importacao} />;
}

let proximaChave = 1;
const ETAPAS = ["Fornecedor e transporte", "Dados gerais", "Produtos", "Custos e pagamento"];

function gerarParcelas(
  total: number,
  base: string,
  condicao: CondicaoPagamento | null,
): ParcelaCompra[] {
  if (total <= 0) return [];
  const data = new Date(base + "T12:00:00");
  const lista: ParcelaCompra[] = [];
  const add = (dias: number, valor: number) => {
    const v = new Date(data);
    v.setDate(data.getDate() + dias);
    lista.push({
      numeroParcela: lista.length + 1,
      dataVencimento: toLocalISODate(v),
      valorParcela: round2(valor),
    });
  };
  if (condicao) {
    if ((condicao.entradaMinimaPercentual ?? 0) > 0)
      add(0, total * (condicao.entradaMinimaPercentual / 100));
    (condicao.condicoesPagamentosParcelas ?? []).forEach((p) =>
      add(p.prazoDias, total * (p.percentual / 100)),
    );
  }
  if (lista.length === 0) add(0, total);
  const dif = round2(total - lista.reduce((s, p) => s + p.valorParcela, 0));
  lista[lista.length - 1].valorParcela = round2(
    lista[lista.length - 1].valorParcela + dif,
  );
  return lista;
}

function CompraForm({
  compra,
  importacao,
}: {
  compra: Compra | null;
  importacao?: ImportacaoXml;
}) {
  const activeWindow = useWindow<true>();
  const ui = useUi();
  const queryClient = useQueryClient();
  const readOnly = !!compra;
  const podeEstornar = !!compra && !compra.estornada;
  const previa = importacao?.previa;
  const dados = previa?.dados;
  const modoXml = !!importacao;

  const [dataCompra, setDataCompra] = React.useState(() =>
    compra ? compra.dataCompra.split("T")[0] : todayBrasiliaISODate(),
  );
  // Emissão do XML (somente leitura): limita a chegada por baixo; hoje (Brasília, como o backend) limita por cima.
  const hoje = todayBrasiliaISODate();
  const emissaoXml = dados
    ? (dados.dataEmissaoLocal ?? dados.dataEmissao).split("T")[0]
    : null;
  const dataBr = (d: string) => d.split("-").reverse().join("/");
  const erroEmissao =
    emissaoXml && emissaoXml > hoje
      ? `A emissão do XML (${dataBr(emissaoXml)}) é posterior a hoje: o XML está incorreto.`
      : null;
  const erroChegada = readOnly
    ? null
    : !/^\d{4}-\d{2}-\d{2}$/.test(dataCompra)
      ? "Informe uma data de chegada válida."
      : dataCompra > hoje
        ? "A chegada não pode ser posterior a hoje."
        : emissaoXml && dataCompra < emissaoXml
          ? `A chegada não pode ser anterior à emissão da NF-e (${dataBr(emissaoXml)}).`
          : null;
  const erroData = erroEmissao ?? erroChegada;
  // Troca a chegada: parcelas ainda geradas a partir da data antiga acompanham a nova; as editadas são mantidas com aviso.
  // Última chegada válida: valores parciais/vazios da digitação são ignorados na comparação com o padrão gerado.
  const chegadaValida = React.useRef(dataCompra);
  const mudarChegada = (v: string | null) => {
    const nova = v ?? "";
    if (/^\d{4}-\d{2}-\d{2}$/.test(nova)) {
      const anterior = chegadaValida.current;
      chegadaValida.current = nova;
      if (nova !== anterior && parcelas.length > 0 && valorNota > 0) {
        const padrao = JSON.stringify(gerarParcelas(valorNota, anterior, condicao));
        if (JSON.stringify(parcelas) === padrao)
          setParcelas(gerarParcelas(valorNota, nova, condicao));
        else if (!(dados && dados.parcelas.length > 0 && !divergeDoXml))
          ui.feedback.notify({
            type: "warning",
            title: "Parcelas mantidas: revise os vencimentos para a nova data de chegada.",
          });
      }
    }
    setDataCompra(nova);
  };
  const [emitente, setEmitente] = React.useState<Emitente | null>(
    compra?.emitente ?? importacao?.emitente ?? null,
  );
  const [fornecedor, setFornecedor] = React.useState<Fornecedor | null>(
    compra?.fornecedor ?? null,
  );
  const [fornecedorDados, setFornecedorDados] =
    React.useState<FornecedorFormValues>();
  const [fornecedorItens, setFornecedorItens] = React.useState<ItensSelecionados>();
  const [transportadoraItens, setTransportadoraItens] = React.useState<ItensSelecionados>();
  const [transportadoraDados, setTransportadoraDados] =
    React.useState<TransportadoraFormValues>();
  const { data: fornecedorXml } = useQuery({
    queryKey: ["fornecedores", "detail", previa?.fornecedorId],
    queryFn: () => fornecedoresApi.getById(previa!.fornecedorId!),
    enabled: !!previa?.fornecedorId,
  });
  const { data: transportadoraXml } = useQuery({
    queryKey: ["transportadoras", "detail", previa?.transportadoraId],
    queryFn: () => transportadorasApi.getById(previa!.transportadoraId!),
    enabled: !!previa?.transportadoraId,
  });
  // Visualização: a transportadora gravada na compra.
  const { data: transportadoraCompra } = useQuery({
    queryKey: ["transportadoras", "detail", compra?.transportadoraId],
    queryFn: () => transportadorasApi.getById(compra!.transportadoraId!),
    enabled: !!compra?.transportadoraId,
  });
  const [observacao, setObservacao] = React.useState(
    compra?.observacao ?? dados?.informacoesComplementares ?? "",
  );

  const [itens, setItens] = React.useState<Linha[]>(() => {
    if (compra)
      return compra.itens.map((i) => ({
        key: proximaChave++,
        sku: i.sku.sku,
        produtoNome: getFullSkuName(i.sku) || i.sku.sku,
        quantidade: Number(i.quantidade),
        valorUnitario: Number(i.valorUnitario),
        valorDesconto: Number(i.valorDesconto),
        permiteDecimais: true,
        rateioFixo: Number(i.valorRateio),
        custoFixo: Number(i.custoUnitario),
      }));
    if (dados && previa)
      return dados.itens.map((i) => {
        const p = previa.itens.find((x) => x.numeroItem === i.numeroItem);
        return {
          key: proximaChave++,
          sku: p?.sku ?? null,
          skuOriginal: p?.sku ?? null,
          sugestao: p?.sugestaoSku ?? null,
          produtoNome: i.descricao,
          descricao: `${i.descricao} (${i.codigoProduto}${i.ean ? ` · EAN ${i.ean}` : ""})`,
          numeroItem: i.numeroItem,
          codigoProduto: i.codigoProduto,
          ean: i.ean,
          quantidade: Number(i.quantidade),
          valorUnitario: Number(i.valorUnitario),
          valorDesconto: Number(i.valorDesconto),
          permiteDecimais: true,
        };
      });
    return [];
  });

  // Mesma regra do backend ao importar: o vDesc da nota já inclui o desconto dos itens, e a diferença
  // até o vNF (IPI, ST etc.) entra como despesa (ou desconto).
  const diferencaXml = dados
    ? dados.totais.nota -
      (dados.itens.reduce((s, i) => s + Number(i.valorProdutos) - Number(i.valorDesconto), 0) +
        dados.totais.frete + dados.totais.seguro + dados.totais.outras)
    : 0;
  const descontoItensXml = dados?.itens.reduce((s, i) => s + Number(i.valorDesconto), 0) ?? 0;

  const [frete, setFrete] = React.useState(
    compra?.valorFrete ?? dados?.totais.frete ?? 0,
  );
  const [seguro, setSeguro] = React.useState(
    compra?.valorSeguro ?? dados?.totais.seguro ?? 0,
  );
  const [outras, setOutras] = React.useState(
    compra?.valorOutrasDespesas ?? (dados ? round2(dados.totais.outras + Math.max(diferencaXml, 0)) : 0),
  );
  const [desconto, setDesconto] = React.useState(
    compra?.valorDesconto ??
      (dados ? round2(Math.max(0, dados.totais.desconto - descontoItensXml) + Math.max(-diferencaXml, 0)) : 0),
  );
  const [freteTerceiro, setFreteTerceiro] = React.useState(
    compra?.valorFreteTerceiro ?? 0,
  );
  const [transportadoraId, setTransportadoraId] = React.useState<number | null>(
    previa?.transportadoraId ?? null,
  );
  // Transportadora escolhida no campo: a etapa desmonta ao navegar, então o item é reidratado pelo id.
  const { data: transportadoraEscolhida } = useQuery({
    queryKey: ["transportadoras", "detail", transportadoraId],
    queryFn: () => transportadorasApi.getById(transportadoraId!),
    enabled: !!transportadoraId,
  });
  // Transportadora do XML segue o fornecedor: existente é reaproveitada, nova é cadastrada ao confirmar.
  // Cadastro existente editado pela janela: salvo direto na API, só sinaliza o badge.
  const [fornecedorEditadoNaApi, setFornecedorEditadoNaApi] = React.useState(false);
  const [transportadoraEditadaNaApi, setTransportadoraEditadaNaApi] = React.useState(false);
  const [cadastrarTransportadora, setCadastrarTransportadora] =
    React.useState(!!modoXml && !previa?.transportadoraId);
  const [vencFrete, setVencFrete] = React.useState(todayLocalISODate());
  const [condicaoEscolhida, setCondicao] = React.useState<CondicaoPagamento | null | undefined>(
    undefined,
  );
  // Condição vinda do fornecedor: as parcelas são geradas ao entrar na etapa de custos (valores já fechados).
  const [gerarDoFornecedor, setGerarParcelasDaCondicao] =
    React.useState(false);
  const aplicarCondicaoDoFornecedor = React.useCallback(
    async (id: number | null | undefined) => {
      if (!id) return;
      try {
        const c = await condicoesApi.getById(id);
        if (!c.ativo) return;
        setCondicao(c);
        setGerarParcelasDaCondicao(true);
      } catch {
        // sem condição padrão: o usuário escolhe na etapa de custos
      }
    },
    [],
  );
  // A condição (XML, cadastro novo editado ou fornecedor existente) sempre aparece; com duplicatas (cobr/dup) no XML elas é que geram as parcelas.
  const condicaoXmlId = dados
      ? (previa?.condicaoPagamentoId ?? fornecedorDados?.condicaoPagamentoId ?? fornecedorXml?.condicaoPagamento?.id)
      : null;
  const { data: condicaoXmlQuery } = useQuery({
    queryKey: ["condicoes-pagamentos", condicaoXmlId],
    queryFn: () => condicoesApi.getById(condicaoXmlId!),
    enabled: !!condicaoXmlId,
  });
  const condicaoXml = condicaoXmlQuery?.ativo ? condicaoXmlQuery : null;
  const condicao = condicaoEscolhida !== undefined ? condicaoEscolhida : condicaoXml;
  const gerarParcelasDaCondicao =
    gerarDoFornecedor || (!!condicaoXml && condicaoEscolhida === undefined && dados?.parcelas.length === 0);
  const [parcelas, setParcelas] = React.useState<ParcelaCompra[]>(() =>
    dados
      ? dados.parcelas.map((p, i) => ({
          numeroParcela: i + 1,
          dataVencimento: p.vencimento.split("T")[0],
          valorParcela: Number(p.valor),
        }))
      : [],
  );
  const [duplicatasXml] = React.useState(parcelas);
  const divergeDoXml =
    !!dados &&
    dados.parcelas.length > 0 &&
    JSON.stringify(parcelas) !== JSON.stringify(duplicatasXml);
  const [etapa, setEtapa] = React.useState(0);
  // Foco no primeiro campo da etapa: a identidade do callback só muda com a etapa.
  const focoEtapa = React.useCallback(
    (el: HTMLDivElement | null) => {
      if (el)
        requestAnimationFrame(() =>
          el
            .querySelector<HTMLElement>(
              "input:not([disabled]),button:not([disabled])",
            )
            ?.focus(),
        );
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [etapa],
  );
  const [skuInputKey, setSkuInputKey] = React.useState(0);
  const skuInputRef = React.useRef<HTMLInputElement>(null);
  const setSkuInput = React.useCallback(
    (el: HTMLInputElement | null) => {
      skuInputRef.current = el;
      if (el && skuInputKey > 0) requestAnimationFrame(() => el.focus());
    },
    [skuInputKey],
  );

  // Se o XML traz frete (modFrete=1) sem frete de terceiro, a previa avisa; o usuário informa aqui.
  const produtos = itens.reduce(
    (s, i) => s + round2(i.quantidade * i.valorUnitario - i.valorDesconto),
    0,
  );
  const valorNota = round2(produtos + frete + seguro + outras - desconto);
  const despesas = round2(frete + seguro + outras + freteTerceiro - desconto);
  const valoresItens = itens.map((i) =>
    round2(i.quantidade * i.valorUnitario - i.valorDesconto),
  );
  const rateios = ratearCompra(valoresItens, despesas);
  const somaParcelas = round2(parcelas.reduce((s, p) => s + p.valorParcela, 0));
  const parcelasDivergem =
    !readOnly &&
    parcelas.length > 0 &&
    Math.abs(somaParcelas - valorNota) > 0.01;
  const pendentes = itens.filter((i) => !i.sku).length;

  const { mutation } = useUpsertMutation<
    () => Promise<unknown>,
    BackendResult<unknown>
  >({
    mutationFn: (run) => run() as Promise<BackendResult<unknown>>,
    queryKey: [
      ["compras"],
      ["skus"],
      ["produtos"],
      ["movimentacoes"],
      ["contas-pagar"],
      ["fornecedores"],
      ["nfes"],
    ],
    onSuccessCallback: () => activeWindow.resolve(true),
  });

  const atualizar = (key: number, patch: Partial<Linha>) =>
    setItens((a) => a.map((i) => (i.key === key ? { ...i, ...patch } : i)));

  const adicionarSku = (sku: Sku | null, quantidade = 1) => {
    if (!sku) return;
    if (quantidade <= 0) {
      ui.feedback.notify({
        type: "error",
        title: "Quantidade deve ser maior que zero.",
      });
      return;
    }
    setItens((a) => [
      ...a,
      {
        key: proximaChave++,
        sku: sku.sku,
        produtoNome: getFullSkuName(sku),
        quantidade,
        valorUnitario: Number(sku.custoUltimaCompra) || 0,
        valorDesconto: 0,
        permiteDecimais: !!sku.produto?.unidadeMedida?.permiteDecimais,
      },
    ]);
    setSkuInputKey((k) => k + 1);
  };

  const removerItem = async (key: number) => {
    const ok = await ui.windows.confirm({
      title: "Remover Item?",
      description: "Deseja realmente remover este item da entrada?",
      confirmLabel: "Remover Item",
      confirmVariant: "destructive",
    });
    if (ok) setItens((a) => a.filter((i) => i.key !== key));
  };

  const recalcularParcelas = () => {
    if (valorNota <= 0) {
      ui.feedback.notify({
        type: "error",
        title: "Informe os itens antes de gerar as parcelas.",
      });
      return;
    }
    setParcelas(gerarParcelas(valorNota, dataCompra, condicao));
  };

  // Igual às parcelas de contas a pagar/receber: lança quantas precisar (o saldo restante, ou zero);
  // a divergência com a nota aparece nos próprios lançamentos e em "Redistribuir" (atalho `distribuir`).
  const adicionarParcela = () => {
    const base = new Date(dataCompra + "T12:00:00");
    base.setDate(base.getDate() + (parcelas.length + 1) * 30);
    setParcelas((p) => [
      ...p,
      {
        numeroParcela: p.length + 1,
        dataVencimento: toLocalISODate(base),
        valorParcela: Math.max(0, round2(valorNota - somaParcelas)),
      },
    ]);
    requestAnimationFrame(() =>
      document
        .querySelector<HTMLElement>(
          `[data-navigation-grid="compra-parcelas"][data-navigation-row="${parcelas.length}"][data-navigation-column="1"] input`,
        )
        ?.focus(),
    );
  };

  /** Divide o valor da nota igualmente entre as parcelas existentes (a última fecha a diferença). */
  const redistribuirParcelas = async () => {
    if (parcelas.length === 0 || valorNota <= 0) {
      ui.feedback.notify({
        type: "error",
        title: "Adicione ao menos uma parcela e informe os itens para redistribuir.",
      });
      return;
    }
    const ok = await ui.windows.confirm({
      title: "Redistribuir parcelas?",
      description: `O valor da nota (${moeda(valorNota)}) será dividido igualmente entre as ${parcelas.length} parcela(s); os vencimentos são mantidos.`,
      confirmLabel: "Redistribuir",
      cancelLabel: "Cancelar",
    });
    if (!ok) return;
    const parte = round2(valorNota / parcelas.length);
    setParcelas((a) =>
      a.map((x, i) => ({
        ...x,
        valorParcela:
          i === a.length - 1 ? round2(valorNota - parte * (a.length - 1)) : parte,
      })),
    );
  };

  const iso = (d: string) => new Date(d + "T12:00:00").toISOString();

  const erroFornecedorNovo =
    modoXml && dados && !previa?.fornecedorId
      ? erroDoCadastro(fornecedorSchema, dados.emitente, fornecedorDados)
      : null;
  const erroTransportadoraNova =
    modoXml && dados?.transportadora && !previa?.transportadoraId && cadastrarTransportadora
      ? erroDoCadastro(transportadoraSchema, dados.transportadora, transportadoraDados)
      : null;

  const validar = (): string | null => {
    if (erroFornecedorNovo) return `Fornecedor: ${erroFornecedorNovo}`;
    if (erroTransportadoraNova) return `Transportadora: ${erroTransportadoraNova}`;
    if (!emitente) return "Emitente é obrigatório.";
    if (erroData) return erroData;
    if (!modoXml && !fornecedor) return "Fornecedor é obrigatório.";
    if (itens.length === 0) return "A entrada deve conter ao menos um item.";
    if (pendentes > 0)
      return `Há ${pendentes} item(ns) sem SKU. Escolha o SKU de cada item.`;
    if (itens.some((i) => i.quantidade <= 0 || i.valorUnitario <= 0))
      return "Quantidade e valor unitário devem ser maiores que zero.";
    if (
      freteTerceiro > 0 &&
      !transportadoraId &&
      !(modoXml && cadastrarTransportadora)
    )
      return "Frete de terceiro exige a transportadora (etapa Fornecedor e transporte).";
    if (parcelas.length === 0)
      return "Informe ao menos uma parcela (use Recalcular parcelas).";
    if (parcelas.some((p) => !(p.valorParcela > 0)))
      return "O valor de cada parcela deve ser maior que zero.";
    return null;
  };

  const confirmar = async () => {
    if (readOnly || mutation.isPending) return;
    const erro = validar();
    if (erro) {
      ui.feedback.notify({ type: "error", title: erro });
      return;
    }
    if (parcelasDivergem) {
      ui.feedback.notify({
        type: "error",
        title: `Parcelas somam ${moeda(somaParcelas)} e a nota vale ${moeda(valorNota)}. Ajuste as parcelas.`,
      });
      return;
    }
    const ok = await ui.windows.confirm({
      title: modoXml ? "Confirmar importação?" : "Fechar compra?",
      description: `Total ${moeda(valorNota)} em ${parcelas.length} parcela(s). Esta ação grava a entrada e movimenta o estoque.`,
      confirmLabel: modoXml ? "Confirmar importação" : "Fechar compra",
    });
    if (!ok) return;
    const parcelasPayload = parcelas.map((p) => ({
      ...p,
      dataVencimento: iso(p.dataVencimento),
    }));
    const venc = freteTerceiro > 0 ? iso(vencFrete) : null;
    try {
      await mutation.mutateAsync(() =>
        modoXml
          ? comprasApi.importarNfe({
              xml: importacao!.xml,
              emitenteId: emitente!.id,
              itens: itens.map((i) => ({
                numeroItem: i.numeroItem!,
                sku: i.sku!,
              })),
              parcelas: parcelasPayload,
              condicaoPagamentoId: condicao?.id ?? null,
              valorFreteTerceiro: freteTerceiro,
              vencimentoFreteTerceiro: venc,
              transportadoraId,
              cadastrarTransportadora,
              fornecedorDados: previa?.fornecedorId ? undefined : fornecedorDados,
              transportadoraDados:
                !previa?.transportadoraId && cadastrarTransportadora
                  ? transportadoraDados
                  : undefined,
              dataChegada: dataCompra,
            })
          : comprasApi.create({
              // Meio-dia UTC (como a importação): o dia não desloca em nenhum fuso do Brasil.
              dataCompra: `${dataCompra}T12:00:00.000Z`,
              emitenteId: emitente!.id,
              fornecedorId: fornecedor!.id,
              valorFrete: frete,
              valorSeguro: seguro,
              valorOutrasDespesas: outras,
              valorDesconto: desconto,
              valorFreteTerceiro: freteTerceiro,
              transportadoraId,
              vencimentoFreteTerceiro: venc,
              observacao: observacao.trim() || null,
              itens: itens.map((i) => ({
                sku: i.sku!,
                quantidade: i.quantidade,
                valorUnitario: i.valorUnitario,
                valorDesconto: i.valorDesconto,
                codigoProdutoFornecedor: i.codigoProduto ?? null,
              })),
              condicaoPagamentoId: condicao?.id ?? null,
              parcelas: parcelasPayload,
            }),
      );
    } catch {
      // O hook central já mostra o erro operacional em um toast.
    }
  };

  const bloqueioEtapa = (): string | null => {
    if (readOnly) return null;
    if (etapa === 0 && !modoXml && !fornecedor)
      return "Selecione o fornecedor.";
    if (etapa === 0 && erroFornecedorNovo) return `Fornecedor: ${erroFornecedorNovo}`;
    if (etapa === 0 && erroTransportadoraNova) return `Transportadora: ${erroTransportadoraNova}`;
    if (etapa === 1 && !emitente) return "Emitente é obrigatório.";
    if (etapa === 1 && erroData) return erroData;
    if (etapa === 2) {
      if (itens.length === 0) return "Adicione ao menos um item.";
      if (pendentes > 0)
        return `Há ${pendentes} item(ns) sem SKU. Escolha o SKU de cada item.`;
    }
    if (etapa === 3 && parcelasDivergem)
      return `Parcelas somam ${moeda(somaParcelas)} e a nota vale ${moeda(valorNota)} (diferença ${moeda(round2(valorNota - somaParcelas))}).`;
    return null;
  };
  const avancar = () => {
    if (etapa === 3) return void confirmar();
    const b = bloqueioEtapa();
    if (b) {
      ui.feedback.notify({ type: "error", title: b });
      if (etapa === 2 && pendentes > 0)
        document
          .querySelector<HTMLElement>("[data-sem-sku] input,[data-sem-sku] button")
          ?.focus();
    } else {
      if (etapa === 2 && gerarParcelasDaCondicao) {
        setParcelas(gerarParcelas(valorNota, dataCompra, condicao));
        setGerarParcelasDaCondicao(false);
      }
      setEtapa(etapa + 1);
    }
  };

  const estornar = async () => {
    if (!compra || !podeEstornar) return;
    const r = await ui.windows.open<true, EstornarCompraWindowProps>({
      component: EstornarCompraWindow,
      props: { compraId: compra.id, temNfe: !!compra.nfeId },
      title: `Estornar compra #${compra.id}`,
      size: "small",
    });
    if (r.status !== "confirmed") return;
    await Promise.all(
      ["compras", "skus", "produtos", "movimentacoes", "contas-pagar", "nfes"].map((k) =>
        queryClient.invalidateQueries({ queryKey: [k] }),
      ),
    );
    ui.feedback.notify({ type: "success", title: `Compra #${compra.id} estornada.` });
    activeWindow.dismiss("cancel");
  };

  useWindowCommands([
    {
      id: "compras.estornar",
      hotkey: combinacaoDe("estornar"),
      label: "Estornar compra",
      enabled: podeEstornar,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        void estornar();
      },
    },
    {
      id: "compras.focus-sku",
      hotkey: combinacaoDe("adicionarSku"),
      label: "Adicionar SKU",
      enabled: !readOnly && !modoXml && etapa === 2,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        skuInputRef.current?.focus();
      },
    },
    {
      id: "compras.back",
      hotkey: combinacaoDe("etapaAnterior"),
      label: "Voltar etapa",
      enabled: etapa > 0 && !mutation.isPending,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        setEtapa((v) => Math.max(0, v - 1));
      },
    },
    {
      id: "compras.next",
      hotkey: combinacaoDe("etapaSeguinte"),
      label: "Avançar etapa",
      enabled: etapa < 3 && !mutation.isPending,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        avancar();
      },
    },
    {
      id: "compras.confirmar",
      hotkey: combinacaoDe("confirmar"),
      label: "Confirmar",
      enabled: etapa === 3 && !mutation.isPending,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        avancar();
      },
    },
    {
      id: "compras.redistribuir-parcelas",
      hotkey: combinacaoDe("distribuir"),
      label: "Redistribuir parcelas",
      enabled: !readOnly && etapa === 3,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        void redistribuirParcelas();
      },
    },
    {
      id: "compras.add-parcela",
      hotkey: combinacaoDe("adicionarParcela"),
      label: "Adicionar parcela",
      enabled: !readOnly && etapa === 3,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        adicionarParcela();
      },
    },
    {
      id: "compras.recalcular-parcelas",
      hotkey: combinacaoDe("gerar"),
      label: "Gerar parcelas",
      enabled: !readOnly && etapa === 3,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        recalcularParcelas();
      },
    },
  ]);

  const registerDirty = () => {
    activeWindow.setDirtyCheck(
      () => !readOnly && (modoXml || itens.length > 0),
    );
    return () => activeWindow.setDirtyCheck(null);
  };

  /** Valor calculado: input somente leitura, para o foco e as setas percorrerem a tela até o fim. */
  const valorFixo = (valor: number, decimals: number, rotulo: string, className?: string) => (
    <NumberInput
      value={valor}
      decimals={decimals}
      inputSize="full"
      inputMode="decimal"
      aria-label={rotulo}
      readOnly
      className={cn("h-8 text-right text-xs font-semibold", className)}
      onNumberChange={() => {}}
    />
  );

  const despesaCampo = (
    label: string,
    valor: number,
    set: (n: number) => void,
    desabilitado = false,
  ) => (
    <Field>
      <FieldLabel htmlFor={`despesa-${label}`}>{label}</FieldLabel>
      <NumberInput
        id={`despesa-${label}`}
        value={valor}
        decimals={2}
        inputSize="full"
        inputMode="decimal"
        className="h-8 text-right text-xs font-semibold"
        readOnly={readOnly || desabilitado}
        onNumberChange={(n) => set(Number.isNaN(n) ? 0 : n)}
      />
    </Field>
  );

  return (
    <div className="flex h-full min-h-0 flex-col gap-4">
      {compra?.estornada && (
        <Alert variant="destructive" role="status">
          <AlertDescription>
            <Badge variant="destructive" className="mr-2">
              Estornada
            </Badge>
            em {formatToLocal(compra.estornadaEm)}: {compra.motivoEstorno}
          </AlertDescription>
        </Alert>
      )}

      <WindowActions>
        {podeEstornar && (
          <Button
            type="button"
            variant="destructive"
            className="mr-auto"
            onClick={() => void estornar()}
          >
            <Undo2 /> Estornar compra <HotkeyKbd hotkey={combinacaoDe("estornar")} />
          </Button>
        )}
        <WindowCancelButton readOnly={readOnly} />
        {etapa > 0 && (
          <Button
            type="button"
            variant="outline"
            onClick={() => setEtapa(etapa - 1)}
          >
            Voltar{" "}
            <HotkeyKbd hotkey={combinacaoDe("etapaAnterior")} />
          </Button>
        )}
        {etapa < 3 ? (
          <Button type="button" onClick={avancar}>
            Avançar{" "}
            <HotkeyKbd hotkey={combinacaoDe("etapaSeguinte")} />
          </Button>
        ) : (
          !readOnly && (
            <Button
              type="button"
              onClick={() => void confirmar()}
              disabled={mutation.isPending || parcelasDivergem}
            >
              {mutation.isPending
                ? "Gravando..."
                : modoXml
                  ? "Confirmar importação"
                  : "Fechar compra"}
              <HotkeyKbd hotkey={combinacaoDe("confirmar")} />
            </Button>
          )
        )}
      </WindowActions>

      <ol className="flex gap-2 text-xs">
        {ETAPAS.map((nome, i) => (
          <li key={nome} className="flex-1">
            <button
              type="button"
              disabled={i > etapa}
              aria-current={i === etapa ? "step" : undefined}
              onClick={() => setEtapa(i)}
              className={cn(
                "w-full rounded-md border px-3 py-1.5 text-left font-medium",
                i === etapa
                  ? "border-primary bg-primary/10 text-primary"
                  : i < etapa
                    ? "hover:bg-muted"
                    : "text-muted-foreground",
              )}
            >
              {i + 1}. {nome}
            </button>
          </li>
        ))}
      </ol>

      <div ref={registerDirty} className="flex min-h-0 flex-1 flex-col">
        <div
          key={etapa}
          ref={focoEtapa}
          className="flex min-h-0 flex-1 flex-col gap-4 -mx-1 overflow-y-auto px-1"
        >
          {etapa === 0 && (
            <>
              {modoXml ? (
                <CadastroSugeridoCard
                  titulo="Fornecedor do XML"
                  nome={fornecedorXml?.nomeRazaosocial ?? fornecedorDados?.nomeRazaosocial ?? dados!.emitente.nome}
                  documento={fornecedorXml?.cpfCnpj ?? fornecedorDados?.cpfCnpj ?? dados!.emitente.documento}
                  existente={!!previa?.fornecedorId}
                  editado={!!fornecedorDados || fornecedorEditadoNaApi}
                  erro={erroFornecedorNovo}
                  onEditar={async () => {
                    if (previa?.fornecedorId) {
                      // Existente: salva direto na API; a query do cadastro é invalidada pelo upsert.
                      if (fornecedorXml) {
                        const r = await ui.windows.open({
                          component: FornecedoresUpsert,
                          props: { editingItem: fornecedorXml },
                          title: "Editar Fornecedor",
                          size: "full",
                        });
                        if (r.status === "confirmed") setFornecedorEditadoNaApi(true);
                      }
                      return;
                    }
                    const r = await ui.windows.open<true | FornecedorFormValues, React.ComponentProps<typeof FornecedoresUpsert>>({
                      component: FornecedoresUpsert,
                      props: {
                        editingItem: null,
                        controlado: true,
                        valoresIniciais: fornecedorDados ?? valoresDoCadastroSugerido(dados!.emitente),
                        itensIniciais: fornecedorItens,
                        onItens: setFornecedorItens,
                      },
                      title: "Editar Fornecedor",
                      size: "full",
                    });
                    if (r.status === "confirmed" && r.value !== true) setFornecedorDados(r.value);
                  }}
                />
              ) : (
                <FornecedorInput
                  name="compra-fornecedor"
                  initialItem={fornecedor}
                  onSelectItem={(f) => {
                    setFornecedor(f);
                    void aplicarCondicaoDoFornecedor(f?.condicaoPagamento?.id);
                  }}
                  onSelectId={() => {}}
                  disabled={readOnly}
                />
              )}
              {readOnly && (
                <TransportadoraInput
                  key={transportadoraCompra?.id ?? "sem"}
                  name="compra-transportadora"
                  label="Transportadora do frete"
                  initialItem={transportadoraCompra ?? null}
                  onSelectId={() => {}}
                  disabled
                />
              )}
              {!readOnly && (
                <div className="w-full">
                      {!(modoXml && dados?.transportadora) && (
                        <TransportadoraInput
                          key={transportadoraEscolhida?.id ?? "sem"}
                          name="compra-transportadora"
                          initialItem={transportadoraEscolhida ?? null}
                          onSelectId={setTransportadoraId}
                          label="Transportadora do frete"
                        />
                      )}
                      {modoXml && dados?.transportadora && (
                        <div>
                          <CadastroSugeridoCard
                            titulo="Transportadora do XML"
                            nome={transportadoraXml?.nomeRazaosocial ?? transportadoraDados?.nomeRazaosocial ?? dados.transportadora.nome}
                            documento={transportadoraXml?.cpfCnpj ?? transportadoraDados?.cpfCnpj ?? dados.transportadora.documento}
                            existente={!!previa?.transportadoraId}
                            editado={!!transportadoraDados || transportadoraEditadaNaApi}
                            erro={erroTransportadoraNova}
                            onEditar={async () => {
                              if (previa?.transportadoraId) {
                                if (transportadoraXml) {
                                  const r = await ui.windows.open({
                                    component: TransportadorasUpsert,
                                    props: { editingItem: transportadoraXml },
                                    title: "Editar Transportadora",
                                    size: "full",
                                  });
                                  if (r.status === "confirmed") setTransportadoraEditadaNaApi(true);
                                }
                                return;
                              }
                              const r = await ui.windows.open<true | TransportadoraFormValues, React.ComponentProps<typeof TransportadorasUpsert>>({
                                component: TransportadorasUpsert,
                                props: {
                                  editingItem: null,
                                  controlado: true,
                                  valoresIniciais: transportadoraDados ?? valoresDoCadastroSugerido(dados.transportadora!),
                                  itensIniciais: transportadoraItens,
                                  onItens: setTransportadoraItens,
                                },
                                title: "Editar Transportadora",
                                size: "full",
                              });
                              if (r.status === "confirmed" && r.value !== true) {
                                setTransportadoraDados(r.value);
                                setCadastrarTransportadora(true);
                              }
                            }}
                          />
                        </div>
                      )}
                </div>
              )}
            </>
          )}
          {etapa === 1 && (
            <>
              {modoXml && dados && previa && (
                <div className="flex flex-col gap-2 rounded-lg border p-3 text-sm">
                  <div className="flex flex-wrap items-center gap-x-6 gap-y-1">
                    <span>
                      <b>NF-e</b> {dados.numero} / série {dados.serie}
                    </span>
                    <span className="font-mono text-xs">
                      {dados.chaveAcesso}
                    </span>
                    <span>
                      <b>Fornecedor:</b> {dados.emitente.nome} (
                      {dados.emitente.documento})
                      {!previa.fornecedorId && (
                        <Badge variant="secondary" className="ml-2">
                          Será cadastrado
                        </Badge>
                      )}
                    </span>
                  </div>
                  {previa.avisos.map((a) => (
                    <Alert key={a}>
                      <TriangleAlert />
                      <AlertDescription>{a}</AlertDescription>
                    </Alert>
                  ))}
                </div>
              )}

              <div className="flex w-full flex-row gap-2">
                <div className="w-[20%]">
                  <Field>
                    <FieldLabel>Data da Compra</FieldLabel>
                    <DatePicker
                      value={dataCompra}
                      onChange={mudarChegada}
                      readOnly={readOnly}
                      max={hoje}
                      min={emissaoXml}
                      invalid={!!erroChegada}
                    />
                    {erroChegada && <FieldError>{erroChegada}</FieldError>}
                  </Field>
                </div>
                <div className="w-[80%]">
                  <EmitenteInput
                    name="compra-emitente"
                    initialItem={emitente}
                    onSelectItem={setEmitente}
                    onSelectId={() => {}}
                    disabled={readOnly || modoXml}
                  />
                </div>
              </div>

              {emissaoXml && (
                <div className="flex flex-col gap-1 text-sm">
                  <Field>
                    <FieldLabel htmlFor="compra-emissao-nfe">Emissão da NF-e</FieldLabel>
                    <Input
                      id="compra-emissao-nfe"
                      readOnly
                      value={dataBr(emissaoXml)}
                      className="h-8"
                    />
                  </Field>
                  {erroEmissao && (
                    <Alert variant="destructive">
                      <TriangleAlert />
                      <AlertDescription>{erroEmissao}</AlertDescription>
                    </Alert>
                  )}
                </div>
              )}
              {dados?.destinatario && (
                <p className="text-muted-foreground text-xs">
                  Destinatário no XML: {dados.destinatario.nome} (
                  {dados.destinatario.documento})
                </p>
              )}
              <Field>
                <FieldLabel>Observação</FieldLabel>
                <Textarea
                  value={observacao}
                  maxLength={500}
                  rows={3}
                  readOnly={readOnly}
                  className=""
                  onChange={(e) => setObservacao(e.target.value)}
                />
              </Field>
            </>
          )}
          {etapa === 2 && (
            <>
              {!readOnly && !modoXml && (
                <SkuInput
                  ref={setSkuInput}
                  key={skuInputKey}
                  name="compra-add-sku"
                  label="Adicionar Produto"
                  onSelectSku={adicionarSku}
                />
              )}

              {modoXml ? (
                <ItensXml
                  itens={itens}
                  onVincular={(key, sku, permiteDecimais) =>
                    atualizar(key, { sku, ...(permiteDecimais === undefined ? {} : { permiteDecimais }) })
                  }
                />
              ) : (
                <>
              <div className="rounded-lg border bg-card overflow-clip">
                <Table pageScroll className="min-w-250" aria-label="Itens da compra">
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-10">#</TableHead>
                      <TableHead className="w-48">
                        SKU
                      </TableHead>
                      <TableHead>Produto</TableHead>
                      <TableHead className="w-28 text-right">
                        Quantidade
                      </TableHead>
                      <TableHead className="w-28 text-right">
                        Valor unit.
                      </TableHead>
                      <TableHead className="w-24 text-right">
                        Desconto
                      </TableHead>
                      <TableHead className="w-24 text-right">
                        Rateio
                      </TableHead>
                      <TableHead className="w-28 text-right">
                        Custo unit. final
                      </TableHead>
                      <TableHead className="w-24 text-right">
                        Total
                      </TableHead>
                      {!readOnly && !modoXml && <TableHead className="w-12" />}
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {itens.length === 0 && (
                      <TableRow>
                        <TableCell
                          colSpan={readOnly || modoXml ? 9 : 10}
                          className="text-muted-foreground py-8 text-center"
                        >
                          Nenhum item. Digite um SKU acima e pressione Enter
                          para adicionar.
                        </TableCell>
                      </TableRow>
                    )}
                    {itens.map((item, idx) => {
                      const rateio = item.rateioFixo ?? rateios[idx];
                      const custo =
                        item.custoFixo ??
                        (item.quantidade > 0
                          ? (valoresItens[idx] + rateio) / item.quantidade
                          : 0);
                      const status = item.sku
                        ? item.sku === item.skuOriginal
                          ? "VINCULADO"
                          : "ESCOLHIDO"
                        : item.sugestao
                          ? "SUGERIDO"
                          : "PENDENTE";
                      const numCell = (
                        col: number,
                        valor: number,
                        onChange: (n: number) => void,
                        dec: number,
                        off = false,
                      ) => (
                        <NumberInput
                          {...navigationCell({
                            grid: "compra-itens",
                            row: idx,
                            column: col,
                          })}
                          value={valor}
                          decimals={dec}
                          inputSize="full"
                          inputMode="decimal"
                          className="text-right"
                          readOnly={readOnly || modoXml || off}
                          onNumberChange={(n) =>
                            onChange(Number.isNaN(n) ? 0 : n)
                          }
                        />
                      );
                      return (
                        <TableRow
                          key={item.key}
                          data-sem-sku={!item.sku && !readOnly ? "" : undefined}
                          className={!item.sku && !readOnly ? "bg-destructive/10" : undefined}
                        >
                          <TableCell>
                            {item.numeroItem ?? idx + 1}
                          </TableCell>
                          <TableCell>
                            {item.sku ? (
                              <SkuViewInput name={`compra-sku-${idx}`} sku={item.sku} />
                            ) : null}
                          </TableCell>
                          <TableCell>
                            <div className="flex flex-col gap-1">
                              <span>{item.descricao ?? item.produtoNome}</span>
                              {modoXml && (
                                <Badge
                                  variant={
                                    status === "PENDENTE"
                                      ? "destructive"
                                      : status === "SUGERIDO"
                                        ? "secondary"
                                        : "outline"
                                  }
                                  className="w-fit"
                                >
                                  {status}
                                </Badge>
                              )}
                            </div>
                          </TableCell>
                          <TableCell>
                            {numCell(
                              1,
                              item.quantidade,
                              (n) => atualizar(item.key, { quantidade: n }),
                              item.permiteDecimais ? 4 : 0,
                            )}
                          </TableCell>
                          <TableCell>
                            {numCell(
                              2,
                              item.valorUnitario,
                              (n) => atualizar(item.key, { valorUnitario: n }),
                              4,
                            )}
                          </TableCell>
                          <TableCell>
                            {numCell(
                              3,
                              item.valorDesconto,
                              (n) => atualizar(item.key, { valorDesconto: n }),
                              2,
                            )}
                          </TableCell>
                          <TableCell className="text-right">
                            {moeda(rateio)}
                          </TableCell>
                          <TableCell className="text-right font-bold">
                            {new Intl.NumberFormat("pt-BR", {
                              style: "currency",
                              currency: "BRL",
                              minimumFractionDigits: 4,
                            }).format(custo)}
                          </TableCell>
                          <TableCell className="text-right font-bold">
                            {moeda(valoresItens[idx])}
                          </TableCell>
                          {!readOnly && !modoXml && (
                            <TableCell>
                              <Button
                                type="button"
                                size="icon-sm"
 variant="ghost"
 title="Remover item"
 aria-label="Remover"
                                onClick={() => void removerItem(item.key)}
                              >
                                <Trash2 />
                              </Button>
                            </TableCell>
                          )}
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </div>
                </>
              )}
            </>
          )}
          {etapa === 3 && (
            <div className="flex flex-col gap-4">
                <section className="flex flex-col gap-3 rounded-lg border p-4">
                  <h3 className="text-sm font-semibold">Despesas da nota</h3>
                  <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
                    {despesaCampo("Frete (na nota)", frete, setFrete, modoXml)}
                    {despesaCampo("Seguro", seguro, setSeguro, modoXml)}
                    {despesaCampo("Outras despesas", outras, setOutras, modoXml)}
                    {despesaCampo("Desconto geral", desconto, setDesconto, modoXml)}
                    {despesaCampo("Frete de terceiro", freteTerceiro, setFreteTerceiro)}
                  </div>
                  {freteTerceiro > 0 && !readOnly && (
                    <Field className="md:w-1/3">
                      <FieldLabel>Vencimento do frete de terceiro</FieldLabel>
                      <DatePicker
                        value={vencFrete}
                        onChange={(v) => setVencFrete(v || todayLocalISODate())}
                      />
                    </Field>
                  )}
                </section>

                {!readOnly && (
                  <section className="flex flex-col gap-3 rounded-lg border p-4">
                    <h3 className="text-sm font-semibold">Pagamento</h3>
                    <div className="flex items-end gap-2">
                      <div className="min-w-0 flex-1">
                        <CondicaoPagamentoInput
                          name="compra-condicao"
                          initialItem={condicao}
                          onSelectItem={(c) => {
                            setCondicao(c);
                            if (c && valorNota > 0)
                              setParcelas(gerarParcelas(valorNota, dataCompra, c));
                          }}
                          onSelectId={() => {}}
                        />
                      </div>
                      <Button
                        type="button"
                        variant="outline"
                        className="h-8"
                        title="Refaz as parcelas a partir da condição e do valor da nota, descartando as atuais"
                        onClick={recalcularParcelas}
                      >
                        <RefreshCw /> Gerar parcelas <HotkeyKbd hotkey={combinacaoDe("gerar")} />
                      </Button>
                    </div>
                    <p className="text-muted-foreground text-xs">
                      {dados && dados.parcelas.length > 0 && !divergeDoXml
                        ? "Parcelas vindas das duplicatas (cobr/dup) do XML. Gerar parcelas refaz pela condição escolhida."
                        : "Gerar parcelas divide o valor da nota conforme a condição escolhida e descarta as parcelas atuais."}
                    </p>

                    <div className="rounded-lg border bg-card overflow-clip">
                      <Table pageScroll aria-label="Parcelas">
                        <TableHeader>
                          <TableRow>
                            <TableHead className="w-20">Parcela</TableHead>
                            <TableHead>Vencimento</TableHead>
                            <TableHead className="w-40 text-right">Valor</TableHead>
                            <TableHead className="w-12" />
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {parcelas.length === 0 && (
                            <TableRow>
                              <TableCell colSpan={4} className="text-muted-foreground py-6 text-center">
                                Nenhuma parcela. Escolha uma condição e use Gerar parcelas, ou adicione manualmente.
                              </TableCell>
                            </TableRow>
                          )}
                          {parcelas.map((p, i) => (
                            <TableRow key={i}>
                              <TableCell className="font-semibold">{p.numeroParcela}</TableCell>
                              <TableCell>
                                <div
                                  className="max-w-48"
                                  {...navigationCell({ grid: "compra-parcelas", row: i, column: 0 })}
                                >
                                  <DatePicker
                                    value={p.dataVencimento}
                                    className="h-8"
                                    onChange={(v) =>
                                      setParcelas((a) =>
                                        a.map((x, j) =>
                                          j === i ? { ...x, dataVencimento: v || dataCompra } : x,
                                        ),
                                      )
                                    }
                                  />
                                </div>
                              </TableCell>
                              <TableCell>
                                <div {...navigationCell({ grid: "compra-parcelas", row: i, column: 1 })}>
                                  <NumberInput
                                    value={p.valorParcela}
                                    decimals={2}
                                    inputSize="full"
                                    inputMode="decimal"
                                    className={cn(
                                      "text-right",
                                      (!(p.valorParcela > 0) || parcelasDivergem) && "border-destructive",
                                    )}
                                    aria-invalid={!(p.valorParcela > 0) || parcelasDivergem || undefined}
                                    title={
                                      !(p.valorParcela > 0)
                                        ? "O valor da parcela deve ser maior que zero."
                                        : parcelasDivergem
                                          ? `A soma das parcelas diverge do valor da nota. Ajuste ou use Redistribuir (${combinacaoDe("distribuir")}).`
                                          : undefined
                                    }
                                    onNumberChange={(n) =>
                                      setParcelas((a) =>
                                        a.map((x, j) =>
                                          j === i
                                            ? { ...x, valorParcela: Number.isNaN(n) ? 0 : n }
                                            : x,
                                        ),
                                      )
                                    }
                                  />
                                </div>
                              </TableCell>
                              <TableCell>
                                <Button
                                  type="button"
                                  size="icon-sm"
 variant="ghost"
 title="Remover parcela"
 aria-label="Remover"
                                  {...navigationCell({ grid: "compra-parcelas", row: i, column: 2 })}
                                  onClick={() =>
                                    setParcelas((a) =>
                                      a
                                        .filter((_, j) => j !== i)
                                        .map((x, j) => ({ ...x, numeroParcela: j + 1 })),
                                    )
                                  }
                                >
                                  <Trash2 />
                                </Button>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                      <div className="flex items-center justify-between gap-3 border-t px-3 py-2">
                        <div className="flex items-center gap-2">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={adicionarParcela}
                        >
                          <Plus /> Parcela <HotkeyKbd hotkey={combinacaoDe("adicionarParcela")} />
                        </Button>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => void redistribuirParcelas()}
                        >
                          Redistribuir <HotkeyKbd hotkey={combinacaoDe("distribuir")} />
                        </Button>
                        </div>
                        <span
                          className={cn(
                            "text-xs font-semibold",
                            parcelasDivergem ? "text-destructive" : "text-muted-foreground",
                          )}
                        >
                          {parcelasDivergem
                            ? `Parcelas ${moeda(somaParcelas)} · nota ${moeda(valorNota)} · diferença ${moeda(round2(valorNota - somaParcelas))}`
                            : `Parcelas ${moeda(somaParcelas)} = valor da nota`}
                        </span>
                      </div>
                    </div>

                    {divergeDoXml && (
                      <Alert>
                        <TriangleAlert />
                        <AlertDescription>
                          As parcelas foram alteradas e divergem das duplicatas (cobr/dup) do XML da NF-e.
                        </AlertDescription>
                      </Alert>
                    )}
                  </section>
                )}

                <section className="flex flex-col gap-2 rounded-lg border p-4">
                  <h3 className="text-sm font-semibold">Custo por item</h3>
                  <div className="rounded-lg border bg-card overflow-clip">
                    <Table pageScroll aria-label="Custo por item">
                      <TableHeader>
                        <TableRow>
                          <TableHead>Item</TableHead>
                          <TableHead className="w-40 text-right">Rateio</TableHead>
                          <TableHead className="w-40 text-right">Custo unit. final</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {itens.map((it, i) => (
                          <TableRow key={it.key}>
                            <TableCell className="max-w-0 truncate">
                              {it.sku} - {it.produtoNome}
                            </TableCell>
                            <TableCell>
                              <div {...navigationCell({ grid: "compra-custos", row: i, column: 0 })}>
                                {valorFixo(it.rateioFixo ?? rateios[i], 2, `Rateio de ${it.produtoNome}`)}
                              </div>
                            </TableCell>
                            <TableCell>
                              <div {...navigationCell({ grid: "compra-custos", row: i, column: 1 })}>
                                {valorFixo(
                                  it.custoFixo ??
                                    (it.quantidade > 0 ? (valoresItens[i] + rateios[i]) / it.quantidade : 0),
                                  4,
                                  `Custo unitário final de ${it.produtoNome}`,
                                )}
                              </div>
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </div>
                </section>

                <section className="grid grid-cols-2 gap-3 rounded-lg border p-4 md:grid-cols-4">
                  {[
                    ["Produtos", produtos],
                    ["Despesas líquidas (rateadas)", despesas],
                    ["Valor da nota (fornecedor)", valorNota],
                    ["Custo total (c/ frete de terceiro)", round2(valorNota + freteTerceiro)],
                  ].map(([rotulo, v]) => (
                    <Field key={rotulo as string}>
                      <FieldLabel>{rotulo}</FieldLabel>
                      {valorFixo(v as number, 2, rotulo as string, "text-sm")}
                    </Field>
                  ))}
                </section>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
