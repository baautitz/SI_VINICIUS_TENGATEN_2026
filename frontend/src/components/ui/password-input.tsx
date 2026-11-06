import * as React from "react"
import { EyeIcon, EyeOffIcon, TriangleAlertIcon } from "lucide-react"

import {
  InputGroup,
  InputGroupAddon,
  InputGroupButton,
  InputGroupInput,
} from "@/components/ui/input-group"
import type { InputProps } from "@/components/ui/input"

type PasswordInputProps = Omit<InputProps, "type">

/** Campo de senha: nao converte para maiusculas e avisa quando o Caps Lock esta ligado. */
const PasswordInput = ({ onKeyDown, onKeyUp, onBlur, ...props }: PasswordInputProps) => {
  const [visible, setVisible] = React.useState(false)
  const [capsLock, setCapsLock] = React.useState(false)

  return (
    <div className="flex w-full flex-col gap-1">
      <InputGroup>
        <InputGroupInput
          {...props}
          type={visible ? "text" : "password"}
          autoComplete={props.autoComplete ?? "current-password"}
          onKeyDown={(e) => {
            setCapsLock(e.getModifierState("CapsLock"))
            onKeyDown?.(e)
          }}
          onKeyUp={(e) => {
            setCapsLock(e.getModifierState("CapsLock"))
            onKeyUp?.(e)
          }}
          onBlur={(e) => {
            setCapsLock(false)
            onBlur?.(e)
          }}
        />
        <InputGroupAddon align="inline-end">
          <InputGroupButton
            type="button"
            size="icon-xs"
            aria-label={visible ? "Ocultar senha" : "Mostrar senha"}
            onClick={() => setVisible((v) => !v)}
          >
            {visible ? <EyeOffIcon /> : <EyeIcon />}
          </InputGroupButton>
        </InputGroupAddon>
      </InputGroup>
      {capsLock && (
        <p role="status" className="flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400">
          <TriangleAlertIcon className="size-3" />
          Caps Lock ligado
        </p>
      )}
    </div>
  )
}

export { PasswordInput }
