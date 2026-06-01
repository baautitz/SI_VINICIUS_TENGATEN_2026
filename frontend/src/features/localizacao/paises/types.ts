import { z } from "zod";

export interface PaisDto {
  id: number;
  pais: string;
  siglaIso: string;
  ddi: string;
  moeda: string;
  simboloMoeda: string;
}

export const paisSchema = z.object({
  pais: z.string().min(1, "País é obrigatório."),
  siglaIso: z
    .string()
    .min(1, "Sigla ISO é obrigatória.")
    .length(3, "Sigla ISO deve ter exatamente 3 caracteres.")
    .regex(/^[A-Z]+$/, "Sigla ISO deve conter apenas letras maiúsculas."),
  ddi: z
    .string()
    .min(1, "DDI é obrigatório.")
    .regex(/^\+\d+$/, "DDI deve ser no formato +55, +1, etc."),
  moeda: z.string().min(1, "Moeda é obrigatória."),
  simboloMoeda: z.string().min(1, "Símbolo da moeda é obrigatório."),
});

export type PaisFormValues = z.infer<typeof paisSchema>;
