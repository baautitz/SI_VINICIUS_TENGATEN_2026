import { z } from "zod"
import type { Bairro } from "@/features/localizacao/bairros/types"
import type { Pais } from "@/features/localizacao/paises/types"
import { TipoPessoa } from "@/api/types"

export interface Emitente {
  id: number
  tipoPessoa: TipoPessoa
  nomeRazaoSocial: string
  cpfCnpj: string
  rgIe?: string
  apelidoNomeFantasia?: string
  logradouro?: string
  numero?: string
  bairro?: Bairro
  nacionalidade: Pais
  telefone?: string
  email?: string
  inscricaoMunicipal?: string
  regimeTributario?: string
  observacao?: string
  ativo: boolean
}

export const emitenteSchema = z.object({
  tipoPessoa: z.nativeEnum(TipoPessoa, { required_error: "Tipo de Pessoa é obrigatório." }),
  nomeRazaoSocial: z.string().min(1, "Nome/Razão Social é obrigatório."),
  cpfCnpj: z.string().min(1, "CPF/CNPJ é obrigatório."),
  apelidoNomeFantasia: z.string().optional(),
  logradouro: z.string().optional(),
  numero: z.string().optional(),
  bairroId: z.number().nullable().optional(),
  nacionalidadeId: z.number({ required_error: "Nacionalidade é obrigatória." }).min(1, "Nacionalidade é obrigatória."),
  telefone: z.string().optional(),
  email: z.string().email("E-mail inválido").or(z.literal("")).optional(),
  rgIe: z.string().optional(),
  inscricaoMunicipal: z.string().optional(),
  regimeTributario: z.string().optional(),
  observacao: z.string().optional(),
  ativo: z.boolean().default(true),
})

export type EmitenteFormValues = z.infer<typeof emitenteSchema>
