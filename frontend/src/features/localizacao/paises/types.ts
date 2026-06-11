import { z } from "zod";

export interface Pais {
  id: number;
  pais: string;
  codigoIsoPais: string;
  ddi: string;
  codigoIsoMoeda: string;
  simboloMoeda: string;
}

export const paisSchema = z.object({
  pais: z.string().min(1, "País é obrigatório").max(100),
  codigoIsoPais: z.string().min(3, "Código ISO do país (3 letras) é obrigatório").max(3),
  ddi: z.string().min(1, "DDI é obrigatório").max(5),
  codigoIsoMoeda: z.string().min(1, "Código ISO da moeda é obrigatório").max(50),
  simboloMoeda: z.string().min(1, "Símbolo da Moeda é obrigatório").max(5),
});

export type PaisFormValues = z.infer<typeof paisSchema>;
