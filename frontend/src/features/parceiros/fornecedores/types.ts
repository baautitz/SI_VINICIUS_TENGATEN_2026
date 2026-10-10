import { z } from "zod"
import type { Bairro } from "@/features/localizacao/bairros/types"
import type { Pais } from "@/features/localizacao/paises/types"
import type { CondicaoPagamento } from "@/features/financeiro/condicoes/types"
import { TipoPessoa } from "@/api/types"

export interface Fornecedor {
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
  ativo: boolean
  sexo?: string
  dataNascimento?: string
  observacao?: string
  condicaoPagamento?: CondicaoPagamento | null
}

export const fornecedorSchema = z.object({
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
  observacao: z.string().optional(),
  condicaoPagamentoId: z.number().nullable().optional(),
  ativo: z.boolean().default(true),
})

export type FornecedorFormValues = z.infer<typeof fornecedorSchema>

/** Itens exibidos nos campos de entidade de um cadastro novo, guardados em memória até gravar. */
export interface ItensSelecionados {
  nacionalidade?: Pais | null
  bairro?: Bairro | null
  condicaoPagamento?: CondicaoPagamento | null
}
