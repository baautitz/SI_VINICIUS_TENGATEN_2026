import { z } from "zod";

export interface UnidadeMedida {
  id: number;
  sigla: string;
  descricao: string;
  categoria: string;
  permiteDecimais: boolean;
  ativo: boolean;
}

export const unidadeMedidaSchema = z.object({
  sigla: z.string().min(1, "Sigla é obrigatória.").max(10, "Máximo 10 caracteres."),
  descricao: z.string().min(1, "Descrição é obrigatória.").max(100, "Máximo 100 caracteres."),
  categoria: z.string().min(1, "Categoria é obrigatória.").max(50, "Máximo 50 caracteres."),
  permiteDecimais: z.boolean().default(false),
  ativo: z.boolean().default(true),
});

export type UnidadeMedidaFormValues = z.infer<typeof unidadeMedidaSchema>;
