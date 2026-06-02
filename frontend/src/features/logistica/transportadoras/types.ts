import { z } from "zod"
import type { Bairro } from "@/features/localizacao/bairros/types"

export interface TransportadoraResumo {
  id: number
  nomeRazaosocial: string
  cpfCnpj: string
  ativo: boolean
}

export interface Transportadora {
  id: number
  nomeRazaosocial: string
  cpfCnpj: string
  rgIe?: string
  apelidoNomefantasia?: string
  endereco?: string
  bairro?: Bairro
  telefone?: string
  email?: string
  rntrc?: string
  ativo: boolean
  observacao?: string
}

export const transportadoraSchema = z.object({
  nomeRazaosocial: z.string().min(1, "Nome/Razão Social é obrigatório."),
  cpfCnpj: z.string().min(1, "CPF/CNPJ é obrigatório."),
  rgIe: z.string().optional(),
  apelidoNomefantasia: z.string().optional(),
  endereco: z.string().optional(),
  bairroId: z.number().nullable().optional(),
  telefone: z.string().optional(),
  email: z.string().email("E-mail inválido").or(z.literal("")).optional(),
  rntrc: z.string().optional(),
  observacao: z.string().optional(),
  ativo: z.boolean().default(true),
})

export type TransportadoraFormValues = z.infer<typeof transportadoraSchema>
