import { z } from "zod"
import type { Bairro } from "@/features/localizacao/bairros/types"
import type { Pais } from "@/features/localizacao/paises/types"
import { TipoPessoa } from "@/api/types"

export interface Transportadora {
  id: number
  tipoPessoa: TipoPessoa
  nomeRazaosocial: string
  cpfCnpj: string
  rgIe?: string
  apelidoNomefantasia?: string
  logradouro?: string
  numero?: string
  bairro?: Bairro
  nacionalidade: Pais
  telefone?: string
  email?: string
  rntrc?: string
  ativo: boolean
  sexo?: string
  dataNascimento?: string
  observacao?: string
}

export const transportadoraSchema = z.object({
  tipoPessoa: z.nativeEnum(TipoPessoa, { required_error: "Tipo de Pessoa é obrigatório." }),
  nomeRazaosocial: z.string().min(1, "Nome/Razão Social é obrigatório."),
  cpfCnpj: z.string().min(1, "CPF/CNPJ é obrigatório."),
  rgIe: z.string().optional(),
  apelidoNomefantasia: z.string().optional(),
  logradouro: z.string().min(1, "Logradouro é obrigatório."),
  numero: z.string().min(1, "Número é obrigatório."),
  sexo: z.string().optional(),
  dataNascimento: z.string().min(1, "Data de nascimento/fundação é obrigatória."),
  bairroId: z.number({ required_error: "Bairro é obrigatório.", invalid_type_error: "Bairro é obrigatório." }).min(1, "Bairro é obrigatório."),
  nacionalidadeId: z.number({ required_error: "Nacionalidade é obrigatória." }).min(1, "Nacionalidade é obrigatória."),
  telefone: z.string().min(1, "Telefone é obrigatório."),
  email: z.string().min(1, "E-mail é obrigatório.").email("E-mail inválido"),
  rntrc: z.string().optional(),
  observacao: z.string().optional(),
  ativo: z.boolean().default(true),
})

export type TransportadoraFormValues = z.infer<typeof transportadoraSchema>
