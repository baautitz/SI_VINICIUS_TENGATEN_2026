"use client";

import React, { useState, useRef } from "react";
import { Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ListDialog } from "@/components/ui/list-dialog";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";

interface EntityInputProps<T, TResumo = T> {
  name: string;
  label: string;
  placeholder?: string;
  error?: string;
  initialItem?: T | TResumo | null;

  onSelectId: (id: number | null) => void;
  onSelectItem?: (item: T | null) => void;

  fetchById: (id: number) => Promise<T | null>;
  fetchList: (term: string) => Promise<{ itens?: TResumo[] } | null>;
  getDisplayLabel: (item: T | TResumo) => string;
  getSearchTerm: (item: TResumo) => string;
  getId: (item: T | TResumo) => number;

  modalTitle: string;
  renderFeature: (props: {
    selectionMode: boolean;
    onSelect: (item: TResumo) => void;
    initialSearchTerm: string;
  }) => React.ReactNode;
}

export function EntityInput<T, TResumo = T>({
  name,
  label,
  placeholder = "Digite, ou Alt+Espaço para buscar...",
  error,
  initialItem = null,
  onSelectId,
  onSelectItem,
  fetchById,
  fetchList,
  getDisplayLabel,
  getSearchTerm,
  getId,
  modalTitle,
  renderFeature,
}: EntityInputProps<T, TResumo>) {
  const [isOpen, setIsOpen] = useState(false);
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

  if (initialItem !== prevInitialItem) {
    setPrevInitialItem(initialItem);
    const newLabel = getLabel(initialItem);
    setSelectedItem((initialItem as TResumo | null) ?? null);
    setSearchText(newLabel ?? "");
    setSelectedLabel(newLabel ?? "");
  }

  const handleSearch = async (text: string, isBlur = false) => {
    if (!text.trim()) {
      onSelectId(null);
      onSelectItem?.(null);
      setSelectedItem(null);
      setSearchText("");
      setSelectedLabel("");
      if (!isBlur) {
        setIsOpen(true);
      }
      return;
    }
    if (isBlur && selectedLabel === text) return;

    try {
      const numericId = parseInt(text, 10);
      if (!isNaN(numericId) && /^\d+$/.test(text.trim())) {
        try {
          const matched = await fetchById(numericId);
          if (matched) {
            applySelection(matched);
            return;
          }
        } catch {}
      }

      const listRes = await fetchList(text);
      if (listRes?.itens && listRes.itens.length === 1) {
        const matched = await fetchById(getId(listRes.itens[0]));
        if (matched) {
          applySelection(listRes.itens[0]);
          return;
        }
      }

      if (isBlur) {
        setSearchText(selectedLabel);
      } else {
        setIsOpen(true);
      }
    } catch {
      if (isBlur) {
        setSearchText(selectedLabel);
      } else {
        setIsOpen(true);
      }
    }
  };

  const applySelection = async (item: T | TResumo) => {
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
    setIsOpen(false);
    setTimeout(() => {
      document.getElementById(name)?.focus();
    }, 100);
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
            onChange={(e) => setSearchText(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                handleSearch(searchText);
              }
              if (e.altKey && (e.key === "q" || e.key === "Q")) {
                e.preventDefault();
                e.stopPropagation();
                setIsOpen(true);
              }
            }}
            onBlur={() => {
              if (searchText !== selectedLabel) {
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
            className="absolute right-1 top-1 h-6 w-6 text-muted-foreground hover:text-foreground"
            onClick={() => setIsOpen(true)}
          >
            <Search className="size-4" />
          </Button>
        </div>
        {error && <FieldError>{error}</FieldError>}
      </Field>

      <ListDialog
        open={isOpen}
        onOpenChange={(o) => {
          setIsOpen(o);
          if (!o) {
            setTimeout(() => {
              document.getElementById(name)?.focus();
            }, 100);
          }
        }}
        title={modalTitle}
      >
        {renderFeature({
          selectionMode: true,
          onSelect: applySelection,
          initialSearchTerm: selectedItem ? getSearchTerm(selectedItem) : searchText,
        })}
      </ListDialog>
    </>
  );
}
