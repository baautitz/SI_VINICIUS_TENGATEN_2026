import React from "react"
import { Field, FieldLabel, FieldError } from "@/components/ui/field"
import { Input } from "@/components/ui/input"

interface FormFieldUIProps {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  field: any
  label: string
  getFieldError: (name: string, errors: unknown[]) => string | undefined
  inputSize?: "small" | "medium" | "large" | "full"
  type?: React.HTMLInputTypeAttribute
}

export function FormFieldUI({
  field,
  label,
  getFieldError,
  inputSize = "full",
  type = "text",
  onChangeOverride,
  ...props
}: FormFieldUIProps & React.InputHTMLAttributes<HTMLInputElement> & {
  onChangeOverride?: (val: string) => unknown
}) {
  const error = getFieldError(field.name, field.state.meta.errors)
  return (
    <Field data-invalid={!!error}>
      <FieldLabel htmlFor={field.name}>{label}</FieldLabel>
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
            field.handleChange(
              type === "number" ? Number(e.target.value) : e.target.value
            )
          }
        }}
        aria-invalid={!!error}
        {...props}
      />
      {error && <FieldError>{error}</FieldError>}
    </Field>
  )
}
