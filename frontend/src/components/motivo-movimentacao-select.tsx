"use client";

import { Field, FieldError, FieldLabel } from "@/ui/primitives";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/ui/primitives";
import { cn } from "@/lib/utils";
import { motivoMovimentacaoLabels, type MotivoMovimentacao } from "@/features/estoque/motivos";

interface MotivoMovimentacaoSelectProps {
  name: string;
  label?: string;
  /** Lista de motivos que o contexto aceita (saída manual, falta ou sobra). */
  opcoes: MotivoMovimentacao[];
  value: string | null | undefined;
  onChange: (value: MotivoMovimentacao) => void;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  /** Controle compacto para dentro de tabelas. */
  compacto?: boolean;
  className?: string;
  /** Atributos do gatilho, como a posição na grade de navegação por teclado. */
  triggerProps?: Record<string, string>;
}

export function MotivoMovimentacaoSelect({
  name,
  label = "Motivo da movimentação",
  opcoes,
  value,
  onChange,
  error,
  required,
  disabled = false,
  compacto = false,
  className,
  triggerProps,
}: MotivoMovimentacaoSelectProps) {
  const select = (
    <Select value={value ?? ""} onValueChange={(v) => onChange(v as MotivoMovimentacao)} disabled={disabled}>
      <SelectTrigger id={name} aria-label={label} aria-invalid={!!error} className={cn("h-8 w-full rounded-lg", className)} {...triggerProps}>
        <SelectValue placeholder="Selecione o motivo..." />
      </SelectTrigger>
      <SelectContent>
        {opcoes.map((m) => (
          <SelectItem key={m} value={m}>
            {motivoMovimentacaoLabels[m]}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );

  if (compacto) return select;

  return (
    <Field data-invalid={!!error}>
      <FieldLabel htmlFor={name} required={required}>
        {label}
      </FieldLabel>
      {select}
      {error && <FieldError>{error}</FieldError>}
    </Field>
  );
}
