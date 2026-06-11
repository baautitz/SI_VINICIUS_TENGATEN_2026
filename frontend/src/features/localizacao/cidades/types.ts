import { z } from "zod";
import type { Estado } from "@/features/localizacao/estados/types";

export interface Cidade {
  id: number;
  cidade: string;
  ddd: number;
  estado: Estado;
}

export const cidadeSchema = z.object({
  cidade: z.string().min(1, "Cidade é obrigatória").max(100),
  ddd: z.coerce.number().min(10, "DDD deve ter no mínimo 2 dígitos").max(99, "DDD deve ter no máximo 2 dígitos"),
  estadoId: z.number({ required_error: "Estado é obrigatório" }).nullable().optional(),
});

export type CidadeFormValues = z.infer<typeof cidadeSchema>;
