export type OrigemTitulo = "MANUAL" | "VENDA" | "COMPRA" | "DEVOLUCAO_VENDA";

export const origemTituloLabels: Record<OrigemTitulo, string> = {
  MANUAL: "Manual",
  VENDA: "Venda",
  COMPRA: "Compra",
  DEVOLUCAO_VENDA: "Devolução de venda",
};

// Título gerado por um documento só se cancela cancelando o documento de origem.
export function avisoTituloExterno(origemTipo: OrigemTitulo, origemId?: number | null): string {
  return `Conta gerada por ${origemTituloLabels[origemTipo]}${origemId ? ` #${origemId}` : ""}. Para cancelá-la, cancele o documento de origem.`;
}
