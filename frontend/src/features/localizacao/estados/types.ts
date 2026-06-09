import { z } from "zod";
import type { Pais } from "@/features/localizacao/paises/types";

export interface Estado {
  id: number;
  estado: string;
  uf: string;
  pais: Pais;
}

export const estadoSchema = z.object({
  estado: z.string().min(1, "Estado é obrigatório").max(100),
  uf: z.string().min(2, "UF é obrigatória").max(2),
  paisId: z.number({ required_error: "País é obrigatório" }).nullable().optional(),
});

export type EstadoFormValues = z.infer<typeof estadoSchema>;
