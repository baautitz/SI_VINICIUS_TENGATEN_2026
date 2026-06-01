import { z } from "zod"
import type { Estado } from "../estados/types"

export interface CidadeResumo {
  id: number
  cidade: string
  ddd: number
  estadoId: number
  estadoNome: string
  uf: string
}

export interface Cidade {
  id: number
  cidade: string
  ddd: number
  estado: Estado
}

export function formatCidadeLabel(cidade?: Cidade | null): string {
  if (!cidade) return "";
  const { cidade: nome, estado } = cidade;
  if (!estado) return nome;
  const uf = estado.uf;

  if (uf) {
    return `${nome} (${uf})`;
  }
  return nome;
}

export type CidadeDto = CidadeResumo;

export const cidadeSchema = z.object({
  cidade: z.string().min(1, "Cidade é obrigatória."),
  ddd: z.coerce
    .number({ invalid_type_error: "DDD deve ser um número." })
    .int("DDD deve ser um número inteiro.")
    .positive("DDD deve ser maior que zero.")
    .nullable(),
  estadoId: z.number({ required_error: "Estado é obrigatório." }).nullable(),
})

export type CidadeFormValues = z.infer<typeof cidadeSchema>
