import { z } from "zod"
import type { Cidade } from "../cidades/types"

export interface BairroResumo {
  id: number
  bairro: string
  cidadeId: number
  cidadeNome: string
  uf: string
}

export interface Bairro {
  id: number
  bairro: string
  cidade: Cidade
}

export function formatBairroLabel(bairro?: Bairro | null): string {
  if (!bairro) return "";
  const { bairro: nome, cidade } = bairro;
  if (!cidade) return nome;
  const cidadeNome = cidade.cidade;
  const uf = cidade.estado?.uf;

  if (cidadeNome && uf) {
    return `${nome} (${cidadeNome}/${uf})`;
  }
  if (cidadeNome) {
    return `${nome} (${cidadeNome})`;
  }
  return nome;
}

export type BairroDto = BairroResumo;

export const bairroSchema = z.object({
  bairro: z.string().min(1, "Bairro é obrigatório."),
  cidadeId: z.number({ required_error: "Cidade é obrigatória." }).nullable(),
})

export type BairroFormValues = z.infer<typeof bairroSchema>
