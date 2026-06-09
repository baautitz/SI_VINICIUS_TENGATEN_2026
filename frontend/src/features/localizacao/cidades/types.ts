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
  ddd: z.string().min(2, "DDD é obrigatório").max(3),
  estadoId: z.number({ required_error: "Estado é obrigatório" }).nullable().optional(),
});

export type CidadeFormValues = z.infer<typeof cidadeSchema>;
