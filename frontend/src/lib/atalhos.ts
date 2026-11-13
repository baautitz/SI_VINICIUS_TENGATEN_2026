// Registro unico de atalhos (ADR 0001, ADR 0005): uma combinacao, um sentido por contexto.
// Documentacao: docs/atalhos.md

// contexto: "global" (vale em qualquer tela) ou nome(s) da tela/janela em que o atalho vale.
function atalho<const C extends string, const X extends string | readonly string[]>(combinacao: C, rotulo: string, contexto: X) {
  return { combinacao, rotulo, contexto } as const;
}

type Contextos<X> = X extends readonly (infer U)[] ? U : X;

export const ATALHOS = {
  novo: atalho("Alt+N", "Novo", "global"),
  busca: atalho("Alt+Q", "Busca", "global"),
  filtros: atalho("Alt+F", "Filtros", "global"),
  ordenar: atalho("Alt+J", "Ordenar", "global"),
  sidebarBuscar: atalho("Alt+S", "Sidebar: buscar", "global"),
  sidebarRecolher: atalho("Alt+B", "Sidebar: recolher", "global"),
  sidebarInicio: atalho("Alt+H", "Sidebar: inicio", "global"),
  adicionarSku: atalho("Alt+K", "Adicionar/buscar SKU", "global"),
  estornar: atalho("Alt+Z", "Estornar", "global"),
  gerar: atalho("Alt+G", "Gerar", ["compra", "venda"]),
  adicionarParcela: atalho("Alt+P", "Adicionar parcela", ["compra", "contas-pagar", "contas-receber", "condicao"]),
  distribuir: atalho("Alt+C", "Distribuir", ["compra", "condicao"]),
  imprimir: atalho("Alt+I", "Imprimir", "nfe"),
  importarXml: atalho("Alt+X", "Importar XML", "compra-lista"),
  verDevolucoes: atalho("Alt+D", "Ver devolucoes", "venda"),
  verContasReceber: atalho("Alt+R", "Ver contas a receber", "venda"),
  verMovimentacao: atalho("Alt+M", "Ver movimentacao", "venda"),
  verVendas: atalho("Alt+V", "Ver vendas", "cliente"),
  verObservacao: atalho("Alt+O", "Ver observacao", "venda"),
  verTitulos: atalho("Alt+T", "Ver titulos", ["cliente", "fornecedor", "compra"]),
  verLocalidades: atalho("Alt+L", "Ver localidades", ["pais", "estado", "cidade"]),
  verXml: atalho("Alt+X", "Ver XML", "nfe"),
  baixarXml: atalho("Alt+D", "Baixar XML", "nfe"),
  salvarRascunho: atalho("Alt+R", "Salvar rascunho", "balanco"),
  reembolsos: atalho("Alt+Shift+B", "Reembolsos", "cliente"),
  movimentacaoOriginal: atalho("Alt+M", "Movimentacao original", "movimentacao"),
  verEstorno: atalho("Alt+V", "Ver estorno", "movimentacao"),
  origem: atalho("Alt+O", "Origem", ["movimentacao", "contas-pagar", "contas-receber"]),
  adicionarOpcaoProduto: atalho("Alt+U", "Adicionar opcao de produto", "produto"),
  replicarPreco: atalho("Alt+C", "Replicar preco entre variacoes", "produto"),
  verProdutos: atalho("Alt+P", "Ver produtos", ["marca", "categoria", "unidade-medida"]),
  confirmar: atalho("Alt+Enter", "Confirmar", "global"),
  editarLinha: atalho("Alt+E", "Editar linha", "global"),
  baixarLinha: atalho("Alt+A", "Baixar linha", "global"),
  abrirLinha: atalho("Enter", "Abrir linha", "global"),
  excluirLinha: atalho("Delete", "Excluir linha", "global"),
  excluirLinhaBackspace: atalho("Backspace", "Excluir linha (Backspace)", "global"),
  anterior: atalho("Alt+ArrowLeft", "Etapa ou pagina anterior", "global"),
  seguinte: atalho("Alt+ArrowRight", "Etapa ou proxima pagina", "global"),
  devolucaoParcial: atalho("Alt+1", "Devolucao parcial", ["venda-devolucao", "compra-devolucao"]),
  devolucaoTotal: atalho("Alt+2", "Devolucao total", ["venda-devolucao", "compra-devolucao"]),
} as const;

export type AtalhoId = keyof typeof ATALHOS;
export type Atalho = (typeof ATALHOS)[AtalhoId];
export type Combinacao = Atalho["combinacao"];

export function combinacaoDe(id: AtalhoId): Combinacao {
  return ATALHOS[id].combinacao;
}

// Falha a compilacao se duas combinacoes iguais valem no mesmo contexto (ou uma delas e global).
type Colide<A extends string, B extends string> = "global" extends A | B ? true : [Extract<A, B>] extends [never] ? false : true;
type Duplicados = {
  [K in AtalhoId]: {
    [J in Exclude<AtalhoId, K>]: (typeof ATALHOS)[K]["combinacao"] extends (typeof ATALHOS)[J]["combinacao"]
      ? Colide<Contextos<(typeof ATALHOS)[K]["contexto"]>, Contextos<(typeof ATALHOS)[J]["contexto"]>> extends true
        ? K
        : never
      : never;
  }[Exclude<AtalhoId, K>];
}[AtalhoId];
export const semAtalhosDuplicados: [Duplicados] extends [never] ? true : never = true;
