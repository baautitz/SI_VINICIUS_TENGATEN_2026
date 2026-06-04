import { z } from "zod"
import type { Pais } from "../paises/types"

export interface EstadoResumo {
  id: number
  estado: string
  uf: string
  paisId: number
  paisNome: string
}

export interface Estado {
  id: number
  estado: string
  uf: string
  pais: Pais
}

export function formatEstadoLabel(estado?: Estado | null): string {
  if (!estado) return "";
  const { estado: nome, uf } = estado;
  if (uf) {
    return `${nome} (${uf})`;
  }
  return nome;
}

export type EstadoDto = EstadoResumo;

export const estadoSchema = z.object({
  estado: z.string().min(1, "Estado é obrigatório."),
  uf: z.string().min(1, "UF é obrigatória.").max(2, "UF deve ter no máximo 2 caracteres."),
  paisId: z.number({ required_error: "País é obrigatório." }).nullable().refine((val) => val !== null, {
    message: "Selecione um país.",
  }),
})

export type EstadoFormValues = z.infer<typeof estadoSchema>
