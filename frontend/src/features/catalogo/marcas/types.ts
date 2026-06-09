import { z } from "zod";

export interface Marca {
  id: number;
  marca: string;
  descricao?: string | null;
  ativo: boolean;
}

export const marcaSchema = z.object({
  marca: z
    .string()
    .min(1, "Marca é obrigatória.")
    .max(100, "Marca deve ter no máximo 100 caracteres."),
  descricao: z
    .string()
    .max(255, "Descrição deve ter no máximo 255 caracteres.")
    .nullable()
    .optional(),
  ativo: z.boolean().default(true),
});

export type MarcaFormValues = z.infer<typeof marcaSchema>;
