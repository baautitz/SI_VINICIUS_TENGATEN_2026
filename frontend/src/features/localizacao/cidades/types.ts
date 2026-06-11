import { z } from "zod";
import type { Estado } from "@/features/localizacao/estados/types";

export interface Cidade {
  id: number;
  cidade: string;
  ddd: string;
  estado: Estado;
}

export const cidadeSchema = z.object({
  cidade: z.string().min(1, "Cidade é obrigatória").max(100),
  ddd: z.string().regex(/^\d{2,4}$/, "DDD deve conter entre 2 e 4 dígitos").min(2).max(4),
  estadoId: z.number({ required_error: "Estado é obrigatório" }).nullable().optional(),
});

export type CidadeFormValues = z.infer<typeof cidadeSchema>;
