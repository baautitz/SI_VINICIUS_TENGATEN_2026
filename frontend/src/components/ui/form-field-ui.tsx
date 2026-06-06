import React from "react"
import { Field, FieldLabel, FieldError } from "@/components/ui/field"
import { Input, type InputProps } from "@/components/ui/input"
import { NumberInput } from "@/components/ui/number-input"

interface FormFieldUIProps {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  field: any
  label: string
  getFieldError: (name: string, errors: unknown[]) => string | undefined
  inputSize?: "small" | "medium" | "large" | "full"
  type?: React.HTMLInputTypeAttribute
  decimals?: number
}

export function FormFieldUI({
  field,
  label,
  getFieldError,
  inputSize = "full",
  type = "text",
  decimals = 2,
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
      <FieldLabel htmlFor={field.name}>{label}</FieldLabel>
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
