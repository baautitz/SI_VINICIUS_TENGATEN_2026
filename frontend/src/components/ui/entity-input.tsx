"use client";

import React, { useState, useRef } from "react";
import { Search } from "lucide-react";
import { Button } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { Field, FieldLabel, FieldError } from "@/ui/primitives";
import { useWindow, useUi } from "@/ui/imperative";

interface EntityInputProps<T, TResumo = T, TId extends string | number = number> {
  name: string;
  label: string;
  placeholder?: string;
  error?: string;
  initialItem?: T | TResumo | null;
  disabled?: boolean;

  onSelectId: (id: TId | null) => void;
  onSelectItem?: (item: T | null) => void;

  fetchById: (id: TId) => Promise<T | null>;
  fetchList: (term: string) => Promise<{ itens?: TResumo[] } | null>;
  getDisplayLabel: (item: T | TResumo) => string;
  getSearchTerm: (item: TResumo) => string;
  getId: (item: T | TResumo) => TId;

  modalTitle: string;
  /** Ícone compartilhado pela listagem quando ela é página e quando é seleção modal. */
  icon?: React.ReactNode;
  renderFeature: (props: {
    selectionMode: boolean;
    onSelect: (item: TResumo) => void;
    initialSearchTerm: string;
    icon?: React.ReactNode;
  }) => React.ReactNode;
}

type EntitySelectionWindowProps<T, TResumo> = Pick<
  EntityInputProps<T, TResumo>,
  "renderFeature"
> & {
  initialSearchTerm: string;
  icon?: React.ReactNode;
};

function EntitySelectionWindow<T, TResumo>({
  renderFeature,
  initialSearchTerm,
  icon,
}: EntitySelectionWindowProps<T, TResumo>) {
  const activeWindow = useWindow<TResumo>();

  return renderFeature({
    selectionMode: true,
    onSelect: (item) => activeWindow.resolve(item),
    initialSearchTerm,
    icon,
  });
}

export function EntityInput<T, TResumo = T, TId extends string | number = number>({
  name,
  label,
  placeholder = "Digite, ou Enter para buscar...",
  error,
  initialItem = null,
  disabled = false,
  onSelectId,
  onSelectItem,
  fetchById,
  fetchList,
  getDisplayLabel,
  getSearchTerm,
  getId,
  modalTitle,
  icon,
  renderFeature,
}: EntityInputProps<T, TResumo, TId>) {
  const ui = useUi();
  const [selectedItem, setSelectedItem] = useState<TResumo | null>(
    (initialItem as TResumo | null) ?? null,
  );

  const getLabel = (item: T | TResumo | null) => {
    if (!item) return "";
    return getDisplayLabel(item);
  };

  const initialLabel = getLabel(initialItem);
  const [searchText, setSearchText] = useState(initialLabel ?? "");
  const [selectedLabel, setSelectedLabel] = useState(initialLabel ?? "");
  const [prevInitialItem, setPrevInitialItem] = useState(initialItem);
  const inputRef = useRef<HTMLInputElement>(null);
  const openingSelectorRef = useRef(false);
  const searchInFlightRef = useRef(false);

  // The value shown in the field is authoritative when the user has edited
  // it.  When it is still the selected item's display label, use the entity's
  // canonical search field instead (for example, "BRASIL" instead of
  // "BRASIL (BRA)").  Without this distinction a dialog opened after typing
  // would search the stale selected item and could appear to return no rows.
  const getSelectionSearchTerm = () => {
    // Read the DOM value as well as React state. A click on the search icon can
    // arrive in the same event turn as the final input change, before the
    // controlled state commit is observable by this callback.
    const visibleText = inputRef.current?.value ?? searchText;
    return selectedItem && visibleText === selectedLabel
      ? getSearchTerm(selectedItem)
      : visibleText;
  };

  const openSelection = async () => {
    // Enter, blur and the search button can converge while the lookup request
    // is still pending. Only the first caller may create a selector window;
    // later callers are ignored until that interaction finishes.
    if (disabled || openingSelectorRef.current) return;
    openingSelectorRef.current = true;

    try {
      const result = await ui.windows.open<TResumo, EntitySelectionWindowProps<T, TResumo>>({
        component: EntitySelectionWindow<T, TResumo>,
        props: {
          renderFeature,
          icon,
          initialSearchTerm: getSelectionSearchTerm(),
        },
        title: modalTitle,
        icon,
        size: "full",
      });

      if (result.status === "confirmed") {
        await applySelection(result.value);
      }
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: `Não foi possível abrir ${modalTitle.toLowerCase()}.`,
      });
    } finally {
      openingSelectorRef.current = false;
    }
  };

  if (initialItem !== prevInitialItem) {
    setPrevInitialItem(initialItem);
    const newLabel = getLabel(initialItem);
    setSelectedItem((initialItem as TResumo | null) ?? null);
    setSearchText(newLabel ?? "");
    setSelectedLabel(newLabel ?? "");
  }

  const handleSearch = async (text: string, isBlur = false) => {
    // Enter and blur may invoke this function concurrently. Serialize the
    // complete lookup/selection flow so a second request cannot open another
    // selector or apply a stale result.
    if (searchInFlightRef.current) return;
    searchInFlightRef.current = true;

    try {
      if (!text.trim()) {
        if (!isBlur) {
          // Enter (or the search button) opens the selector. It must not clear
          // the field first: clearing here sends `null` to the parent form and
          // runs its required-field validator before the user has selected
          // anything. An explicit clear is still handled on blur below.
          await openSelection();
          return;
        }

        onSelectId(null);
        onSelectItem?.(null);
        setSelectedItem(null);
        setSearchText("");
        setSelectedLabel("");
        return;
      }
      if (isBlur && selectedLabel === text) return;

      try {
        const textTrimmed = text.trim();
        const numericId = parseInt(textTrimmed, 10);
        if (!isNaN(numericId) && /^\d+$/.test(textTrimmed)) {
          try {
            const matched = await fetchById(numericId as TId);
            if (matched) {
              await applySelection(matched, isBlur);
              return;
            }
          } catch {}
        }

        const listRes = await fetchList(text);
        if (listRes?.itens && listRes.itens.length === 1) {
          const matched = await fetchById(getId(listRes.itens[0]));
          if (matched) {
            await applySelection(listRes.itens[0], isBlur);
            return;
          }
        }

        if (isBlur) {
          setSearchText(selectedLabel);
        } else {
          await openSelection();
        }
      } catch {
        if (isBlur) {
          setSearchText(selectedLabel);
        } else {
          await openSelection();
        }
      }
    } finally {
      searchInFlightRef.current = false;
    }
  };

  const applySelection = async (item: T | TResumo, isBlur = false) => {
    const itemId = getId(item);
    const fullItem = await fetchById(itemId);
    if (fullItem) {
      const newLabel = getDisplayLabel(fullItem);
      onSelectId(itemId);
      onSelectItem?.(fullItem);
      setSelectedItem(item as TResumo);
      setSearchText(newLabel);
      setSelectedLabel(newLabel);
    }
    if (!isBlur) inputRef.current?.focus();
  };

  return (
    <>
      <Field data-invalid={!!error}>
        <FieldLabel htmlFor={name}>{label}</FieldLabel>
        <div className={`relative w-full`}>
          <Input
            ref={inputRef}
            id={name}
            placeholder={placeholder}
            value={searchText}
            disabled={disabled}
            onChange={(e) => setSearchText(e.target.value)}
            onKeyDown={(e) => {
              if (disabled) return;
              if (e.key === "Enter" && !e.altKey) {
                e.preventDefault();
                // Enter is the keyboard command for opening the entity
                // browser. Do not block the dialog on the optional inline
                // lookup request; that request remains useful on blur, while
                // the selector can query the current text immediately.
                void openSelection();
              }
              if (e.altKey && (e.key === "q" || e.key === "Q")) {
                e.preventDefault();
                e.stopPropagation();
                void openSelection();
              }
            }}
            onBlur={() => {
              // Opening a selector moves focus to its own surface. Do not
              // interpret that deliberate blur as an explicit field clear.
              if (openingSelectorRef.current) return;
              if (!disabled && searchText !== selectedLabel) {
                handleSearch(searchText, true);
              }
            }}
            aria-invalid={!!error}
            className="pr-10"
          />
          <Button
            size="icon-xs"
            variant="ghost"
            type="button"
            tabIndex={-1}
            disabled={disabled}
            className="text-muted-foreground hover:text-foreground absolute top-1 right-1 h-6 w-6"
            onMouseDown={(event) => event.preventDefault()}
            onClick={() => void openSelection()}
          >
            <Search className="size-4" />
          </Button>
        </div>
        {error && <FieldError>{error}</FieldError>}
      </Field>

    </>
  );
}
