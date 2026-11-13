// Motor de mascara puro (sem React). O estado guarda o valor limpo ("12345678901");
// a tela mostra o valor formatado ("123.456.789-01").
//
// No padrao, 9 = digito e * = letra ou digito; qualquer outro caractere e separador.
// Cada mascara usa um unico tipo de posicao: assim apagar no meio so desloca o
// restante, sem descartar caracteres que cairiam numa posicao de outro tipo.
const POSICOES: Record<string, RegExp> = { "9": /\d/, "*": /[A-Za-z0-9]/ }

/** Um padrao, ou varios em ordem crescente de tamanho (ex.: telefone fixo e celular). */
export type Mascara = string | readonly string[]

export const MASCARAS = {
  cpf: "999.999.999-99",
  // CNPJ alfanumerico (2026): letras e digitos nas 14 posicoes; o digito verificador valida.
  cnpj: "**.***.***/****-**",
  cpfCnpj: ["***.***.***-**", "**.***.***/****-**"],
  telefone: ["(99) 9999-9999", "(99) 99999-9999"],
  cep: "99999-999",
  data: "99/99/9999",
  placa: "*******",
  // GTIN-8, 12, 13 ou 14: so digitos, sem separador.
  ean: "99999999999999",
} as const satisfies Record<string, Mascara>

const padroesDe = (mascara: Mascara) => (typeof mascara === "string" ? [mascara] : mascara)
const posicoesDe = (padrao: string) => [...padrao].filter((c) => POSICOES[c])

/** Mascara so de digitos (para escolher o teclado numerico). */
export const mascaraNumerica = (mascara: Mascara) => !padroesDe(mascara).join("").includes("*")

/** Remove a mascara: fica so o que cabe nas posicoes, em maiusculas. */
export function limparMascara(texto: string | null | undefined, mascara: Mascara): string {
  const lista = padroesDe(mascara)
  const posicoes = posicoesDe(lista[lista.length - 1])
  let limpo = ""
  for (const c of texto ?? "") {
    if (limpo.length === posicoes.length) break
    if (POSICOES[posicoes[limpo.length]].test(c)) limpo += c
  }
  return limpo.toUpperCase()
}

// Formata e devolve onde fica o cursor depois de `antes` caracteres limpos.
// O separador so aparece quando ha caractere depois dele, entao apagar nunca trava.
function montar(limpo: string, mascara: Mascara, antes: number) {
  const lista = padroesDe(mascara)
  const padrao = lista.find((p) => posicoesDe(p).length >= limpo.length) ?? lista[lista.length - 1]
  let formatado = ""
  let cursor = 0
  let i = 0
  for (const c of padrao) {
    if (i === limpo.length) break
    if (!POSICOES[c]) {
      formatado += c
      continue
    }
    formatado += limpo[i++]
    if (i === antes) cursor = formatado.length
  }
  return { formatado, cursor: antes >= limpo.length ? formatado.length : cursor }
}

/** O texto cabe na mascara: tirando os separadores, sobra so o que as posicoes aceitam, sem excesso. */
export function encaixaNaMascara(texto: string | null | undefined, mascara: Mascara): boolean {
  const separadores = new Set([...padroesDe(mascara).join("")].filter((c) => !POSICOES[c]))
  const resto = [...(texto ?? "")].filter((c) => !separadores.has(c)).join("")
  return limparMascara(resto, mascara).length === resto.length
}

/**
 * Aplica a mascara a um valor limpo ou ja formatado. Com `livre`, o valor que
 * nao cabe na mascara (ex.: telefone internacional) sai como veio.
 */
export function aplicarMascara(valor: string | null | undefined, mascara: Mascara, livre = false): string {
  if (livre && !encaixaNaMascara(valor, mascara)) return valor ?? ""
  return montar(limparMascara(valor, mascara), mascara, 0).formatado
}

/**
 * Resolve uma edicao do campo: recebe o texto como o navegador deixou e a posicao
 * do cursor, devolve o valor limpo, o texto formatado e o cursor no mesmo ponto
 * logico (depois da mesma quantidade de caracteres limpos).
 *
 * `anterior` (valor limpo antes da edicao) e `tipoEdicao` (InputEvent.inputType)
 * servem para o caso em que a tecla apagou so um separador: apaga-se o caractere
 * vizinho, senao Backspace/Delete nao fariam nada.
 *
 * Com `livre`, a mascara vale so enquanto o texto cabe nela.
 */
export function editarMascara(
  texto: string,
  cursor: number,
  mascara: Mascara,
  anterior = "",
  tipoEdicao?: string,
  livre = false,
) {
  // Mascara livre: o que nao cabe vira texto comum, do jeito que foi digitado.
  if (livre && !encaixaNaMascara(texto, mascara)) return { limpo: texto, formatado: texto, cursor }
  let limpo = limparMascara(texto, mascara)
  let antes = Math.min(limparMascara(texto.slice(0, cursor), mascara).length, limpo.length)
  if (limpo === anterior) {
    if (tipoEdicao === "deleteContentBackward" && antes > 0) {
      limpo = limpo.slice(0, antes - 1) + limpo.slice(antes)
      antes--
    } else if (tipoEdicao === "deleteContentForward") {
      limpo = limpo.slice(0, antes) + limpo.slice(antes + 1)
    }
  }
  return { limpo, ...montar(limpo, mascara, antes) }
}

/** CPF/CNPJ para exibicao; documento de outro formato (estrangeiro) sai como veio. */
export function formatarDocumento(valor: string | null | undefined): string {
  const v = valor ?? ""
  if (/^\d{11}$/.test(v)) return aplicarMascara(v, MASCARAS.cpf)
  if (/^[A-Za-z0-9]{14}$/.test(v)) return aplicarMascara(v, MASCARAS.cnpj)
  return v
}

/** Telefone brasileiro para exibicao; outro formato sai como veio. */
export function formatarTelefone(valor: string | null | undefined): string {
  const v = valor ?? ""
  return /^\d{10,11}$/.test(v) ? aplicarMascara(v, MASCARAS.telefone) : v
}
