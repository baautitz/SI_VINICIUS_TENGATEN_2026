import { z } from "zod"
import { TipoPessoa } from "@/api/types"
import { cnpjValido, cpfValido, telefoneValido } from "@/lib/documento"
import { MASCARAS, encaixaNaMascara, limparMascara } from "@/lib/mascara"

// Regras de documento e telefone comuns aos cadastros de pessoas. Fora do Brasil
// o documento e o telefone sao texto livre, sem mascara nem digito verificador.

export const mascaraDocumento = (isBrasil: boolean, tipoPessoa: TipoPessoa) =>
  !isBrasil ? undefined : tipoPessoa === TipoPessoa.FISICA ? MASCARAS.cpf : MASCARAS.cnpj

export const documentoSchema = (isBrasil: boolean, tipoPessoa: TipoPessoa) => {
  const fisica = tipoPessoa === TipoPessoa.FISICA
  return z
    .string()
    .trim()
    .min(1, isBrasil ? `${fisica ? "CPF" : "CNPJ"} é obrigatório.` : "Documento é obrigatório.")
    .refine(
      (v) => !isBrasil || (fisica ? cpfValido(v) : cnpjValido(v)),
      fisica ? "CPF inválido." : "CNPJ inválido.",
    )
}

// Telefone brasileiro usa a mascara enquanto couber nela (DDD + 8 ou 9 digitos).
// O que nao cabe (ramal, numero internacional) e o telefone de estrangeiro sao texto livre.
export const mascaraTelefone = (isBrasil: boolean) => (isBrasil ? MASCARAS.telefone : undefined)

/** Acrescenta a regra de formato ao schema do cadastro, que continua dizendo se o campo é obrigatório. */
export const telefoneSchema = <T extends z.ZodTypeAny>(base: T, isBrasil: boolean) =>
  base.refine(
    (v) => !isBrasil || !v || !encaixaNaMascara(v, MASCARAS.telefone) || telefoneValido(v),
    "Telefone incompleto. Informe DDD e número.",
  )

/** Telefone como vai para a API: só dígitos quando cabe na máscara brasileira. */
export const telefoneParaApi = (telefone: string | undefined, isBrasil: boolean) =>
  isBrasil && encaixaNaMascara(telefone, MASCARAS.telefone) ? limparMascara(telefone, MASCARAS.telefone) : telefone
