import { z } from "zod";

export interface SkuAtributoValor {
  id: number;
  chaveId: number;
  valor: string;
}

export interface Sku {
  sku: string;
  gtinEan?: string | null;
  preco: number;
  estoque: number;
  ativo: boolean;
  skuAtributosValores: SkuAtributoValor[];
}

export interface CategoriaResumida {
  id: number;
  categoria: string;
}

export interface MarcaResumida {
  id: number;
  marca: string;
}

export interface UnidadeMedidaResumida {
  id: number;
  sigla: string;
  descricao: string;
}

export interface Produto {
  id: number;
  produto: string;
  descricao?: string | null;
  categoria: CategoriaResumida;
  marca: MarcaResumida;
  unidadeMedida: UnidadeMedidaResumida;
  ativo: boolean;
  skus: Sku[];
}

export interface ProdutoResumo {
  id: number;
  produto: string;
  descricao?: string | null;
  ativo: boolean;
}

export function formatProdutoLabel(p?: Produto | ProdutoResumo | null): string {
  if (!p) return "";
  return p.produto;
}

export const skuFormSchema = z.object({
  sku: z
    .string()
    .max(50, "SKU deve ter no máximo 50 caracteres.")
    .optional(),
  preco: z
    .number({ invalid_type_error: "Preço deve ser um número." })
    .min(0, "Preço não pode ser negativo."),
  estoque: z
    .number({ invalid_type_error: "Estoque deve ser um número." })
    .min(0, "Estoque não pode ser negativo."),
  gtinEan: z
    .string()
    .max(14, "Código de barras deve ter no máximo 14 caracteres.")
    .nullable()
    .optional(),
  ativo: z.boolean().default(true),
  atributoValorIds: z.array(z.number()),
  variantLabel: z.string().optional(),
});

export const produtoBaseSchema = z.object({
  produto: z
    .string()
    .min(1, "Nome do produto é obrigatório.")
    .max(150, "Nome deve ter no máximo 150 caracteres."),
  descricao: z.string().optional().nullable(),
  categoriaId: z
    .number({ required_error: "Categoria é obrigatória." })
    .nullable()
    .refine((val) => val !== null, {
      message: "Selecione uma categoria.",
    }),
  marcaId: z
    .number({ required_error: "Marca é obrigatória." })
    .nullable()
    .refine((val) => val !== null, {
      message: "Selecione uma marca.",
    }),
  unidadeMedidaId: z
    .number({ required_error: "Unidade de medida é obrigatória." })
    .nullable()
    .refine((val) => val !== null, {
      message: "Selecione uma unidade de medida.",
    }),
  ativo: z.boolean().default(true),
  skus: z
    .array(skuFormSchema)
    .min(1, "O produto deve conter pelo menos um SKU."),
});

export const produtoSchema = produtoBaseSchema.refine(
  (data) => {
    const codes = data.skus
      .map((s) => s.sku?.trim().toLowerCase())
      .filter(Boolean);
    const uniqueCodes = new Set(codes);
    return uniqueCodes.size === codes.length;
  },
  {
    message: "Não podem existir códigos SKU duplicados no mesmo produto.",
    path: ["skus"],
  },
);

export type SkuFormValues = z.infer<typeof skuFormSchema>;
export type ProdutoFormValues = z.infer<typeof produtoSchema>;

export interface VariantOption {
  keyId: number;
  keyName: string;
  valores: Array<{ id: number; valor: string }>;
}
