"use client";

import * as React from "react";
import { X } from "lucide-react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/ui/primitives";
import { cn } from "@/lib/utils";
import { selectInputTextOnFocus } from "@/ui/keyboard-navigation";

interface ComboboxContextType {
  multiple?: boolean;
  disabled: boolean;
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
  canCreate: boolean;
  createOption: () => void;
  activeChipIndex: number | null;
  setActiveChipIndex: (idx: number | null) => void;
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
  onCreateOption?: (text: string) => void;
  disabled?: boolean;
  children: React.ReactNode;
}

export function Combobox({
  multiple = false,
  items,
  defaultValue = [],
  value,
  onValueChange,
  onCreateOption,
  disabled = false,
  children,
}: ComboboxProps) {
  const [selectedValuesState, setSelectedValuesState] =
    React.useState<string[]>(defaultValue);
  const [searchText, setSearchText] = React.useState("");
  const [isOpen, setIsOpen] = React.useState(false);
  const [focusedIndex, setFocusedIndex] = React.useState(-1);
  const [activeChipIndex, setActiveChipIndex] = React.useState<number | null>(null);
  const anchorRef = React.useRef<HTMLDivElement>(null);

  const selectedValues = value !== undefined ? value : selectedValuesState;
  const createText = searchText.trim();
  const hasExactMatch = React.useMemo(
    () =>
      items.some((item) => {
        const label =
          typeof item === "string"
            ? item
            : String(
                (item as Record<string, unknown>).label ||
                  (item as Record<string, unknown>).value ||
                  "",
              );
        return label.toLowerCase() === createText.toLowerCase();
      }),
    [createText, items],
  );
  const canCreate = Boolean(!disabled && onCreateOption && createText && !hasExactMatch);
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
    if (disabled) return;
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
    setActiveChipIndex(null);
  };

  const createOption = () => {
    if (disabled || !onCreateOption || !canCreate) return;

    onCreateOption(createText);
    setSearchText("");
    setFocusedIndex(-1);
    setActiveChipIndex(null);
    setIsOpen(false);
  };

  return (
    <ComboboxContext.Provider
      value={{
        multiple,
        disabled,
        selectedValues,
        setSelectedValues,
        searchText,
        setSearchText,
        isOpen,
        setIsOpen: (open) => {
          if (!disabled) setIsOpen(open);
        },
        items: [...items],
        filteredItems,
        focusedIndex,
        setFocusedIndex,
        canCreate,
        createOption,
        activeChipIndex,
        setActiveChipIndex,
        anchorRef,
        handleSelect,
      }}
    >
      <Popover open={isOpen} onOpenChange={(open) => !disabled && setIsOpen(open)}>
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
          const chip = target.closest<HTMLElement>("[data-combobox-chip-index]");
          const isChip = !!chip;

          if (isInput || isChip || context.isOpen) {
            e.preventDefault();
          }

          const input = e.currentTarget.querySelector("input");
          if (input && !context.disabled && !input.disabled && !input.readOnly) {
            input.dataset.navigationEditing = "true";
            input.focus();
            context.setActiveChipIndex(
              chip ? Number(chip.dataset.comboboxChipIndex) : null,
            );
            if (chip && !context.searchText) {
              input.setSelectionRange(0, 0);
            }
            if (
              !context.searchText &&
              context.focusedIndex < 0 &&
              context.filteredItems.length > 0
            ) {
              context.setFocusedIndex(0);
            }
          }
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
  chipIndex?: number;
  onRemove?: () => void;
  removeLabel?: string;
}

export function ComboboxChip({
  children,
  chipIndex,
  className,
  onRemove,
  removeLabel,
  ...props
}: ComboboxChipProps) {
  const context = React.useContext(ComboboxContext);

  const remove = () => {
    if (context?.disabled) return;

    if (onRemove) {
      onRemove();
      return;
    }

    if (!context || typeof children !== "string") return;

    context.setSelectedValues(
      context.selectedValues.filter((value) => value !== children),
    );
  };

  const handleRemovePointerDown = (event: React.PointerEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.stopPropagation();
  };

  const handleRemoveClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    event.preventDefault();
    event.stopPropagation();
    remove();
  };

  const handleRemoveKeyDown = (event: React.KeyboardEvent<HTMLButtonElement>) => {
    if (event.key !== "Delete" && event.key !== "Backspace") return;

    event.preventDefault();
    event.stopPropagation();
    remove();
  };

  return (
    <span
      data-combobox-chip-index={chipIndex}
      className={cn(
        "combobox-chip bg-secondary text-secondary-foreground inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-medium",
        context?.activeChipIndex === chipIndex &&
          "ring-ring ring-1 ring-offset-1",
        className,
      )}
      {...props}
    >
      {children}
      <button
        type="button"
        disabled={context?.disabled}
        data-navigation-chip-remove="true"
        tabIndex={-1}
        aria-label={removeLabel ?? `Remover ${typeof children === "string" ? children : "item"}`}
        onPointerDown={handleRemovePointerDown}
        onClick={handleRemoveClick}
        onKeyDown={handleRemoveKeyDown}
        className="focus:ring-ring cursor-pointer rounded-sm opacity-70 hover:opacity-100 focus:ring-1 focus:outline-none disabled:cursor-not-allowed disabled:opacity-40"
      >
        <X className="size-3" aria-hidden="true" />
      </button>
    </span>
  );
}

type ComboboxChipsInputProps = React.InputHTMLAttributes<HTMLInputElement>;

export function ComboboxChipsInput({
  className,
  autoComplete = "off",
  onFocus,
  onKeyDown,
  onClick,
  ...props
}: ComboboxChipsInputProps) {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  const getItemValue = (item: unknown) =>
    typeof item === "string"
      ? item
      : String(
          (item as Record<string, unknown>).value ||
            (item as Record<string, unknown>).id ||
            "",
        );

  const selectItemAt = (index: number) => {
    if (context.canCreate && index === context.filteredItems.length) {
      context.createOption();
      return true;
    }
    if (index < 0 || index >= context.filteredItems.length) return false;
    context.handleSelect(getItemValue(context.filteredItems[index]));
    return true;
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    const isCursorMode = e.currentTarget.dataset.navigationEditing === "true";

    const removeChipAt = (index: number) => {
      if (index < 0 || index >= context.selectedValues.length) return;

      context.setSelectedValues(
        context.selectedValues.filter((_, chipIndex) => chipIndex !== index),
      );
      context.setActiveChipIndex(
        context.selectedValues.length <= 1
          ? null
          : Math.min(index, context.selectedValues.length - 2),
      );
    };

    const isCaretAtStart =
      e.currentTarget.selectionStart === 0 && e.currentTarget.selectionEnd === 0;

    if (isCursorMode && isCaretAtStart) {
      if (e.key === "ArrowLeft" && context.selectedValues.length > 0) {
        e.preventDefault();
        context.setActiveChipIndex(
          context.activeChipIndex === null
            ? context.selectedValues.length - 1
            : Math.max(context.activeChipIndex - 1, 0),
        );
        return;
      }

      if (e.key === "ArrowRight" && context.activeChipIndex !== null) {
        e.preventDefault();
        if (context.activeChipIndex >= context.selectedValues.length - 1) {
          context.setActiveChipIndex(null);
          requestAnimationFrame(() => {
            if (document.activeElement === e.currentTarget) {
              e.currentTarget.setSelectionRange(0, 0);
            }
          });
        } else {
          context.setActiveChipIndex(context.activeChipIndex + 1);
        }
        return;
      }

      if (e.key === "Backspace" && context.searchText === "") {
        e.preventDefault();
        removeChipAt(
          context.activeChipIndex ?? context.selectedValues.length - 1,
        );
        return;
      }

      if (e.key === "Delete" && context.searchText === "") {
        e.preventDefault();
        removeChipAt(context.activeChipIndex ?? 0);
        return;
      }
    }

    if (
      e.key === "Backspace" &&
      !context.searchText &&
      context.isOpen &&
      context.selectedValues.length > 0
    ) {
      context.setSelectedValues(context.selectedValues.slice(0, -1));
    }

    const isSpace = e.key === " " || e.code === "Space";
    if (isSpace && context.isOpen && context.focusedIndex >= 0) {
      if (selectItemAt(context.focusedIndex)) {
        e.preventDefault();
        onKeyDown?.(e);
        return;
      }
    }

    if (e.key === "Enter" && !e.altKey && !e.ctrlKey && !e.metaKey && !context.isOpen) {
      e.preventDefault();
      context.setIsOpen(true);
      context.setFocusedIndex(
        context.filteredItems.length > 0 || context.canCreate ? 0 : -1,
      );
      onKeyDown?.(e);
      return;
    }
    if (e.key === "Enter" && !e.altKey && !e.ctrlKey && !e.metaKey) {
      e.preventDefault();
      if (!selectItemAt(context.focusedIndex) && context.filteredItems.length === 1) {
        selectItemAt(0);
      }
    }
    if (e.key === "ArrowDown" && context.isOpen) {
      e.preventDefault();
      const lastFocusableIndex =
        context.filteredItems.length - 1 + (context.canCreate ? 1 : 0);
      context.setFocusedIndex(
        lastFocusableIndex < 0
          ? -1
          : Math.min(context.focusedIndex + 1, lastFocusableIndex),
      );
    }
    if (e.key === "ArrowUp" && context.isOpen) {
      e.preventDefault();
      const lastFocusableIndex =
        context.filteredItems.length - 1 + (context.canCreate ? 1 : 0);
      context.setFocusedIndex(
        lastFocusableIndex < 0
          ? -1
          : context.focusedIndex < 0
            ? lastFocusableIndex
            : Math.max(context.focusedIndex - 1, 0),
      );
    }
    onKeyDown?.(e);
  };

  return (
    <input
      type="text"
      autoComplete={autoComplete}
      value={context.searchText}
      onChange={(e) => {
        context.setSearchText(e.target.value);
        context.setActiveChipIndex(null);
        context.setIsOpen(true);
        context.setFocusedIndex(-1);
      }}
      onFocus={(e) => {
        onFocus?.(e);
        if (e.currentTarget.dataset.navigationEditing !== "true") {
          context.setActiveChipIndex(null);
        }
        selectInputTextOnFocus(e.currentTarget);
      }}
      onClick={(e) => {
        context.setIsOpen(true);
        if (
          !context.searchText &&
          context.focusedIndex < 0 &&
          context.filteredItems.length > 0
        ) {
          context.setFocusedIndex(0);
        }
        onClick?.(e);
      }}
      onKeyDown={handleKeyDown}
      data-navigation-popup-open={context.isOpen ? "true" : undefined}
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
  children: (text: string) => React.ReactNode;
}

export function ComboboxCreate({ children }: ComboboxCreateProps) {
  const context = React.useContext(ComboboxContext);
  if (!context) return null;

  const text = context.searchText.trim();
  if (!context.canCreate) return null;

  return (
    <div
      onClick={context.createOption}
      data-focused={context.focusedIndex === context.filteredItems.length}
      className="text-primary hover:bg-accent hover:text-accent-foreground data-[focused=true]:bg-accent data-[focused=true]:text-accent-foreground relative flex cursor-pointer items-center rounded-sm px-2 py-1.5 text-sm font-medium outline-none select-none"
    >
      {children(text)}
    </div>
  );
}
