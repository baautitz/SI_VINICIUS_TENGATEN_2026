import { z } from "zod";
import type { Cidade } from "@/features/localizacao/cidades/types";

export interface Bairro {
  id: number;
  bairro: string;
  cidade: Cidade;
}

export const bairroSchema = z.object({
  bairro: z.string().min(1, "Bairro é obrigatório").max(100),
  cidadeId: z.number({ required_error: "Cidade é obrigatória" }).nullable().optional(),
});

export type BairroFormValues = z.infer<typeof bairroSchema>;
