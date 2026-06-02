"use client"

import React from "react"
import { TipoPessoa } from "@/api/types"
import { Field, FieldLabel, FieldError } from "@/components/ui/field"
import { inputVariants } from "@/components/ui/input"
import { cn } from "@/lib/utils"

interface TipoPessoaSelectProps {
  name: string
  label?: string
  error?: string
  value: TipoPessoa
  onChange: (value: TipoPessoa) => void
  inputSize?: "small" | "medium" | "large" | "full"
}

export function TipoPessoaSelect({
  name,
  label = "Tipo de Pessoa",
  error,
  value,
  onChange,
  inputSize = "full"
}: TipoPessoaSelectProps) {
  return (
    <Field data-invalid={!!error}>
      <FieldLabel htmlFor={name}>{label}</FieldLabel>
      <select
        id={name}
        className={cn(
          inputVariants({ inputSize }),
          "h-8 py-0 appearance-none bg-[url('data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20fill%3D%22none%22%20viewBox%3D%220%200%2020%2020%22%3E%3Cpath%20stroke%3D%22%236b7280%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%20stroke-width%3D%221.5%22%20d%3D%22m6%208%204%204%204-4%22%2F%3E%3C%2Fsvg%3E')] bg-[position:right_0.5rem_center] bg-[length:1.25rem_1.25rem] bg-no-repeat pr-8"
        )}
        value={value}
        onChange={(e) => onChange(Number(e.target.value) as TipoPessoa)}
      >
        <option value={TipoPessoa.FISICA}>Física</option>
        <option value={TipoPessoa.JURIDICA}>Jurídica</option>
      </select>
      {error && <FieldError>{error}</FieldError>}
    </Field>
  )
}
