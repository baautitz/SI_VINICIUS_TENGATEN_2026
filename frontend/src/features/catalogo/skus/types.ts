export interface SkuResumo {
  sku: string;
  gtinEan?: string | null;
  preco: number;
  estoque: number;
  custoMedio: number;
  custoUltimaCompra: number;
  ativo: boolean;
  produtoId: number;
  produtoNome: string;
  unidadeMedidaSigla: string;
  permiteDecimais: boolean;
}

export function formatSkuLabel(s?: SkuResumo | null): string {
  if (!s) return "";
  return `${s.sku} - ${s.produtoNome}`;
}
