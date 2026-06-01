import { z } from "zod"
import type { Bairro } from "@/features/localizacao/bairros/types"

export interface EmitenteResumo {
  id: number
  nomeRazaoSocial: string
  cpfCnpj: string
  apelidoNomeFantasia?: string
}

export interface Emitente {
  id: number
  nomeRazaoSocial: string
  cpfCnpj: string
  rgIe?: string
  apelidoNomeFantasia?: string
  endereco?: string
  bairro?: Bairro
  telefone?: string
  email?: string
  inscricaoMunicipal?: string
  regimeTributario?: string
  observacao?: string
  ativo: boolean
}

export type EmitenteDto = EmitenteResumo;

export const emitenteSchema = z.object({
  nomeRazaoSocial: z.string().min(1, "Nome/Razão Social é obrigatório."),
  cpfCnpj: z.string().min(1, "CPF/CNPJ é obrigatório."),
  apelidoNomeFantasia: z.string().optional(),
  endereco: z.string().optional(),
  bairroId: z.number().nullable().optional(),
  telefone: z.string().optional(),
  email: z.string().email("E-mail inválido").or(z.literal("")).optional(),
  rgIe: z.string().optional(),
  inscricaoMunicipal: z.string().optional(),
  regimeTributario: z.string().optional(),
  observacao: z.string().optional(),
  ativo: z.boolean().default(true),
})

export type EmitenteFormValues = z.infer<typeof emitenteSchema>
