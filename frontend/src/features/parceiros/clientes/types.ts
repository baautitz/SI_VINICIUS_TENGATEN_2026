import { z } from "zod"
import type { Bairro } from "@/features/localizacao/bairros/types"
import type { Pais } from "@/features/localizacao/paises/types"
import { TipoPessoa } from "@/api/types"

export interface ClienteResumo {
  id: number
  tipoPessoa: TipoPessoa
  nomeRazaoSocial: string
  cpfCnpj: string
  apelidoNomeFantasia?: string
  nacionalidadeId: number
}

export interface Cliente {
  id: number
  tipoPessoa: TipoPessoa
  nomeRazaoSocial: string
  cpfCnpj: string
  rgIe?: string
  apelidoNomeFantasia?: string
  endereco?: string
  bairro?: Bairro
  nacionalidade: Pais
  telefone?: string
  email?: string
  limiteCredito: number
  ativo: boolean
  observacao?: string
}

export type ClienteDto = ClienteResumo;

export const clienteSchema = z.object({
  tipoPessoa: z.nativeEnum(TipoPessoa, { required_error: "Tipo de Pessoa é obrigatório." }),
  nomeRazaoSocial: z.string().min(1, "Nome/Razão Social é obrigatório."),
  cpfCnpj: z.string().min(1, "CPF/CNPJ é obrigatório."),
  rgIe: z.string().optional(),
  apelidoNomeFantasia: z.string().optional(),
  endereco: z.string().optional(),
  bairroId: z.number().nullable().optional(),
  nacionalidadeId: z.number({ required_error: "Nacionalidade é obrigatória." }).min(1, "Nacionalidade é obrigatória."),
  telefone: z.string().optional(),
  email: z.string().email("E-mail inválido").or(z.literal("")).optional(),
  limiteCredito: z.coerce.number().min(0, "Limite de crédito não pode ser negativo."),
  observacao: z.string().optional(),
  ativo: z.boolean().default(true),
})

export type ClienteFormValues = z.infer<typeof clienteSchema>
