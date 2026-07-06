"use client";

import React from "react";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import { cn } from "@/lib/utils";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface SexoSelectProps {
  name: string;
  label?: string;
  error?: string;
  value: string;
  onChange: (value: string) => void;
  inputSize?: "small" | "medium" | "large" | "full";
  disabled?: boolean;
}

export function SexoSelect({
  name,
  label = "Sexo",
  error,
  value,
  onChange,
  inputSize = "full",
  disabled = false,
}: SexoSelectProps) {
  return (
    <Field data-invalid={!!error}>
      <FieldLabel htmlFor={name}>{label}</FieldLabel>
      <Select
        value={value || "NONE"}
        onValueChange={(val) => onChange(val === "NONE" ? "" : val)}
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
          <SelectValue placeholder="Selecione o sexo..." />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="NONE">Selecione...</SelectItem>
          <SelectItem value="M">Masculino</SelectItem>
          <SelectItem value="F">Feminino</SelectItem>
          <SelectItem value="O">Outro</SelectItem>
        </SelectContent>
      </Select>
      {error && <FieldError>{error}</FieldError>}
    </Field>
  );
}
