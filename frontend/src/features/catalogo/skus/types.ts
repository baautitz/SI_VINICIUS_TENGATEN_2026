import { z } from "zod";
import type { SkuAtributoValor } from "@/features/catalogo/atributos/types";
import type { UnidadeMedida } from "@/features/catalogo/unidades-medida/types";

export interface Sku {
  sku: string;
  gtinEan?: string | null;
  preco: number;
  estoque: number;
  ativo: boolean;
  custoMedio: number;
  custoUltimaCompra: number;
  atributos: SkuAtributoValor[];
  produto?: {
    id: number;
    produto: string;
    unidadeMedida: UnidadeMedida;
  };
}

export const skuSchema = z.object({
  sku: z.string().min(1, "SKU é obrigatório."),
  gtinEan: z.string().nullable().optional(),
  preco: z.coerce.number().min(0, "Preço não pode ser negativo."),
  ativo: z.boolean().default(true),
  atributoValorIds: z.array(z.number()).optional(),
});

export type SkuFormValues = z.infer<typeof skuSchema>;

export const getFullSkuName = (sku: Sku | undefined | null): string => {
  if (!sku) return "";
  const produtoNome = sku.produto?.produto ?? "";
  const variacao = sku.atributos?.map((a) => a.valor).join(" / ");
  return variacao ? `${produtoNome} - ${variacao}` : produtoNome;
};
