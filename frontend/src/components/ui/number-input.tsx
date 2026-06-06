"use client";

import * as React from "react";
import { cn } from "@/lib/utils";
import { Input, type InputProps } from "./input";

interface NumberInputProps extends Omit<InputProps, "value" | "onChange"> {
  value: string | number;
  onValueChange?: (value: string) => void;
  onNumberChange?: (value: number) => void;
  decimals?: number;
}

const NumberInput = ({
  className,
  decimals = 2,
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
      if (isNaN(iNum) || pNum !== iNum) {
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
    val = val.replace(/\./g, ",");
    val = val.replace(/[^0-9,]/g, "");

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
      const diff = val.length - originalValue.length;
      const newPosition = Math.max(0, selectionStart + diff);
      requestAnimationFrame(() => {
        input.setSelectionRange(newPosition, newPosition);
      });
    }
  };

  const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
    setIsFocused(false);
    let val = internalValue;

    if (val !== "" && decimals > 0) {
      val = pad(val);
      setInternalValue(val);
      onValueChange?.(val);
      
      const numericValue = toNum(val);
      onNumberChange?.(isNaN(numericValue) ? 0 : numericValue);
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
      value={internalValue}
      onChange={handleChange}
      onBlur={handleBlur}
      onFocus={handleFocus}
      className={cn("font-mono", className)}
    />
  );
};

export { NumberInput };
