"use client";

import React from "react";
import { Pencil } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import {
  Combobox,
  ComboboxChip,
  ComboboxChips,
  ComboboxChipsInput,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxItem,
  ComboboxList,
  ComboboxValue,
  ComboboxCreate,
  useComboboxAnchor,
} from "@/components/ui/combobox";

export interface MultiEntityItem {
  id: number;
  label: string;
}

interface MultiEntityInputProps {
  name: string;
  label: string;
  placeholder?: string;
  error?: string;
  selectedItems: MultiEntityItem[];
  availableItems: MultiEntityItem[];
  onChange: (items: MultiEntityItem[]) => void;
  onEditEntity?: () => void;
  onCreateOption?: (text: string) => void;
  editLabel?: string;
  loading?: boolean;
}

export function MultiEntityInput({
  name,
  label,
  placeholder = "Buscar...",
  error,
  selectedItems,
  availableItems,
  onChange,
  onEditEntity,
  onCreateOption,
  editLabel = "Editar",
  loading = false,
}: MultiEntityInputProps) {
  const anchor = useComboboxAnchor();

  const handleValueChange = (values: string[]) => {
    const newSelected = values.map((val) => {
      const item = availableItems.find((i) => String(i.id) === val);
      return item || { id: Number(val), label: val };
    });
    onChange(newSelected);
  };

  const selectedIds = selectedItems.map((i) => String(i.id));

  return (
    <Field data-invalid={!!error} className="w-full">
      <FieldLabel htmlFor={name}>{label}</FieldLabel>

      <div className="w-full flex gap-1 items-start">
        <div className="flex-1">
          <Combobox
            multiple
            items={availableItems}
            value={selectedIds}
            onValueChange={handleValueChange}
          >
            <ComboboxChips ref={anchor} className="w-full">
              <ComboboxValue>
                {(values) => (
                  <React.Fragment>
                    {values.map((value: string) => {
                      const item = selectedItems.find((i) => String(i.id) === value);
                      const displayLabel = item ? item.label : value;
                      return (
                        <ComboboxChip
                          key={value}
                          onRemove={() => {
                            onChange(selectedItems.filter((i) => String(i.id) !== value));
                          }}
                        >
                          {displayLabel}
                        </ComboboxChip>
                      );
                    })}
                    <ComboboxChipsInput id={name} placeholder={selectedItems.length === 0 ? placeholder : ""} disabled={loading} />
                  </React.Fragment>
                )}
              </ComboboxValue>
            </ComboboxChips>

            <ComboboxContent>
              <ComboboxEmpty>Nenhum valor encontrado.</ComboboxEmpty>
              <ComboboxList>
                {(item: MultiEntityItem) => (
                  <ComboboxItem key={item.id} value={String(item.id)}>
                    {item.label}
                  </ComboboxItem>
                )}
              </ComboboxList>
              {onCreateOption && (
                <ComboboxCreate onClick={onCreateOption}>
                  {(text) => `+ Criar valor "${text}"`}
                </ComboboxCreate>
              )}
            </ComboboxContent>
          </Combobox>
        </div>

        {onEditEntity && (
          <Button
            size="icon"
            variant="outline"
            type="button"
            tabIndex={-1}
            onClick={onEditEntity}
            title={editLabel}
            className="shrink-0"
          >
            <Pencil />
          </Button>
        )}
      </div>

      {error && <FieldError>{error}</FieldError>}
    </Field>
  );
}
