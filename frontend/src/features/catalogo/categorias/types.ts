import { z } from "zod";

export interface Categoria {
  id: number;
  categoria: string;
  descricao?: string | null;
  ativo: boolean;
}

export interface CategoriaResumo {
  id: number;
  categoria: string;
  ativo: boolean;
}

export function formatCategoriaLabel(cat?: CategoriaResumo | Categoria | null): string {
  if (!cat) return "";
  return cat.categoria;
}

export const categoriaSchema = z.object({
  categoria: z
    .string()
    .min(1, "Categoria é obrigatória.")
    .max(100, "Categoria deve ter no máximo 100 caracteres."),
  descricao: z
    .string()
    .max(255, "Descrição deve ter no máximo 255 caracteres.")
    .nullable()
    .optional(),
  ativo: z.boolean().default(true),
});

export type CategoriaFormValues = z.infer<typeof categoriaSchema>;
