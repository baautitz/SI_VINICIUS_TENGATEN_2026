"use client";

import * as React from "react";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import { CalendarIcon } from "lucide-react";

import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
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

export function DatePicker({
  value,
  onChange,
  placeholder = "Selecione uma data",
  disabled = false,
  className,
  inputSize = "full",
}: DatePickerProps) {
  const selectedDate = React.useMemo(() => parseStringDate(value), [value]);

  const handleSelect = React.useCallback(
    (date: Date | undefined) => {
      const formatted = formatToDateString(date);
      onChange(formatted);
    },
    [onChange]
  );

  const displayString = React.useMemo(() => {
    if (!selectedDate) return placeholder;
    return format(selectedDate, "dd/MM/yyyy");
  }, [selectedDate, placeholder]);

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          disabled={disabled}
          className={cn(
            "justify-start text-left font-normal h-10 px-3 py-2 border border-input rounded-md bg-background hover:bg-accent hover:text-accent-foreground focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
            inputSize === "full" && "w-full",
            !value && "text-muted-foreground",
            className
          )}
        >
          <CalendarIcon className="mr-2 h-4 w-4 shrink-0 opacity-50" />
          <span className="truncate">{displayString}</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar
          mode="single"
          selected={selectedDate}
          onSelect={handleSelect}
          locale={ptBR}
        />
      </PopoverContent>
    </Popover>
  );
}
