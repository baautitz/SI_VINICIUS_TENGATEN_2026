import { z } from "zod";

export const skuFormSchema = z.object({
  sku: z.string().min(1, "SKU é obrigatório."),
  gtinEan: z.string().nullable().optional(),
  preco: z.coerce.number().min(0, "Preço não pode ser negativo."),
  ativo: z.boolean().default(true),
  atributoValorIds: z.array(z.number()).optional(),
});

export type SkuFormValues = z.infer<typeof skuFormSchema>;
