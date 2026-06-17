"use client";

import * as React from "react";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import { CalendarIcon } from "lucide-react";

import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";

interface DatePickerProps {
  value?: string | null;
  onChange: (value: string | null) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  inputSize?: "sm" | "default" | "lg" | "full";
}

const parseStringDate = (dateStr: string | null | undefined): Date | undefined => {
  if (!dateStr) return undefined;
  const parts = dateStr.split("-");
  if (parts.length !== 3) return undefined;
  const year = parseInt(parts[0], 10);
  const month = parseInt(parts[1], 10) - 1;
  const day = parseInt(parts[2], 10);
  if (isNaN(year) || isNaN(month) || isNaN(day)) return undefined;
  return new Date(year, month, day);
};

const formatToDateString = (date: Date | undefined): string | null => {
  if (!date) return null;
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};

const parseBrDateToIso = (brDateStr: string): string | null => {
  const parts = brDateStr.split("/");
  if (parts.length !== 3) return null;
  const day = parseInt(parts[0], 10);
  const month = parseInt(parts[1], 10);
  const year = parseInt(parts[2], 10);
  if (isNaN(day) || isNaN(month) || isNaN(year)) return null;

  if (day < 1 || day > 31 || month < 1 || month > 12 || year < 1000 || year > 9999) return null;

  const dateObj = new Date(year, month - 1, day);
  if (dateObj.getFullYear() !== year || dateObj.getMonth() !== month - 1 || dateObj.getDate() !== day) {
    return null;
  }

  const mStr = String(month).padStart(2, "0");
  const dStr = String(day).padStart(2, "0");
  return `${year}-${mStr}-${dStr}`;
};

export function DatePicker({
  value,
  onChange,
  placeholder = "DD/MM/AAAA",
  disabled = false,
  className,
  inputSize = "full",
}: DatePickerProps) {
  const [inputValue, setInputValue] = React.useState("");

  const selectedDate = React.useMemo(() => parseStringDate(value), [value]);

  // Sync internal state when parent value changes
  React.useEffect(() => {
    if (value && value !== "invalid-date") {
      const parsed = parseStringDate(value);
      if (parsed) {
        setInputValue(format(parsed, "dd/MM/yyyy"));
        return;
      }
    }
    if (!value) {
      setInputValue("");
    }
  }, [value]);

  const handleSelect = React.useCallback(
    (date: Date | undefined) => {
      const formatted = formatToDateString(date);
      onChange(formatted);
      if (date) {
        setInputValue(format(date, "dd/MM/yyyy"));
      } else {
        setInputValue("");
      }
    },
    [onChange]
  );

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const rawVal = e.target.value;
    const clean = rawVal.replace(/\D/g, "");
    let formatted = "";

    if (clean.length <= 2) {
      formatted = clean;
    } else if (clean.length <= 4) {
      formatted = `${clean.slice(0, 2)}/${clean.slice(2)}`;
    } else {
      formatted = `${clean.slice(0, 2)}/${clean.slice(2, 4)}/${clean.slice(4, 8)}`;
    }

    setInputValue(formatted);

    if (clean.length === 8) {
      const iso = parseBrDateToIso(formatted);
      if (iso) {
        onChange(iso);
      } else {
        onChange("invalid-date");
      }
    } else if (clean.length === 0) {
      onChange(null);
    } else {
      onChange(""); // Mark as incomplete
    }
  };

  const isSmall = className?.includes("h-8") || className?.includes("text-xs");
  const hasError = className?.includes("border-destructive");

  return (
    <div className={cn("flex gap-1 items-center", inputSize === "full" && "w-full")}>
      <Input
        type="text"
        value={inputValue}
        onChange={handleInputChange}
        placeholder={placeholder}
        disabled={disabled}
        maxLength={10}
        inputMode="numeric"
        className={cn(
          "flex-1",
          isSmall ? "h-8 text-xs px-2" : "h-10 text-sm px-3",
          hasError && "border-destructive focus-visible:ring-destructive"
        )}
      />
      <Popover>
        <PopoverTrigger asChild>
          <Button
            type="button"
            variant="outline"
            size="icon"
            disabled={disabled}
            className={cn(
              "shrink-0",
              isSmall ? "h-8 w-8" : "h-10 w-10"
            )}
          >
            <CalendarIcon className="h-4 w-4 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="end">
          <Calendar
            mode="single"
            selected={selectedDate}
            onSelect={handleSelect}
            locale={ptBR}
          />
        </PopoverContent>
      </Popover>
    </div>
  );
}
