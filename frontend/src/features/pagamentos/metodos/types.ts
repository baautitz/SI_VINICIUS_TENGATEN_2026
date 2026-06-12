import { z } from "zod";

export interface MetodoPagamento {
  codigo: string;
  descricao: string;
  ativo: boolean;
}

export const metodoPagamentoSchema = z.object({
  codigo: z
    .string()
    .max(10, "Código deve ter no máximo 10 caracteres.")
    .optional()
    .or(z.literal("")),
  descricao: z
    .string()
    .min(1, "Descrição é obrigatória.")
    .max(100, "Descrição deve ter no máximo 100 caracteres."),
  ativo: z.boolean().default(true),
});

export type MetodoPagamentoFormValues = z.infer<typeof metodoPagamentoSchema>;
