"use client";

import React, { useState, useRef } from "react";
import { Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ListDialog } from "@/components/ui/list-dialog";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";

interface EntityInputProps<T> {
  name: string;
  label: string;
  placeholder?: string;
  error?: string;
  initialDisplayValue?: string;

  onSelectId: (id: number | undefined) => void;

  // APIs abstraídas
  fetchById: (id: number) => Promise<T | null>;
  fetchList: (term: string) => Promise<{ itens?: unknown[] } | null>;
  getDisplayLabel: (item: T) => string;
  getId: (item: T) => number;

  // UI
  modalTitle: string;
  renderFeature: (props: {
    selectionMode: boolean;
    onSelect: (item: T) => void;
    initialSearchTerm: string;
  }) => React.ReactNode;

  // Variantes do Input
  inputSize?: "small" | "medium" | "large" | "full";
}

export function EntityInput<T>({
  name,
  label,
  placeholder = "Digite o código, nome ou clique na lupa...",
  error,
  initialDisplayValue = "",
  onSelectId,
  fetchById,
  fetchList,
  getDisplayLabel,
  getId,
  modalTitle,
  renderFeature,
  inputSize = "full",
}: EntityInputProps<T>) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchText, setSearchText] = useState(initialDisplayValue);
  const [selectedLabel, setSelectedLabel] = useState(initialDisplayValue);
  const [prevInitialValue, setPrevInitialValue] = useState(initialDisplayValue);
  const inputRef = useRef<HTMLInputElement>(null);

  if (initialDisplayValue !== prevInitialValue) {
    setPrevInitialValue(initialDisplayValue);
    setSearchText(initialDisplayValue);
    setSelectedLabel(initialDisplayValue);
  }

  const handleSearch = async (text: string, isBlur = false) => {
    if (!text.trim()) {
      onSelectId(undefined);
      setSearchText("");
      setSelectedLabel("");
      return;
    }
    if (isBlur && selectedLabel === text) return;

    try {
      // 1. Busca por ID (se numérico)
      const numericId = parseInt(text, 10);
      if (!isNaN(numericId) && /^\d+$/.test(text.trim())) {
        try {
          const matched = await fetchById(numericId);
          if (matched) {
            applySelection(matched);
            return;
          }
        } catch {
          // Fallback para nome
        }
      }

      // 2. Busca Autofill Mágica por Nome
      const listRes = await fetchList(text);
      if (listRes?.itens && listRes.itens.length === 1) {
        // Encontrou exatamente um! Fazemos o getById para garantir os dados completos
        const matched = await fetchById(getId(listRes.itens[0] as T));
        if (matched) {
          applySelection(matched);
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

  const applySelection = (item: T) => {
    const newLabel = getDisplayLabel(item);
    onSelectId(getId(item));
    setSearchText(newLabel);
    setSelectedLabel(newLabel);
    setIsOpen(false);
    setTimeout(() => {
      document.getElementById(name)?.focus();
    }, 100);
  };

  const getCleanSearchTerm = (text: string) => {
    if (text === selectedLabel) {
      return text.replace(/\s*\([^)]*\)$/, "").trim();
    }
    return text;
  };

  return (
    <>
      <Field data-invalid={!!error}>
        <FieldLabel htmlFor={name}>{label}</FieldLabel>
        <div className="relative flex-1">
          <Input
            ref={inputRef}
            id={name}
            inputSize={inputSize}
            placeholder={placeholder}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                handleSearch(searchText);
              }
            }}
            onBlur={() => {
              if (searchText !== selectedLabel && searchText.trim() !== "") {
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
          initialSearchTerm: getCleanSearchTerm(searchText),
        })}
      </ListDialog>
    </>
  );
}
