import type { Emitente } from "@/features/parceiros/emitentes/types";
import type { Fornecedor } from "@/features/parceiros/fornecedores/types";
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
  valorTotal: number;
  observacao?: string | null;
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
  }>;
}

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

/** Espelha o rateio do backend: proporcional ao valor líquido do item, resíduo no maior item. */
export function ratearCompra(
  valoresItens: number[],
  despesasLiquidas: number,
): number[] {
  const total = valoresItens.reduce((s, v) => s + v, 0);
  if (total <= 0) return valoresItens.map(() => 0);
  const rateios = valoresItens.map((v) =>
    round2((despesasLiquidas * v) / total),
  );
  const residuo = round2(despesasLiquidas - rateios.reduce((s, r) => s + r, 0));
  if (residuo !== 0) {
    const maior = valoresItens.indexOf(Math.max(...valoresItens));
    rateios[maior] = round2(rateios[maior] + residuo);
  }
  return rateios;
}

export const moeda = (n: number) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(
    n,
  );
