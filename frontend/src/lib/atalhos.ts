// Registro unico de atalhos (ADR 0001): uma combinacao, um sentido.
// Documentacao: docs/atalhos.md

function atalho<const C extends string>(combinacao: C, rotulo: string) {
  return { combinacao, rotulo } as const;
}

export const ATALHOS = {
  novo: atalho("Alt+N", "Novo"),
  busca: atalho("Alt+Q", "Busca"),
  filtros: atalho("Alt+F", "Filtros"),
  sidebarBuscar: atalho("Alt+S", "Sidebar: buscar"),
  sidebarRecolher: atalho("Alt+B", "Sidebar: recolher"),
  sidebarInicio: atalho("Alt+H", "Sidebar: inicio"),
  adicionarSku: atalho("Alt+K", "Adicionar/buscar SKU"),
  estornar: atalho("Alt+Z", "Estornar"),
  gerar: atalho("Alt+G", "Gerar"),
  adicionarParcela: atalho("Alt+P", "Adicionar parcela"),
  distribuir: atalho("Alt+C", "Distribuir"),
  imprimir: atalho("Alt+I", "Imprimir"),
  importarXml: atalho("Alt+X", "Importar XML"),
  verDevolucoes: atalho("Alt+D", "Ver devolucoes"),
  verContasReceber: atalho("Alt+R", "Ver contas a receber"),
  verMovimentacao: atalho("Alt+M", "Ver movimentacao"),
  verVendas: atalho("Alt+V", "Ver vendas"),
  verObservacao: atalho("Alt+O", "Ver observacao"),
  verTitulos: atalho("Alt+T", "Ver titulos"),
  verLocalidades: atalho("Alt+L", "Ver localidades"),
  verXml: atalho("Alt+Shift+X", "Ver XML"),
  baixarXml: atalho("Alt+Shift+D", "Baixar XML"),
  salvarRascunho: atalho("Alt+Shift+R", "Salvar rascunho"),
  reembolsos: atalho("Alt+Shift+B", "Reembolsos"),
  movimentacaoOriginal: atalho("Alt+Shift+M", "Movimentacao original"),
  verEstorno: atalho("Alt+Shift+V", "Ver estorno"),
  origem: atalho("Alt+Shift+O", "Origem"),
  adicionarOpcaoProduto: atalho("Alt+Shift+A", "Adicionar opcao de produto"),
  replicarPreco: atalho("Alt+Shift+C", "Replicar preco entre variacoes"),
  verProdutos: atalho("Alt+Shift+P", "Ver produtos"),
} as const;

export type AtalhoId = keyof typeof ATALHOS;
export type Atalho = (typeof ATALHOS)[AtalhoId];
export type Combinacao = Atalho["combinacao"];

export function combinacaoDe(id: AtalhoId): Combinacao {
  return ATALHOS[id].combinacao;
}

// Falha a compilacao se duas combinacoes forem iguais.
type Duplicados = {
  [K in AtalhoId]: {
    [J in Exclude<AtalhoId, K>]: (typeof ATALHOS)[K]["combinacao"] extends (typeof ATALHOS)[J]["combinacao"] ? K : never;
  }[Exclude<AtalhoId, K>];
}[AtalhoId];
export const semAtalhosDuplicados: [Duplicados] extends [never] ? true : never = true;
