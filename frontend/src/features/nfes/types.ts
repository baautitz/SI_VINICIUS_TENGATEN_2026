export type TipoOperacaoNfe = "ENTRADA" | "SAIDA";
export type StatusNfe = "RASCUNHO" | "EMITIDA" | "CANCELADA";

export interface NfeResumo {
  id: number;
  chaveAcesso: string | null;
  tipoOperacao: TipoOperacaoNfe;
  numero: number;
  serie: number;
  dataEmissao: string;
  valorTotal: number;
  statusNfe: StatusNfe;
}

// Na NF-e de entrada, os campos cliente* guardam o fornecedor (snapshot).
export interface Nfe extends Omit<NfeResumo, "chaveAcesso"> {
  chaveAcesso: string | null;
  dataSaida: string | null;
  emitenteNomeRazaosocial: string;
  emitenteCpfCnpj: string;
  emitenteRgIe: string | null;
  emitenteApelidoNomefantasia: string | null;
  emitenteEndereco: string | null;
  emitenteBairro: string | null;
  emitenteTelefone: string | null;
  clienteNomeRazaosocial: string;
  clienteCpfCnpj: string;
  clienteRgIe: string | null;
  clienteEndereco: string | null;
  clienteBairro: string | null;
  clienteTelefone: string | null;
  vendaId: number | null;
  compraId: number | null;
  justificativaCancelamento: string | null;
  dataCancelamento: string | null;
  valorProdutos: number;
  valorDesconto: number;
  valorFrete: number;
  valorSeguro: number;
  valorOutrasDespesas: number;
  nfesItens: Array<{
    numeroItem: number;
    descricaoItem: string;
    quantidade: number;
    valorUnitario: number;
    valorDesconto: number;
    valorTotal: number;
    sku: { sku: string };
    unidadeMedida: { sigla: string };
  }>;
  nfesPagamentos: Array<{
    indicadorPagamento: "A_VISTA" | "A_PRAZO";
    valorPagamento: number;
    metodosPagamento: { descricao: string };
  }>;
  nfesTransportes: {
    modalidadeFrete: string;
    transportadoraNomeRazaosocial: string | null;
    transportadoraCpfCnpj: string | null;
    veiculoPlaca: string | null;
    quantidadeVolumes: number | null;
    pesoBruto: number | null;
    pesoLiquido: number | null;
  } | null;
  nfesInformacoesAdicionais: {
    informacoesComplementares: string | null;
  } | null;
}

export const statusLabels: Record<StatusNfe, string> = {
  RASCUNHO: "Rascunho",
  EMITIDA: "Emitida",
  CANCELADA: "Cancelada",
};

export const tipoLabels: Record<TipoOperacaoNfe, string> = {
  ENTRADA: "Entrada",
  SAIDA: "Saída",
};

export const modalidadeFreteLabels: Record<string, string> = {
  EMITENTE: "Por conta do emitente",
  DESTINATARIO: "Por conta do destinatário",
  TERCEIROS: "Por conta de terceiros",
  REMETENTE_PROPRIO: "Transporte próprio do remetente",
  DESTINATARIO_PROPRIO: "Transporte próprio do destinatário",
  SEM_TRANSPORTE: "Sem transporte",
};

export const MIN_JUSTIFICATIVA = 15;

export const brl = (v: number | string) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(Number(v));

// 4 blocos de 4 dígitos separados por espaço, como no DANFE.
export const chaveFormatada = (c: string | null) => (c ?? "").replace(/(\d{4})(?=\d)/g, "$1 ");
