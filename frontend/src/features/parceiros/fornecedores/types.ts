import { z } from "zod"
import type { Bairro } from "@/features/localizacao/bairros/types"

export interface FornecedorResumo {
  id: number
  nomeRazaosocial: string
  cpfCnpj: string
  apelidoNomefantasia?: string
}

export interface Fornecedor {
  id: number
  nomeRazaosocial: string
  cpfCnpj: string
  rgIe?: string
  apelidoNomefantasia?: string
  endereco?: string
  bairro?: Bairro
  telefone?: string
  email?: string
  ativo: boolean
  observacao?: string
}

export type FornecedorDto = FornecedorResumo;

export const fornecedorSchema = z.object({
  nomeRazaosocial: z.string().min(1, "Nome/Razão Social é obrigatório."),
  cpfCnpj: z.string().min(1, "CPF/CNPJ é obrigatório."),
  rgIe: z.string().optional(),
  apelidoNomefantasia: z.string().optional(),
  endereco: z.string().optional(),
  bairroId: z.number().nullable().optional(),
  telefone: z.string().optional(),
  email: z.string().email("E-mail inválido").or(z.literal("")).optional(),
  observacao: z.string().optional(),
  ativo: z.boolean().default(true),
})

export type FornecedorFormValues = z.infer<typeof fornecedorSchema>
