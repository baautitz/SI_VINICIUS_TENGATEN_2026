"use client";

import * as React from "react";
import { X } from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { cn } from "@/lib/utils";

interface ComboboxContextType {
  multiple?: boolean;
  selectedValues: string[];
  setSelectedValues: (vals: string[]) => void;
  searchText: string;
  setSearchText: (txt: string) => void;
  isOpen: boolean;
  setIsOpen: (open: boolean) => void;
  items: unknown[];
  filteredItems: unknown[];
  focusedIndex: number;
  setFocusedIndex: (idx: number) => void;
  anchorRef: React.RefObject<HTMLDivElement | null>;
  handleSelect: (val: string) => void;
}

const ComboboxContext = React.createContext<ComboboxContextType | null>(null);

interface ComboboxProps {
  multiple?: boolean;
  items: readonly unknown[] | unknown[];
  defaultValue?: string[];
  value?: string[];
  onValueChange?: (value: string[]) => void;
  children: React.ReactNode;
}

export function Combobox({
  multiple = false,
  items,
  defaultValue = [],
  value,
  onValueChange,
  children,
}: ComboboxProps) {
  const [selectedValuesState, setSelectedValuesState] =
    React.useState<string[]>(defaultValue);
  const [searchText, setSearchText] = React.useState("");
  const [isOpen, setIsOpen] = React.useState(false);
  const [focusedIndex, setFocusedIndex] = React.useState(-1);
  const anchorRef = React.useRef<HTMLDivElement>(null);

  const selectedValues = value !== undefined ? value : selectedValuesState;
  const setSelectedValues = (vals: string[]) => {
    if (value === undefined) {
      setSelectedValuesState(vals);
    }
    onValueChange?.(vals);
  };

  const filteredItems = React.useMemo(() => {
    if (!searchText) return [...items];
    return items.filter((item) => {
      const label =
        typeof item === "string"
          ? item
          : String(
              (item as Record<string, unknown>).label ||
                (item as Record<string, unknown>).value ||
                "",
            );
      return label.toLowerCase().includes(searchText.toLowerCase());
    });
  }, [items, searchText]);

  const handleSelect = (val: string) => {
    if (multiple) {
      if (selectedValues.includes(val)) {
        setSelectedValues(selectedValues.filter((v) => v !== val));
      } else {
        setSelectedValues([...selectedValues, val]);
      }
    } else {
      setSelectedValues([val]);
      setIsOpen(false);
    }
    setSearchText("");
  };

  return (
    <ComboboxContext.Provider
      value={{
        multiple,
        selectedValues,
        setSelectedValues,
        searchText,
        setSearchText,
        isOpen,
        setIsOpen,
        items: [...items],
        filteredItems,
        focusedIndex,
        setFocusedIndex,
        anchorRef,
        handleSelect,
      }}
    >
      <Popover open={isOpen} onOpenChange={setIsOpen}>
        {children}
      </Popover>
    </ComboboxContext.Provider>
  );
}

export function useComboboxAnchor() {
  return React.useRef<HTMLDivElement>(null);
}

type ComboboxChipsProps = React.ComponentPropsWithRef<"div">;

export const ComboboxChips = ({
  children,
  className,
  ref,
  ...props
}: ComboboxChipsProps) => {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  return (
    <PopoverTrigger asChild>
      <div
        ref={ref}
        className={cn(
          "border-input bg-background ring-offset-background focus-within:ring-ring flex min-h-8 w-full cursor-text flex-wrap items-center gap-2 rounded-lg border px-2.5 py-0.5 text-base focus-within:ring-2 focus-within:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 md:text-sm",
          className,
        )}
        onClick={(e) => {
          const target = e.target as HTMLElement;
          const isInput = target.tagName === "INPUT";
          const isChip = !!target.closest(".combobox-chip");

          if (isInput || isChip || context.isOpen) {
            e.preventDefault();
          }

          const input = e.currentTarget.querySelector("input");
          if (input) input.focus();
        }}
        {...props}
      >
        {children}
      </div>
    </PopoverTrigger>
  );
};

interface ComboboxValueProps {
  children: (values: string[]) => React.ReactNode;
}

export function ComboboxValue({ children }: ComboboxValueProps) {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  return <>{children(context.selectedValues)}</>;
}

interface ComboboxChipProps extends React.HTMLAttributes<HTMLSpanElement> {
  children: React.ReactNode;
  onRemove?: () => void;
}

export function ComboboxChip({
  children,
  className,
  onRemove,
  ...props
}: ComboboxChipProps) {
  const context = React.useContext(ComboboxContext);

  const handleRemove = (e: React.MouseEvent) => {
    e.stopPropagation();
    e.preventDefault();
    if (onRemove) {
      onRemove();
    } else if (context) {
      const val = typeof children === "string" ? children : "";
      if (val) {
        context.setSelectedValues(
          context.selectedValues.filter((v) => v !== val),
        );
      }
    }
  };

  return (
    <span
      className={cn(
        "combobox-chip bg-secondary text-secondary-foreground inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-medium",
        className,
      )}
      {...props}
    >
      {children}
      <button
        type="button"
        onClick={handleRemove}
        className="focus:ring-ring cursor-pointer rounded-sm opacity-70 hover:opacity-100 focus:ring-1 focus:outline-none"
      >
        <X className="size-3" />
      </button>
    </span>
  );
}

type ComboboxChipsInputProps = React.InputHTMLAttributes<HTMLInputElement>;

export function ComboboxChipsInput({
  className,
  autoComplete = "off",
  ...props
}: ComboboxChipsInputProps) {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Backspace" && !context.searchText) {
      if (context.selectedValues.length > 0) {
        context.setSelectedValues(context.selectedValues.slice(0, -1));
      }
    }
    if (e.key === "Enter") {
      e.preventDefault();
      if (
        context.focusedIndex >= 0 &&
        context.focusedIndex < context.filteredItems.length
      ) {
        const item = context.filteredItems[context.focusedIndex];
        const val =
          typeof item === "string"
            ? item
            : String(
                (item as Record<string, unknown>).value ||
                  (item as Record<string, unknown>).id ||
                  "",
              );
        context.handleSelect(val);
      } else if (context.filteredItems.length === 1) {
        const item = context.filteredItems[0];
        const val =
          typeof item === "string"
            ? item
            : String(
                (item as Record<string, unknown>).value ||
                  (item as Record<string, unknown>).id ||
                  "",
              );
        context.handleSelect(val);
      }
    }
    if (e.key === "ArrowDown") {
      e.preventDefault();
      context.setIsOpen(true);
      context.setFocusedIndex(
        Math.min(context.focusedIndex + 1, context.filteredItems.length - 1),
      );
    }
    if (e.key === "ArrowUp") {
      e.preventDefault();
      context.setFocusedIndex(Math.max(context.focusedIndex - 1, 0));
    }
    props.onKeyDown?.(e);
  };

  return (
    <input
      type="text"
      autoComplete={autoComplete}
      value={context.searchText}
      onChange={(e) => {
        context.setSearchText(e.target.value);
        context.setIsOpen(true);
        context.setFocusedIndex(-1);
      }}
      onFocus={(e) => {
        context.setIsOpen(true);
        props.onFocus?.(e);
      }}
      onKeyDown={handleKeyDown}
      className={cn(
        "min-w-15 flex-1 border-none bg-transparent p-0 text-sm outline-none focus:ring-0",
        className,
      )}
      {...props}
    />
  );
}

interface ComboboxContentProps {
  children: React.ReactNode;
  className?: string;
}

export function ComboboxContent({ children, className }: ComboboxContentProps) {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  return (
    <PopoverContent
      className={cn("w-(--radix-popover-trigger-width) p-1", className)}
      align="start"
      onOpenAutoFocus={(e) => e.preventDefault()}
    >
      {children}
    </PopoverContent>
  );
}

interface ComboboxEmptyProps {
  children: React.ReactNode;
}

export function ComboboxEmpty({ children }: ComboboxEmptyProps) {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  if (context.filteredItems.length > 0) return null;

  return (
    <div className="text-muted-foreground p-2 text-center text-sm">
      {children}
    </div>
  );
}

interface ComboboxListProps<T = unknown> {
  children: (item: T) => React.ReactNode;
}

export function ComboboxList<T = unknown>({ children }: ComboboxListProps<T>) {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  return (
    <div className="flex max-h-50 flex-col gap-0.5 overflow-y-auto p-1">
      {context.filteredItems.map((item) => children(item as T))}
    </div>
  );
}

interface ComboboxItemProps {
  value: string;
  children: React.ReactNode;
  className?: string;
}

export function ComboboxItem({
  value,
  children,
  className,
}: ComboboxItemProps) {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  const isSelected = context.selectedValues.includes(value);
  const isFocused =
    context.focusedIndex >= 0 &&
    context.filteredItems[context.focusedIndex] &&
    (typeof context.filteredItems[context.focusedIndex] === "string"
      ? context.filteredItems[context.focusedIndex] === value
      : String(
          (
            context.filteredItems[context.focusedIndex] as Record<
              string,
              unknown
            >
          ).value ||
            (
              context.filteredItems[context.focusedIndex] as Record<
                string,
                unknown
              >
            ).id ||
            "",
        ) === value);

  return (
    <div
      onClick={() => context.handleSelect(value)}
      className={cn(
        "hover:bg-accent hover:text-accent-foreground data-[focused=true]:bg-accent data-[focused=true]:text-accent-foreground relative flex cursor-pointer items-center rounded-sm px-2 py-1.5 text-sm transition-colors outline-none select-none",
        isSelected && "bg-accent/50 font-medium",
        className,
      )}
      data-focused={isFocused}
    >
      {children}
    </div>
  );
}

interface ComboboxCreateProps {
  onClick: (text: string) => void;
  children: (text: string) => React.ReactNode;
}

export function ComboboxCreate({ onClick, children }: ComboboxCreateProps) {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  const text = context.searchText.trim();
  if (!text) return null;

  const hasExactMatch = context.items.some((item) => {
    const label =
      typeof item === "string"
        ? item
        : String(
            (item as Record<string, unknown>).label ||
              (item as Record<string, unknown>).value ||
              "",
          );
    return label.toLowerCase() === text.toLowerCase();
  });

  if (hasExactMatch) return null;

  return (
    <div
      onClick={() => {
        onClick(text);
        context.setSearchText("");
        context.setIsOpen(false);
      }}
      className="text-primary hover:bg-accent hover:text-accent-foreground relative flex cursor-pointer items-center rounded-sm px-2 py-1.5 text-sm font-medium outline-none select-none"
    >
      {children(text)}
    </div>
  );
}
