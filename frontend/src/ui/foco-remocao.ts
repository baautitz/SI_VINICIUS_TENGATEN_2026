import { markKeyboardFocus } from "./keyboard-navigation"

const BOTAO_REMOVER = 'button[aria-label^="Remover"], button[aria-label="Excluir"]'

function linhasRemoviveis(tabela: Element | null | undefined): HTMLElement[] {
  if (!tabela?.isConnected) return []
  return Array.from(tabela.querySelectorAll<HTMLElement>("tbody tr")).filter((linha) =>
    linha.querySelector(BOTAO_REMOVER),
  )
}

/**
 * Foco depois de remover uma linha de uma tabela: vai para a lixeira da linha mais proxima
 * (a que ocupa o lugar da removida ou, se era a ultima, a anterior). Sem linhas, chama `semLinhas`
 * (por exemplo, voltar ao campo de SKU). Chame no clique, antes de confirmar; execute o retorno
 * quando a remocao terminar (ou for cancelada: o foco volta para a mesma lixeira).
 */
export function capturarFocoDeRemocao(semLinhas?: () => void): () => void {
  const origem = document.activeElement
  const tabela = origem?.closest("table")
  const indice = linhasRemoviveis(tabela).indexOf(origem?.closest("tr") as HTMLElement)

  return () => {
    // Dois quadros: o estado e a lista refeita precisam estar no DOM antes de procurar a lixeira.
    requestAnimationFrame(() =>
      requestAnimationFrame(() => {
        const linhas = linhasRemoviveis(tabela)
        const alvo = linhas[Math.min(Math.max(indice, 0), linhas.length - 1)]
        const botao = alvo?.querySelector<HTMLElement>(BOTAO_REMOVER)
        if (botao) {
          markKeyboardFocus()
          botao.focus()
        } else semLinhas?.()
      }),
    )
  }
}
