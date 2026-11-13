// Edicao de campo numerico (valores e quantidades), sem React.
// O estado guarda "1234,50"; a tela mostra "1.234,50".

/** Acrescenta o ponto de milhar a "1234,50". */
export function comMilhares(valor: string): string {
  const [inteiro, fracao] = valor.split(",")
  const agrupado = inteiro.replace(/\B(?=(\d{3})+(?!\d))/g, ".")
  return fracao === undefined ? agrupado : agrupado + "," + fracao
}

export interface OpcoesNumero {
  decimais: number
  negativo?: boolean
}

// So digitos, uma virgula e o sinal na frente; corta as casas decimais em excesso.
function limpar(texto: string, { decimais, negativo = false }: OpcoesNumero): string {
  const sinal = negativo && texto.trimStart().startsWith("-") ? "-" : ""
  const [inteiro, ...resto] = texto.replace(/[^0-9,]/g, "").split(",")
  if (decimais <= 0) return sinal + inteiro + resto.join("")
  if (resto.length === 0) return sinal + inteiro
  return sinal + inteiro + "," + resto.join("").slice(0, decimais)
}

/**
 * Resolve uma edicao: recebe o texto como o navegador deixou e a posicao do cursor,
 * devolve o valor ("1234,5"), o texto formatado ("1.234,5") e o cursor no mesmo
 * ponto logico (depois da mesma quantidade de digitos, virgula e sinal).
 *
 * `anterior` e o valor antes da edicao. `digitado` (InputEvent.data) converte o
 * ponto digitado em virgula. `tipoEdicao` (InputEvent.inputType) trata a tecla que
 * apagou so um ponto de milhar: apaga-se o digito vizinho.
 */
export function editarNumero(
  texto: string,
  cursor: number,
  anterior: string,
  opcoes: OpcoesNumero,
  digitado?: string | null,
  tipoEdicao?: string,
) {
  if (digitado === "." && texto[cursor - 1] === ".") {
    texto = texto.slice(0, cursor - 1) + "," + texto.slice(cursor)
  }
  let valor = limpar(texto, opcoes)
  let antes = Math.min(limpar(texto.slice(0, cursor), opcoes).length, valor.length)
  if (valor === anterior) {
    if (tipoEdicao === "deleteContentBackward" && antes > 0) {
      valor = valor.slice(0, antes - 1) + valor.slice(antes)
      antes--
    } else if (tipoEdicao === "deleteContentForward") {
      valor = valor.slice(0, antes) + valor.slice(antes + 1)
    }
  }
  const formatado = comMilhares(valor)
  let posicao = 0
  for (let falta = antes; posicao < formatado.length && falta > 0; posicao++) {
    if (formatado[posicao] !== ".") falta--
  }
  return { valor, formatado, cursor: posicao }
}
