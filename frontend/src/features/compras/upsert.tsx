"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, RefreshCw, Trash2, TriangleAlert, Undo2 } from "lucide-react";
import { WindowActions } from "@/imperative-ui";
import { fornecedoresApi, transportadorasApi } from "@/api/parceiros";
import { FornecedoresUpsert } from "@/features/parceiros/fornecedores/upsert";
import { TransportadorasUpsert } from "@/features/parceiros/transportadoras/upsert";
import type { FornecedorFormValues } from "@/features/parceiros/fornecedores/types";
import type { TransportadoraFormValues } from "@/features/parceiros/transportadoras/types";
import { ParteXmlCard, valoresDaParteXml } from "./parte-xml";
import { comprasApi } from "@/api/compras";
import { condicoesApi } from "@/api/financeiro";
import { SkuInput } from "@/components/entity-inputs/sku-input";
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
import {
  Alert,
  AlertDescription,
  Badge,
  Button,
  Checkbox,
  Field,
  FieldError,
  FieldLabel,
  Kbd,
  KbdGroup,
  Spinner,
  Textarea,
} from "@/ui/primitives";
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
import { SkuPicker } from "./sku-picker";
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
const ETAPAS = ["Fornecedor", "Dados gerais", "Produtos", "Custos e pagamento"];

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
  const [cadastrarTransportadora, setCadastrarTransportadora] =
    React.useState(false);
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
  // XML com parcelas (cobr/dup) tem prioridade sobre a condição do fornecedor; sem elas, a condição do XML é derivada da query.
  const condicaoXmlId = dados && dados.parcelas.length === 0 ? previa?.condicaoPagamentoId : null;
  const { data: condicaoXmlQuery } = useQuery({
    queryKey: ["condicoes-pagamentos", condicaoXmlId],
    queryFn: () => condicoesApi.getById(condicaoXmlId!),
    enabled: !!condicaoXmlId,
  });
  const condicaoXml = condicaoXmlQuery?.ativo ? condicaoXmlQuery : null;
  const condicao = condicaoEscolhida !== undefined ? condicaoEscolhida : condicaoXml;
  const gerarParcelasDaCondicao = gerarDoFornecedor || (!!condicaoXml && condicaoEscolhida === undefined);
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

  const adicionarParcela = () => {
    if (round2(valorNota - somaParcelas) <= 0) {
      ui.feedback.notify({
        type: "error",
        title: "Não há saldo para uma nova parcela: ajuste as parcelas existentes.",
      });
      return;
    }
    setParcelas((p) => [
      ...p,
      {
        numeroParcela: p.length + 1,
        dataVencimento: dataCompra,
        valorParcela: round2(valorNota - somaParcelas),
      },
    ]);
  };

  const iso = (d: string) => new Date(d + "T12:00:00").toISOString();

  const validar = (): string | null => {
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
      return "Frete de terceiro exige a transportadora.";
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
          .querySelector<HTMLInputElement>("[data-sem-sku] input")
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
      hotkey: "Alt+Z" as const,
      label: "Estornar compra",
      enabled: podeEstornar,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        void estornar();
      },
    },
    {
      id: "compras.next",
      hotkey: "Alt+Enter" as const,
      label: "Avançar / confirmar",
      enabled: !mutation.isPending,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        avancar();
      },
    },
    {
      id: "compras.add-parcela",
      hotkey: "Alt+A" as const,
      label: "Adicionar parcela",
      enabled: !readOnly && etapa === 3,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        adicionarParcela();
      },
    },
    {
      id: "compras.recalcular-parcelas",
      hotkey: "Alt+G" as const,
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
        disabled={readOnly || desabilitado}
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
              ESTORNADA
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
            <Undo2 /> Estornar compra <Kbd>Alt+Z</Kbd>
          </Button>
        )}
        <Button
          type="button"
          variant="outline"
          onClick={() => activeWindow.dismiss("cancel")}
        >
          {readOnly ? "Fechar" : "Cancelar"} <Kbd>Esc</Kbd>
        </Button>
        {etapa > 0 && (
          <Button
            type="button"
            variant="outline"
            onClick={() => setEtapa(etapa - 1)}
          >
            Voltar
          </Button>
        )}
        {!readOnly && bloqueioEtapa() && (
          <span role="status" className="text-destructive self-center text-xs">
            {bloqueioEtapa()}
          </span>
        )}
        {etapa < 3 ? (
          <Button type="button" onClick={avancar}>
            Avançar{" "}
            <KbdGroup>
              <Kbd>Alt</Kbd>
              <Kbd>Enter</Kbd>
            </KbdGroup>
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
              <KbdGroup>
                <Kbd>Alt</Kbd>
                <Kbd>Enter</Kbd>
              </KbdGroup>
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
          className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto pr-1"
        >
          {etapa === 0 && (
            <>
              {modoXml ? (
                <ParteXmlCard
                  titulo="Fornecedor do XML"
                  nome={fornecedorXml?.nomeRazaosocial ?? fornecedorDados?.nomeRazaosocial ?? dados!.emitente.nome}
                  documento={fornecedorXml?.cpfCnpj ?? fornecedorDados?.cpfCnpj ?? dados!.emitente.documento}
                  existente={!!previa?.fornecedorId}
                  editado={!!fornecedorDados}
                  onEditar={async () => {
                    if (previa?.fornecedorId) {
                      // Existente: salva direto na API; a query do cadastro é invalidada pelo upsert.
                      if (fornecedorXml)
                        await ui.windows.open({
                          component: FornecedoresUpsert,
                          props: { editingItem: fornecedorXml },
                          title: "Editar Fornecedor",
                          size: "full",
                        });
                      return;
                    }
                    const r = await ui.windows.open<true | FornecedorFormValues, React.ComponentProps<typeof FornecedoresUpsert>>({
                      component: FornecedoresUpsert,
                      props: {
                        editingItem: null,
                        controlado: true,
                        valoresIniciais: fornecedorDados ?? valoresDaParteXml(dados!.emitente),
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
                      disabled={readOnly}
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
                  <span>
                    <b>Emissão da NF-e:</b> {dataBr(emissaoXml)}
                  </span>
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
                  disabled={readOnly}
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

              <div className="rounded-lg border">
                <Table className="min-w-250" aria-label="Itens da compra">
                  <TableHeader className="bg-muted">
                    <TableRow className="hover:bg-transparent">
                      <TableHead className="w-10 px-2">#</TableHead>
                      <TableHead className="w-48 px-2">
                        SKU {!readOnly && <Kbd>Enter</Kbd>}
                      </TableHead>
                      <TableHead className="px-2">Produto</TableHead>
                      <TableHead className="w-28 px-2 text-right">
                        Quantidade
                      </TableHead>
                      <TableHead className="w-28 px-2 text-right">
                        Valor unit.
                      </TableHead>
                      <TableHead className="w-24 px-2 text-right">
                        Desconto
                      </TableHead>
                      <TableHead className="w-24 px-2 text-right">
                        Rateio
                      </TableHead>
                      <TableHead className="w-28 px-2 text-right">
                        Custo unit. final
                      </TableHead>
                      <TableHead className="w-24 px-2 text-right">
                        Total
                      </TableHead>
                      {!readOnly && !modoXml && <TableHead className="w-10" />}
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {itens.length === 0 && (
                      <TableRow>
                        <TableCell
                          colSpan={10}
                          className="text-muted-foreground py-8 text-center text-sm"
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
                          className="h-7 text-right text-xs font-semibold"
                          disabled={readOnly || modoXml || off}
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
                          <TableCell className="px-2 text-xs">
                            {item.numeroItem ?? idx + 1}
                          </TableCell>
                          <TableCell className="px-2 py-1">
                            {readOnly ? (
                              <span className="font-mono text-xs">
                                {item.sku}
                              </span>
                            ) : (
                              <div className="flex flex-col gap-1">
                                <SkuPicker
                                  value={item.sku}
                                  inputProps={navigationCell({
                                    grid: "compra-itens",
                                    row: idx,
                                    column: 0,
                                  })}
                                  onPick={(s) =>
                                    atualizar(item.key, {
                                      sku: s.sku,
                                      permiteDecimais:
                                        !!s.produto?.unidadeMedida
                                          ?.permiteDecimais,
                                      ...(modoXml
                                        ? {}
                                        : { produtoNome: getFullSkuName(s) }),
                                    })
                                  }
                                />
                                {!item.sku && item.sugestao && (
                                  <Button
                                    type="button"
                                    size="xs"
                                    variant="outline"
                                    onClick={() =>
                                      atualizar(item.key, {
                                        sku: item.sugestao!,
                                      })
                                    }
                                  >
                                    Usar {item.sugestao}
                                  </Button>
                                )}
                              </div>
                            )}
                          </TableCell>
                          <TableCell className="px-2 py-1 text-xs">
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
                          <TableCell className="px-2 py-1">
                            {numCell(
                              1,
                              item.quantidade,
                              (n) => atualizar(item.key, { quantidade: n }),
                              item.permiteDecimais ? 4 : 0,
                            )}
                          </TableCell>
                          <TableCell className="px-2 py-1">
                            {numCell(
                              2,
                              item.valorUnitario,
                              (n) => atualizar(item.key, { valorUnitario: n }),
                              4,
                            )}
                          </TableCell>
                          <TableCell className="px-2 py-1">
                            {numCell(
                              3,
                              item.valorDesconto,
                              (n) => atualizar(item.key, { valorDesconto: n }),
                              2,
                            )}
                          </TableCell>
                          <TableCell className="px-2 text-right text-xs">
                            {moeda(rateio)}
                          </TableCell>
                          <TableCell className="px-2 text-right text-xs font-bold">
                            {new Intl.NumberFormat("pt-BR", {
                              style: "currency",
                              currency: "BRL",
                              minimumFractionDigits: 4,
                            }).format(custo)}
                          </TableCell>
                          <TableCell className="px-2 text-right text-xs font-bold">
                            {moeda(valoresItens[idx])}
                          </TableCell>
                          {!readOnly && !modoXml && (
                            <TableCell className="px-1">
                              <Button
                                type="button"
                                size="icon-xs"
                                variant="ghost"
                                className="text-red-500"
                                title="Remover item"
                                onClick={() => void removerItem(item.key)}
                              >
                                <Trash2 className="size-3.5" />
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
          {etapa === 3 && (
            <>
              <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
                {despesaCampo("Frete (na nota)", frete, setFrete, modoXml)}
                {despesaCampo("Seguro", seguro, setSeguro, modoXml)}
                {despesaCampo("Outras despesas", outras, setOutras, modoXml)}
                {despesaCampo("Desconto geral", desconto, setDesconto, modoXml)}
                {despesaCampo(
                  "Frete de terceiro",
                  freteTerceiro,
                  setFreteTerceiro,
                )}
              </div>

              {(freteTerceiro > 0 ||
                (modoXml && previa?.dados.modalidadeFrete === 1)) &&
                !readOnly && (
                  <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                    <div>
                      <TransportadoraInput
                        name="compra-transportadora"
                        initialItem={null}
                        onSelectId={setTransportadoraId}
                        label="Transportadora do frete"
                      />
                      {modoXml && dados?.transportadora && (
                        <div className="mt-2">
                          <ParteXmlCard
                            titulo="Transportadora do XML"
                            nome={transportadoraXml?.nomeRazaosocial ?? transportadoraDados?.nomeRazaosocial ?? dados.transportadora.nome}
                            documento={transportadoraXml?.cpfCnpj ?? transportadoraDados?.cpfCnpj ?? dados.transportadora.documento}
                            existente={!!previa?.transportadoraId}
                            editado={!!transportadoraDados}
                            onEditar={async () => {
                              if (previa?.transportadoraId) {
                                if (transportadoraXml)
                                  await ui.windows.open({
                                    component: TransportadorasUpsert,
                                    props: { editingItem: transportadoraXml },
                                    title: "Editar Transportadora",
                                    size: "full",
                                  });
                                return;
                              }
                              const r = await ui.windows.open<true | TransportadoraFormValues, React.ComponentProps<typeof TransportadorasUpsert>>({
                                component: TransportadorasUpsert,
                                props: {
                                  editingItem: null,
                                  controlado: true,
                                  valoresIniciais: transportadoraDados ?? valoresDaParteXml(dados.transportadora!),
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
                      {modoXml &&
                        dados?.transportadora &&
                        !previa?.transportadoraId && (
                          <label className="mt-2 flex items-center gap-2 text-xs">
                            <Checkbox
                              checked={cadastrarTransportadora}
                              onCheckedChange={(c) =>
                                setCadastrarTransportadora(c === true)
                              }
                            />
                            Cadastrar a transportadora do XML (
                            {dados.transportadora.nome})
                          </label>
                        )}
                    </div>
                    <Field>
                      <FieldLabel>Vencimento do frete de terceiro</FieldLabel>
                      <DatePicker
                        value={vencFrete}
                        onChange={(v) => setVencFrete(v || todayLocalISODate())}
                      />
                    </Field>
                  </div>
                )}

              {!readOnly && (
                <div className="flex flex-col gap-2 rounded-lg border p-3">
                  <div className="flex flex-wrap items-end gap-3">
                    <div className="w-72">
                      <CondicaoPagamentoInput
                        name="compra-condicao"
                        initialItem={condicao}
                        onSelectItem={(c) => {
                          setCondicao(c);
                          if (c && valorNota > 0)
                            setParcelas(
                              gerarParcelas(valorNota, dataCompra, c),
                            );
                        }}
                        onSelectId={() => {}}
                      />
                    </div>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={recalcularParcelas}
                    >
                      <RefreshCw /> Recalcular parcelas <Kbd>Alt+G</Kbd>
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={adicionarParcela}
                    >
                      <Plus /> Parcela <Kbd>Alt+A</Kbd>
                    </Button>
                  </div>
                  {parcelas.length === 0 && (
                    <p className="text-muted-foreground text-xs">
                      Nenhuma parcela. Escolha uma condição ou use Recalcular
                      parcelas.
                    </p>
                  )}
                  {parcelas.map((p, i) => (
                    <div key={i} className="flex items-center gap-2">
                      <span className="w-16 text-xs font-semibold">
                        Parcela {p.numeroParcela}
                      </span>
                      <div className="w-40">
                        <DatePicker
                          value={p.dataVencimento}
                          onChange={(v) =>
                            setParcelas((a) =>
                              a.map((x, j) =>
                                j === i
                                  ? { ...x, dataVencimento: v || dataCompra }
                                  : x,
                              ),
                            )
                          }
                        />
                      </div>
                      <div className="w-36">
                        <NumberInput
                          value={p.valorParcela}
                          decimals={2}
                          inputSize="full"
                          inputMode="decimal"
                          className="h-8 text-right text-xs font-semibold"
                          onNumberChange={(n) =>
                            setParcelas((a) =>
                              a.map((x, j) =>
                                j === i
                                  ? {
                                      ...x,
                                      valorParcela: Number.isNaN(n) ? 0 : n,
                                    }
                                  : x,
                              ),
                            )
                          }
                        />
                        {!(p.valorParcela > 0) && (
                          <FieldError>Informe um valor maior que zero.</FieldError>
                        )}
                      </div>
                      <Button
                        type="button"
                        size="icon-xs"
                        variant="ghost"
                        className="text-red-500"
                        title="Remover parcela"
                        onClick={() =>
                          setParcelas((a) =>
                            a
                              .filter((_, j) => j !== i)
                              .map((x, j) => ({ ...x, numeroParcela: j + 1 })),
                          )
                        }
                      >
                        <Trash2 className="size-3.5" />
                      </Button>
                    </div>
                  ))}
                  {divergeDoXml && (
                    <Alert>
                      <TriangleAlert />
                      <AlertDescription>
                        As parcelas foram alteradas e divergem das duplicatas
                        (cobr/dup) do XML da NF-e.
                      </AlertDescription>
                    </Alert>
                  )}
                  {parcelasDivergem && (
                    <Alert variant="destructive">
                      <TriangleAlert />
                      <AlertDescription>
                        Soma das parcelas {moeda(somaParcelas)} x valor da nota{" "}
                        {moeda(valorNota)}: diferença de{" "}
                        {moeda(round2(valorNota - somaParcelas))}. Ajuste para
                        fechar a compra.
                      </AlertDescription>
                    </Alert>
                  )}
                </div>
              )}

              <div className="rounded-lg border text-xs">
                <div className="bg-muted grid grid-cols-4 px-3 py-1 font-semibold">
                  <span className="col-span-2">Item</span>
                  <span className="text-right">Rateio</span>
                  <span className="text-right">Custo unit. final</span>
                </div>
                {itens.map((it, i) => (
                  <div
                    key={it.key}
                    className="grid grid-cols-4 border-t px-3 py-1"
                  >
                    <span className="col-span-2 truncate">
                      {it.sku} - {it.produtoNome}
                    </span>
                    <span className="text-right">
                      {moeda(it.rateioFixo ?? rateios[i])}
                    </span>
                    <span className="text-right font-bold">
                      {(
                        it.custoFixo ??
                        (it.quantidade > 0
                          ? (valoresItens[i] + rateios[i]) / it.quantidade
                          : 0)
                      ).toLocaleString("pt-BR", {
                        style: "currency",
                        currency: "BRL",
                        minimumFractionDigits: 4,
                      })}
                    </span>
                  </div>
                ))}
              </div>

              <div
                className={cn(
                  "grid grid-cols-2 divide-x rounded-lg border md:grid-cols-4",
                )}
              >
                {[
                  ["Produtos", produtos],
                  ["Despesas líquidas (rateadas)", despesas],
                  ["Valor da nota (fornecedor)", valorNota],
                  [
                    "Custo total (c/ frete de terceiro)",
                    round2(valorNota + freteTerceiro),
                  ],
                ].map(([rotulo, v]) => (
                  <div
                    key={rotulo as string}
                    className="flex flex-col px-4 py-2 text-end"
                  >
                    <span className="text-muted-foreground text-xs font-semibold uppercase">
                      {rotulo}
                    </span>
                    <span className="text-lg font-bold">
                      {moeda(v as number)}
                    </span>
                  </div>
                ))}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
