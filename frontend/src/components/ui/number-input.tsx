"use client";

import * as React from "react";
import { cn } from "@/lib/utils";
import { comMilhares, editarNumero } from "@/lib/numero";
import { Input, type InputProps } from "./input";

interface NumberInputProps extends Omit<InputProps, "value" | "onChange"> {
  value: string | number;
  onValueChange?: (value: string) => void;
  onNumberChange?: (value: number) => void;
  decimals?: number;
  allowNegative?: boolean;
}

const NumberInput = ({
  className,
  decimals = 2,
  allowNegative = false,
  value: propValue,
  onValueChange,
  onNumberChange,
  ref,
  ...props
}: NumberInputProps) => {
  const normalize = (v: string | number) => {
    if (v === null || v === undefined || v === "") return "";
    return v.toString().replace(/\./g, ",");
  };

  const pad = (v: string) => {
    if (v === "" || decimals <= 0) return v;
    if (!v.includes(",")) {
      return v + "," + "0".repeat(decimals);
    }
    const parts = v.split(",");
    const integerPart = parts[0] || "0";
    const fractionalPart = parts[1].padEnd(decimals, "0");
    return integerPart + "," + fractionalPart.substring(0, decimals);
  };

  const toNum = (v: string | number) => {
    if (typeof v === "number") return v;
    if (!v) return NaN;
    return parseFloat(v.replace(",", "."));
  };

  const [isFocused, setIsFocused] = React.useState(false);
  const [internalValue, setInternalValue] = React.useState(() => {
    if (propValue === "") return "";
    return pad(normalize(propValue));
  });
  const [prevPropValue, setPrevPropValue] = React.useState(propValue);

  if (propValue !== prevPropValue) {
    setPrevPropValue(propValue);

    const pNum = toNum(propValue);
    const iNum = toNum(internalValue);

    if (propValue === "") {
      setInternalValue("");
    } else if (!isNaN(pNum)) {
      if (isNaN(iNum) && pNum === 0) {
        // Campo vazio (ou só com o sinal): o pai recebeu 0 por padrão, mas o
        // usuário ainda está digitando, então não reescreve o texto.
      } else if (isNaN(iNum) || pNum !== iNum) {
        setInternalValue(
          isFocused ? normalize(propValue) : pad(normalize(propValue)),
        );
      }
    }
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const input = e.currentTarget;
    const evento = e.nativeEvent as InputEvent;
    const edicao = editarNumero(
      input.value,
      input.selectionStart ?? input.value.length,
      internalValue,
      { decimais: decimals, negativo: allowNegative },
      evento.data,
      evento.inputType,
    );
    // Corrige texto e cursor no próprio evento: editar no meio não joga o cursor para o fim.
    input.value = edicao.formatado;
    input.setSelectionRange(edicao.cursor, edicao.cursor);

    const val = edicao.valor;
    setInternalValue(val);
    onValueChange?.(val);

    const numericValue = toNum(val);
    onNumberChange?.(isNaN(numericValue) ? 0 : numericValue);
  };

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    setIsFocused(false);
    const val = internalValue;

    if ((val === "" || val === "-") && propValue !== "") {
      // Ao sair com o campo vazio, mostra o zero que o pai já recebeu.
      const zero = pad("0") || "0";
      setInternalValue(zero);
      onValueChange?.(zero);
    } else if (val !== "" && decimals > 0) {
      const padded = pad(val);
      if (padded !== val) {
        setInternalValue(padded);
        onValueChange?.(padded);
        // Do NOT call onNumberChange here — numeric value hasn't changed,
        // calling it would trigger parent re-renders that steal Tab focus.
      }
    }

    props.onBlur?.(e);
  };

  const handleFocus = (e: React.FocusEvent<HTMLInputElement>) => {
    setIsFocused(true);
    props.onFocus?.(e);
  };

  return (
    <Input
      {...props}
      ref={ref}
      type="text"
      value={comMilhares(internalValue)}
      onChange={handleChange}
      onBlur={handleBlur}
      onFocus={handleFocus}
      className={cn("font-mono", className)}
    />
  );
};

export { NumberInput };
