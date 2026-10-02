"use client";

import * as React from "react";
import { cn } from "@/lib/utils";
import { Input, type InputProps } from "./input";

interface NumberInputProps extends Omit<InputProps, "value" | "onChange"> {
  value: string | number;
  onValueChange?: (value: string) => void;
  onNumberChange?: (value: number) => void;
  decimals?: number;
  allowNegative?: boolean;
}

// Máscara de milhares: o estado guarda "1234,50"; a tela mostra "1.234,50".
const withThousands = (v: string) => {
  const [int, frac] = v.split(",");
  const grouped = int.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
  return frac === undefined ? grouped : grouped + "," + frac;
};

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
    const input = e.target;
    const originalValue = input.value;
    const selectionStart = input.selectionStart;

    let val = originalValue;
    // "." digitado vira vírgula; os pontos já presentes são da máscara.
    if (
      (e.nativeEvent as InputEvent).data === "." &&
      selectionStart !== null
    ) {
      val =
        val.slice(0, selectionStart - 1) + "," + val.slice(selectionStart);
    }
    const negative = allowNegative && val.trimStart().startsWith("-");
    val = val.replace(/[^0-9,]/g, "");
    if (negative) val = "-" + val;

    const parts = val.split(",");
    if (parts.length > 2) {
      val = parts[0] + "," + parts.slice(1).join("");
    }

    if (decimals > 0) {
      const splitVal = val.split(",");
      if (splitVal.length === 2 && splitVal[1].length > decimals) {
        val = splitVal[0] + "," + splitVal[1].substring(0, decimals);
      }
    } else {
      val = val.replace(/,/g, "");
    }

    setInternalValue(val);
    onValueChange?.(val);
    
    const numericValue = toNum(val);
    onNumberChange?.(isNaN(numericValue) ? 0 : numericValue);

    if (selectionStart !== null) {
      // Posiciona o cursor após o mesmo número de caracteres significativos.
      let keep = originalValue.slice(0, selectionStart).replace(/[^0-9,-]/g, "").length;
      const shown = withThousands(val);
      let newPosition = 0;
      while (newPosition < shown.length && keep > 0) {
        if (shown[newPosition] !== ".") keep--;
        newPosition++;
      }
      requestAnimationFrame(() => {
        input.setSelectionRange(newPosition, newPosition);
      });
    }
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
      value={withThousands(internalValue)}
      onChange={handleChange}
      onBlur={handleBlur}
      onFocus={handleFocus}
      className={cn("font-mono", className)}
    />
  );
};

export { NumberInput };
