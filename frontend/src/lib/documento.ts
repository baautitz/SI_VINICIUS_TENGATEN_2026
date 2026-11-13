// Validacao de CPF, CNPJ e telefone brasileiros. Aceita o valor com ou sem mascara.
// Mesma regra do backend (Backend.Core/Common/ValueObjects/Cpf.cs e Cnpj.cs).

// Modulo 11 com pesos a partir de 2 da direita para a esquerda, voltando a 2 depois de pesoMaximo.
// O valor de cada caractere e o codigo ASCII menos 48, o que cobre o CNPJ alfanumerico.
function digitoVerificador(base: string, pesoMaximo: number): number {
  let soma = 0
  for (let i = 0; i < base.length; i++) {
    soma += (base.charCodeAt(base.length - 1 - i) - 48) * (2 + (i % (pesoMaximo - 1)))
  }
  const resto = soma % 11
  return resto < 2 ? 0 : 11 - resto
}

function confere(valor: string, pesoMaximo: number): boolean {
  if (/^(.)\1+$/.test(valor)) return false
  const base = valor.slice(0, -2)
  const dv1 = digitoVerificador(base, pesoMaximo)
  return valor.slice(-2) === `${dv1}${digitoVerificador(base + dv1, pesoMaximo)}`
}

export function cpfValido(cpf: string): boolean {
  const valor = cpf.replace(/\D/g, "")
  return valor.length === 11 && confere(valor, 11)
}

export function cnpjValido(cnpj: string): boolean {
  const valor = cnpj.replace(/[^A-Za-z0-9]/g, "").toUpperCase()
  return /^[A-Z0-9]{12}\d{2}$/.test(valor) && confere(valor, 9)
}

/** DDD + numero: 10 digitos (fixo) ou 11 (celular). */
export function telefoneValido(telefone: string): boolean {
  return /^\d{10,11}$/.test(telefone.replace(/\D/g, ""))
}
