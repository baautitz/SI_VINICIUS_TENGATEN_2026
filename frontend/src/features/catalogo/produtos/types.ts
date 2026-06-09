import { z } from "zod";
import type { Categoria } from "@/features/catalogo/categorias/types";
import type { Marca } from "@/features/catalogo/marcas/types";
import type { UnidadeMedida } from "@/features/catalogo/unidades-medida/types";
import type { Sku } from "@/features/catalogo/skus/types";
import { skuSchema } from "@/features/catalogo/skus/types";

export interface Produto {
  id: number;
  produto: string;
  descricao?: string | null;
  ativo: boolean;
  categoria: Categoria;
  marca: Marca;
  unidadeMedida: UnidadeMedida;
  skus: Sku[];
  estoqueTotal: number;
}

export const produtoSchema = z.object({
  produto: z.string().min(1, "Nome do produto é obrigatório."),
  descricao: z.string().nullable().optional(),
  ativo: z.boolean().default(true),
  categoriaId: z.number().min(1, "Categoria é obrigatória."),
  marcaId: z.number().min(1, "Marca é obrigatória."),
  unidadeMedidaId: z.number().min(1, "Unidade de medida é obrigatória."),
  skus: z.array(skuSchema).min(1, "Pelo menos um SKU é obrigatório."),
});

export type ProdutoFormValues = z.infer<typeof produtoSchema>;
