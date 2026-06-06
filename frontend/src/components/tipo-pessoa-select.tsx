"use client";

import React from "react";
import { TipoPessoa } from "@/api/types";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import { cn } from "@/lib/utils";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface TipoPessoaSelectProps {
  name: string;
  label?: string;
  error?: string;
  value: TipoPessoa;
  onChange: (value: TipoPessoa) => void;
  inputSize?: "small" | "medium" | "large" | "full";
  disabled?: boolean;
}

export function TipoPessoaSelect({
  name,
  label = "Tipo de Pessoa",
  error,
  value,
  onChange,
  inputSize = "full",
  disabled = false,
}: TipoPessoaSelectProps) {
  const stringValue = String(value);

  return (
    <Field data-invalid={!!error}>
      <FieldLabel htmlFor={name}>{label}</FieldLabel>
      <Select
        value={stringValue}
        onValueChange={(val) => onChange(Number(val) as TipoPessoa)}
        disabled={disabled}
      >
        <SelectTrigger
          id={name}
          className={cn(
            inputSize === "full"
              ? "w-full"
              : inputSize === "large"
                ? "w-64"
                : inputSize === "medium"
                  ? "w-48"
                  : "w-32",
            "h-8 rounded-lg",
          )}
        >
          <SelectValue placeholder="Selecione o tipo..." />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={String(TipoPessoa.FISICA)}>Física</SelectItem>
          <SelectItem value={String(TipoPessoa.JURIDICA)}>Jurídica</SelectItem>
        </SelectContent>
      </Select>
      {error && <FieldError>{error}</FieldError>}
    </Field>
  );
}
