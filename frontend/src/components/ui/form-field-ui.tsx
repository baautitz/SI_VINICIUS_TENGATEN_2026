import React from "react"
import { Field, FieldLabel, FieldError } from "@/ui/primitives"
import { Input, type InputProps } from "@/ui/primitives"
import { NumberInput } from "@/ui/composites"
import { MaskedInput } from "@/components/ui/masked-input"
import type { Mascara } from "@/lib/mascara"
import { campoObrigatorio } from "@/utils/campo-obrigatorio"

interface FormFieldUIProps {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  field: any
  label: string
  getFieldError: (name: string, errors: unknown[]) => string | undefined
  inputSize?: "small" | "medium" | "large" | "full"
  type?: React.HTMLInputTypeAttribute
  decimals?: number
  required?: boolean
  /** Com máscara, o campo guarda o valor limpo e exibe o formatado. */
  mascara?: Mascara
  /** A máscara vale só enquanto o texto cabe nela (ver MaskedInput). */
  mascaraLivre?: boolean
}

export function FormFieldUI({
  field,
  label,
  getFieldError,
  inputSize = "full",
  type = "text",
  decimals = 2,
  required,
  mascara,
  mascaraLivre,
  onChangeOverride,
  ...props
}: FormFieldUIProps &
  Omit<InputProps, "value" | "onChange"> & {
    onChangeOverride?: (val: string) => unknown
  }) {
  const error = getFieldError(field.name, field.state.meta.errors)

  const isNumeric = type === "number"

  return (
    <Field data-invalid={!!error}>
      <FieldLabel htmlFor={field.name} required={required ?? campoObrigatorio(field)}>{label}</FieldLabel>
      {isNumeric ? (
        <NumberInput
          id={field.name}
          inputSize={inputSize}
          decimals={decimals}
          value={field.state.value}
          onNumberChange={(num) => {
            if (onChangeOverride) {
              field.handleChange(onChangeOverride(num.toString()))
            } else {
              field.handleChange(num)
            }
          }}
          onBlur={field.handleBlur}
          aria-invalid={!!error}
          {...props}
        />
      ) : mascara ? (
        <MaskedInput
          id={field.name}
          mascara={mascara}
          livre={mascaraLivre}
          inputSize={inputSize}
          value={field.state.value as string | null | undefined}
          onBlur={field.handleBlur}
          onValueChange={field.handleChange}
          aria-invalid={!!error}
          {...props}
        />
      ) : (
        <Input
          id={field.name}
          type={type}
          inputSize={inputSize}
          value={(field.state.value ?? "") as string | number}
          onBlur={field.handleBlur}
          onChange={(e) => {
            if (onChangeOverride) {
              field.handleChange(onChangeOverride(e.target.value))
            } else {
              field.handleChange(e.target.value)
            }
          }}
          aria-invalid={!!error}
          {...props}
        />
      )}
      {error && <FieldError>{error}</FieldError>}
    </Field>
  )
}
