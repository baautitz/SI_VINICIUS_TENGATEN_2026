import { z } from "zod";

export interface Pais {
  id: number;
  pais: string;
  siglaIso: string;
  ddi: string;
  moeda: string;
  simboloMoeda: string;
}

export const paisSchema = z.object({
  pais: z.string().min(1, "País é obrigatório").max(100),
  siglaIso: z.string().min(3, "Sigla ISO 3 é obrigatória").max(3),
  ddi: z.string().min(1, "DDI é obrigatório").max(5),
  moeda: z.string().min(1, "Moeda é obrigatória").max(50),
  simboloMoeda: z.string().min(1, "Símbolo da Moeda é obrigatório").max(5),
});

export type PaisFormValues = z.infer<typeof paisSchema>;
