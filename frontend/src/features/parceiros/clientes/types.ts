import { z } from "zod"
import type { Bairro } from "@/features/localizacao/bairros/types"

export interface ClienteResumo {
  id: number
  nomeRazaoSocial: string
  cpfCnpj: string
  apelidoNomeFantasia?: string
}

export interface Cliente {
  id: number
  nomeRazaoSocial: string
  cpfCnpj: string
  rgIe?: string
  apelidoNomeFantasia?: string
  endereco?: string
  bairro?: Bairro
  telefone?: string
  email?: string
  limiteCredito: number
  ativo: boolean
  observacao?: string
}

export type ClienteDto = ClienteResumo;

export const clienteSchema = z.object({
  nomeRazaoSocial: z.string().min(1, "Nome/Razão Social é obrigatório."),
  cpfCnpj: z.string().min(1, "CPF/CNPJ é obrigatório."),
  rgIe: z.string().optional(),
  apelidoNomeFantasia: z.string().optional(),
  endereco: z.string().optional(),
  bairroId: z.number().nullable().optional(),
  telefone: z.string().optional(),
  email: z.string().email("E-mail inválido").or(z.literal("")).optional(),
  limiteCredito: z.coerce.number().min(0, "Limite de crédito não pode ser negativo."),
  observacao: z.string().optional(),
  ativo: z.boolean().default(true),
})

export type ClienteFormValues = z.infer<typeof clienteSchema>
