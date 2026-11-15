import type { Emitente } from "@/features/parceiros/emitentes/types";
import type { Fornecedor } from "@/features/parceiros/fornecedores/types";
import type { FornecedorFormValues } from "@/features/parceiros/fornecedores/types";
import type { TransportadoraFormValues } from "@/features/parceiros/transportadoras/types";
import type { Sku } from "@/features/catalogo/skus/types";

export interface Compra {
  id: number;
  dataCompra: string;
  valorNota: number;
  valorFrete: number;
  valorSeguro: number;
  valorOutrasDespesas: number;
  valorDesconto: number;
  valorFreteTerceiro: number;
  transportadoraId?: number | null;
  valorTotal: number;
  observacao?: string | null;
  estornada: boolean;
  estornadaEm?: string | null;
  motivoEstorno?: string | null;
  nfeId?: number | null;
  emitente: Emitente;
  fornecedor: Fornecedor;
  itens: Array<{
    id: number;
    sku: Sku;
    quantidade: number;
    valorUnitario: number;
    valorDesconto: number;
    valorTotal: number;
    valorRateio: number;
    custoUnitario: number;
    quantidadeDevolvida: number;
    saldoDevolvivel: number;
  }>;
}

export type StatusDevolucaoCompra = "ATIVA" | "ESTORNADA";

export interface DevolucaoCompraResumo {
  id: number;
  compraId: number;
  fornecedorNome: string;
  dataDevolucao: string;
  valorTotal: number;
  motivo: string;
  status: StatusDevolucaoCompra;
}

export interface DevolucaoCompra {
  id: number;
  status: StatusDevolucaoCompra;
  estornadoEm?: string | null;
  motivoEstorno?: string | null;
  compraId: number;
  dataDevolucao: string;
  motivo: string;
  valorTotal: number;
  /** NF-e de devolução emitida com a devolução e NF-e de estorno emitida no estorno; vazias se a compra não tinha NF-e ativa. */
  nfeDevolucao?: { nfeId: number; chaveAcesso: string } | null;
  nfeEstorno?: { nfeId: number; chaveAcesso: string } | null;
  itens: Array<{
    id: number;
    compraItemId: number;
    sku: string;
    quantidade: number;
    valorUnitario: number;
    custoUnitario: number;
    valorTotal: number;
  }>;
}

export interface CriarDevolucaoCompraValues {
  motivo: string;
  itens: Array<{ compraItemId: number; quantidade: number }>;
}

/** Quanto do item ainda pode voltar ao fornecedor: o saldo da compra, limitado ao que há em estoque. */
export const quantidadeDevolvivel = (item: Compra["itens"][number]) =>
  Math.max(0, Math.min(Number(item.saldoDevolvivel), Number(item.sku.estoque)));

/** Compra sem nada a devolver (saldo esgotado ou estornada). */
export const compraSemSaldoDevolvivel = (c: Compra) =>
  c.estornada || c.itens.every((i) => Number(i.saldoDevolvivel) <= 0);

export interface ParcelaCompra {
  numeroParcela: number;
  dataVencimento: string; // yyyy-MM-dd na tela; ISO no envio
  valorParcela: number;
}

export interface CriarCompraPayload {
  dataCompra: string;
  emitenteId: number;
  fornecedorId: number;
  valorFrete: number;
  valorSeguro: number;
  valorOutrasDespesas: number;
  valorDesconto: number;
  valorFreteTerceiro: number;
  transportadoraId: number | null;
  vencimentoFreteTerceiro: string | null;
  observacao: string | null;
  itens: Array<{
    sku: string;
    quantidade: number;
    valorUnitario: number;
    valorDesconto: number;
    codigoProdutoFornecedor: string | null;
  }>;
  condicaoPagamentoId: number | null;
  parcelas: Array<{
    numeroParcela: number;
    dataVencimento: string;
    valorParcela: number;
  }>;
}

export interface ImportarNfePayload {
  xml: string;
  emitenteId: number;
  itens: Array<{ numeroItem: number; sku: string }>;
  parcelas: CriarCompraPayload["parcelas"];
  condicaoPagamentoId: number | null;
  valorFreteTerceiro: number;
  vencimentoFreteTerceiro: string | null;
  transportadoraId: number | null;
  cadastrarTransportadora: boolean;
  /** Só para cadastros novos (inexistentes); gravados ao confirmar a compra. */
  fornecedorDados?: FornecedorFormValues;
  transportadoraDados?: TransportadoraFormValues;
  /** Dia de chegada (YYYY-MM-DD); omitida = hoje. */
  dataChegada?: string;
}

export interface ImportarNfeResultado {
  compraId: number;
  nfeId: number;
  chaveAcesso: string;
}

export interface NfeXmlParte {
  documento?: string | null;
  nome: string;
  fantasia?: string | null;
}

export interface PreviaNfe {
  dados: {
    chaveAcesso: string;
    numero: number;
    serie: number;
    dataEmissao: string;
    /** Dia de emissão no fuso do próprio XML (sem Z). */
    dataEmissaoLocal?: string;
    emitente: NfeXmlParte;
    destinatario?: NfeXmlParte | null;
    itens: Array<{
      numeroItem: number;
      codigoProduto: string;
      ean?: string | null;
      descricao: string;
      unidade: string;
      quantidade: number;
      valorUnitario: number;
      valorDesconto: number;
      valorProdutos: number;
    }>;
    totais: {
      produtos: number;
      desconto: number;
      frete: number;
      seguro: number;
      outras: number;
      nota: number;
    };
    modalidadeFrete?: number | null;
    transportadora?: NfeXmlParte | null;
    parcelas: Array<{ numero: number; vencimento: string; valor: number }>;
    informacoesComplementares?: string | null;
  };
  fornecedorId?: number | null;
  transportadoraId?: number | null;
  condicaoPagamentoId?: number | null;
  itens: Array<{
    numeroItem: number;
    status: "VINCULADO" | "SUGERIDO" | "PENDENTE";
    sku?: string | null;
    sugestaoSku?: string | null;
  }>;
  avisos: string[];
}

export const round2 = (n: number) =>
  Math.round((n + Number.EPSILON) * 100) / 100;

/** Desconto total da linha a partir do desconto por unidade (o backend grava o total da linha). */
export const descontoLinha = (quantidade: number, descontoUnitario: number) =>
  round2(quantidade * descontoUnitario);

/** Valor líquido do item: quantidade * (preço unitário - desconto unitário). */
export const valorLiquidoItem = (
  quantidade: number,
  valorUnitario: number,
  descontoUnitario: number,
) => round2(quantidade * valorUnitario - descontoLinha(quantidade, descontoUnitario));

/** Desconto por unidade a partir do desconto total gravado na linha. */
export const descontoUnitarioDe = (valorDesconto: number, quantidade: number) =>
  quantidade > 0 ? valorDesconto / quantidade : 0;

export const descontoUnitarioExcedePreco = (
  valorUnitario: number,
  descontoUnitario: number,
) => descontoUnitario > valorUnitario;

/** Espelha o rateio do backend: proporcional ao valor líquido do item, resíduo no maior item. */
export function ratearCompra(
  valoresItens: number[],
  despesasLiquidas: number,
): number[] {
  const total = valoresItens.reduce((s, v) => s + v, 0);
  if (total <= 0) return valoresItens.map(() => 0);
  // Maior resto, em centavos inteiros (espelha o RateioCompra do backend): cada item recebe o piso da sua
  // parte; os centavos que sobram vao um a um para os de maior resto (empate: maior valor, depois a ordem).
  const exatos = valoresItens.map((v) => (despesasLiquidas * 100 * v) / total);
  const pisos = exatos.map((e) => Math.floor(e + 1e-9));
  const sobra = Math.round(despesasLiquidas * 100) - pisos.reduce((s, p) => s + p, 0);
  const receberao = new Set(
    valoresItens
      .map((_, i) => i)
      .sort(
        (a, b) =>
          exatos[b] - pisos[b] - (exatos[a] - pisos[a]) ||
          valoresItens[b] - valoresItens[a] ||
          a - b,
      )
      .slice(0, Math.max(0, sobra)),
  );
  return pisos.map((p, i) => (p + (receberao.has(i) ? 1 : 0)) / 100);
}

export const moeda = (n: number) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(
    n,
  );
