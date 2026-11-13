"use client"

import { aplicarMascara, editarMascara, limparMascara, mascaraNumerica, type Mascara } from "@/lib/mascara"
import { Input, type InputProps } from "./input"

interface MaskedInputProps extends Omit<InputProps, "value" | "onChange"> {
  mascara: Mascara
  /** Valor limpo (sem máscara); um valor já formatado também é aceito. */
  value: string | null | undefined
  /** Recebe sempre o valor limpo, que é o que vai para o estado e para a API. */
  onValueChange: (limpo: string) => void
  /** A máscara vale só enquanto o texto cabe nela; o que não cabe fica como texto livre. */
  livre?: boolean
}

/**
 * Input com máscara. Sem estado próprio: a tela é sempre aplicarMascara(value).
 * O texto e o cursor são corrigidos no próprio evento, antes do React renderizar,
 * então editar no meio não joga o cursor para o fim.
 */
function MaskedInput({ mascara, value, onValueChange, livre = false, ...props }: MaskedInputProps) {
  return (
    <Input
      inputMode={mascaraNumerica(mascara) ? "numeric" : undefined}
      {...props}
      value={aplicarMascara(value, mascara, livre)}
      onChange={(event) => {
        const el = event.currentTarget
        const edicao = editarMascara(
          el.value,
          el.selectionStart ?? el.value.length,
          mascara,
          limparMascara(value, mascara),
          (event.nativeEvent as InputEvent).inputType,
          livre,
        )
        el.value = edicao.formatado
        el.setSelectionRange(edicao.cursor, edicao.cursor)
        onValueChange(edicao.limpo)
      }}
    />
  )
}

export { MaskedInput }
