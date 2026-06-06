import { z } from "zod";

export interface UnidadeMedida {
  id: number;
  sigla: string;
  descricao: string;
  categoria: string;
  permiteDecimais: boolean;
  ativo: boolean;
}

export interface UnidadeMedidaResumo {
  id: number;
  sigla: string;
  descricao: string;
  categoria: string;
  permiteDecimais: boolean;
  ativo: boolean;
}

export function formatUnidadeMedidaLabel(um?: UnidadeMedidaResumo | UnidadeMedida | null): string {
  if (!um) return "";
  return `${um.descricao} (${um.sigla})`;
}

export const unidadeMedidaSchema = z.object({
  sigla: z
    .string()
    .min(1, "Sigla é obrigatória.")
    .max(10, "Sigla deve ter no máximo 10 caracteres.")
    .regex(/^[A-Za-z0-9]+$/, "Sigla deve conter apenas letras e números."),
  descricao: z
    .string()
    .min(1, "Descrição é obrigatória.")
    .max(100, "Descrição deve ter no máximo 100 caracteres."),
  categoria: z
    .string()
    .min(1, "Categoria é obrigatória.")
    .max(50, "Categoria deve ter no máximo 50 caracteres."),
  permiteDecimais: z.boolean().default(false),
  ativo: z.boolean().default(true),
});

export type UnidadeMedidaFormValues = z.infer<typeof unidadeMedidaSchema>;
