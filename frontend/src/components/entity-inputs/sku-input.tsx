"use client";

import React, { useEffect, useRef, useState } from "react";
import { Search } from "lucide-react";
import { Button } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { Field, FieldLabel, FieldError } from "@/ui/primitives";
import { skusApi } from "@/api/catalogo";
import { SkusFeature } from "@/features/catalogo/skus";
import { Sku } from "@/features/catalogo/skus/types";
import { NumberInput } from "@/ui/composites";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { useWindow, useWindowCommands, useUi } from "@/ui/imperative";

interface SkuInputProps {
  name: string;
  label?: React.ReactNode;
  error?: string;
  initialSku?: string | null;
  onSelectSku: (sku: Sku | null, quantidade?: number) => void | Promise<void>;
  disabled?: boolean;
}

interface SkuSelectionWindowProps {
  initialSearchTerm?: string;
}

interface QuantityWindowProps {
  sku: Sku;
  initialQuantity?: number;
}

export interface ParsedSkuInput {
  code: string;
  quantity: number;
  hasQuantityPrefix: boolean;
}

/**
 * Accepts the scanner-friendly `quantidade*sku` notation without allowing the
 * quantity prefix to leak into the SKU lookup.  A comma is accepted as the
 * decimal separator used by the rest of the Portuguese UI.
 */
export function parseSkuInput(value: string): ParsedSkuInput {
  const text = value.trim();
  const match = text.match(
    /^([+-]?(?:[0-9]+(?:[.,][0-9]+)?|[.,][0-9]+))\s*\*\s*(.*)$/,
  );

  if (!match) {
    return { code: text, quantity: 1, hasQuantityPrefix: false };
  }

  return {
    code: match[2].trim(),
    quantity: Number(match[1].replace(",", ".")),
    hasQuantityPrefix: true,
  };
}

/** Janela imperativa de seleção: a seleção resolve a Promise da receita chamadora. */
function SkuSelectionWindow({
  initialSearchTerm = "",
}: SkuSelectionWindowProps) {
  const activeWindow = useWindow<Sku>();
  const ui = useUi();

  const handleSelect = async (sku: Sku) => {
    if (!sku.ativo) {
      ui.feedback.notify({
        type: "error",
        title: "Este SKU está inativo.",
      });
      return;
    }

    activeWindow.resolve(sku);
  };

  return (
    <div className="flex h-full min-h-0 flex-col p-4">
      <SkusFeature
        selectionMode
        onSelect={handleSelect}
        initialSearchTerm={initialSearchTerm}
      />
    </div>
  );
}

/** Janela imperativa para informar a quantidade de um SKU selecionado. */
function QuantityWindow({ sku, initialQuantity }: QuantityWindowProps) {
  const activeWindow = useWindow<number>();
  const ui = useUi();
  const [quantity, setQuantity] = useState(initialQuantity ?? 1);
  const allowsDecimals = sku.produto?.unidadeMedida?.permiteDecimais ?? false;

  useEffect(() => {
    activeWindow.setDirty(quantity !== 1);
  }, [activeWindow, quantity]);

  const confirm = async () => {
    if (Number.isNaN(quantity) || quantity <= 0) {
      ui.feedback.notify({
        type: "error",
        title: "Quantidade deve ser maior que zero.",
      });
      return;
    }

    activeWindow.resolve(quantity);
  };

  const cancel = async () => {
    activeWindow.dismiss("cancel");
  };

  useWindowCommands([
    {
      id: "sku.quantity.confirm",
      hotkey: "Alt+Enter",
      label: "Confirmar quantidade",
      run: confirm,
    },
  ]);

  const handleQuantityKeyDown = async (
    event: React.KeyboardEvent<HTMLInputElement>,
  ) => {
    if (event.key === "Enter" && !event.altKey && !event.ctrlKey) {
      event.preventDefault();
      await confirm();
    }
  };

  return (
    <div className="flex flex-col gap-4">
      <div className="py-2">
        <FieldLabel htmlFor="qty-input">Quantidade a adicionar</FieldLabel>
        <NumberInput
          id="qty-input"
          autoFocus
          className="mt-1.5"
          value={quantity}
          decimals={allowsDecimals ? 4 : 0}
          onNumberChange={setQuantity}
          onKeyDown={handleQuantityKeyDown}
        />
      </div>
      <div data-window-actions className="flex justify-end gap-2">
        <Button variant="outline" type="button" onClick={cancel}>
          Cancelar <Kbd>Esc</Kbd>
        </Button>
        <Button type="button" onClick={confirm}>
          Confirmar
          <KbdGroup className="ml-2">
            <Kbd>Alt</Kbd>
            <Kbd>Enter</Kbd>
          </KbdGroup>
        </Button>
      </div>
    </div>
  );
}

export const SkuInput = ({
  name,
  label = "Produto/SKU",
  error,
  initialSku = null,
  onSelectSku,
  disabled = false,
  ref,
}: SkuInputProps & { ref?: React.Ref<HTMLInputElement> }) => {
  const ui = useUi();
  const [skuText, setSkuText] = useState(initialSku ?? "");
  const [selectedSku, setSelectedSku] = useState<string | null>(initialSku);
  const internalRef = useRef<HTMLInputElement>(null);
  const interactionInFlightRef = useRef(false);
  const selectorOpeningRef = useRef(false);

  const setRefs = React.useCallback(
    (element: HTMLInputElement | null) => {
      internalRef.current = element;
      if (ref) {
        if (typeof ref === "function") {
          ref(element);
        } else {
          (ref as React.RefObject<HTMLInputElement | null>).current = element;
        }
      }
    },
    [ref],
  );

  const [prevInitialSku, setPrevInitialSku] = useState(initialSku);
  if (initialSku !== prevInitialSku) {
    setPrevInitialSku(initialSku);
    setSelectedSku(initialSku);
    setSkuText(initialSku ?? "");
  }

  const focusInput = () => {
    internalRef.current?.focus();
  };

  const refocusAfterWindow = () => {
    focusInput();
    // WindowController restores an opener on the next animation frame. A
    // selector followed by a removal confirmation can otherwise restore an
    // older field after this callback runs.
    if (typeof window !== "undefined") {
      window.requestAnimationFrame(focusInput);
    }
  };

  const openSelector = async (initialSearchTerm = "") => {
    // A lookup started by Enter can finish at the same time as the blur
    // lookup. Both paths may ask for the selector, but only one window may be
    // opened for this input.
    if (disabled || selectorOpeningRef.current) return null;
    selectorOpeningRef.current = true;

    try {
      const result = await ui.windows.open<Sku, SkuSelectionWindowProps>({
        component: SkuSelectionWindow,
        props: { initialSearchTerm },
        title: "Selecionar Produto (SKU)",
        size: "full",
        chrome: "plain",
      });

      return result.status === "confirmed" ? result.value : null;
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível abrir a seleção de SKU.",
      });
      return null;
    } finally {
      selectorOpeningRef.current = false;
    }
  };

  const openQuantity = async (sku: Sku, initialQuantity = 1) => {
    const result = await ui.windows.open<number, QuantityWindowProps>({
      component: QuantityWindow,
      props: { sku, initialQuantity },
      title: "Informe a Quantidade",
      description: `SKU selecionado: ${sku.sku}`,
      size: "small",
    });

    return result.status === "confirmed" ? result.value : null;
  };

  const selectSku = async (
    sku: Sku,
    initialQuantity = 1,
    quantityIsFinal = false,
  ) => {
    try {
      if (!sku.ativo) {
        ui.feedback.notify({
          type: "error",
          title: `O SKU "${sku.sku}" está inativo.`,
        });
        return;
      }

      const quantity = quantityIsFinal
        ? initialQuantity
        : await openQuantity(sku, initialQuantity);
      if (quantity === null) return;

      await onSelectSku(sku, quantity);
      setSelectedSku(sku.sku);
      setSkuText("");
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível adicionar o SKU.",
      });
    } finally {
      // The parent list forms remount this component after adding a row.  The
      // refocus is also intentional after cancellation, so opening a selector
      // never strands keyboard users on a closed window.
      refocusAfterWindow();
    }
  };

  const handleLookup = async (
    code: string,
    quantity = 1,
    refocusAfter = false,
  ) => {
    // Enter and blur can start the same lookup concurrently. Keep the whole
    // interaction locked through selector and quantity confirmation so the
    // blur caused by opening the next window cannot restart it.
    if (interactionInFlightRef.current) return;
    interactionInFlightRef.current = true;
    try {
      const parsedInput = parseSkuInput(code);
      const trimmedCode = parsedInput.code;
      const lookupQuantity = parsedInput.hasQuantityPrefix
        ? parsedInput.quantity
        : quantity;

      if (
        parsedInput.hasQuantityPrefix &&
        (!Number.isFinite(lookupQuantity) || lookupQuantity === 0)
      ) {
        ui.feedback.notify({
          type: "error",
          title: "Quantidade não pode ser zero.",
        });
        return;
      }

      if (!trimmedCode) {
        onSelectSku(null);
        setSelectedSku(null);
        setSkuText("");
        return;
      }

      try {
        const match = await skusApi.getBySku(trimmedCode);
        if (match) {
          if (!match.ativo) {
            ui.feedback.notify({
              type: "error",
              title: `O SKU "${trimmedCode}" está inativo.`,
            });
            onSelectSku(null);
            setSelectedSku(null);
            setSkuText("");
            if (refocusAfter) refocusAfterWindow();
            return;
          }

          await onSelectSku(match, lookupQuantity);
          setSelectedSku(match.sku);
          setSkuText("");
          if (refocusAfter) refocusAfterWindow();
          return;
        }
      } catch {
        // A miss is the normal path for a partial code: the selector below
        // performs the broader search.  Do not report an expected lookup miss
        // as a console error or interrupt the selection flow.
      }

      // Opening the selector is not a selection change. Keep the current form
      // value until the user confirms a SKU, otherwise an Enter used only to
      // search would write `null` and trigger the parent form validator.
      const selected = await openSelector(trimmedCode);
      if (selected) {
        await selectSku(
          selected,
          lookupQuantity,
          parsedInput.hasQuantityPrefix && lookupQuantity < 0,
        );
      } else if (refocusAfter) refocusAfterWindow();
    } finally {
      interactionInFlightRef.current = false;
    }
  };

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setSkuText(event.target.value);
  };

  const handleInputKeyDown = async (
    event: React.KeyboardEvent<HTMLInputElement>,
  ) => {
    if (event.key !== "Enter" || event.altKey || event.ctrlKey) return;
    event.preventDefault();

    // Read the DOM value as well as React state. Scanner input can dispatch
    // the final character and Enter in the same turn, before the controlled
    // state update is observable by this callback.
    const rawInputText = internalRef.current?.value ?? skuText;
    const parsedInput = parseSkuInput(rawInputText);
    const text = parsedInput.code;

    if (
      parsedInput.hasQuantityPrefix &&
      (!Number.isFinite(parsedInput.quantity) || parsedInput.quantity === 0)
    ) {
      ui.feedback.notify({
        type: "error",
        title: "Quantidade não pode ser zero.",
      });
      refocusAfterWindow();
      return;
    }

    if (!text) {
      if (interactionInFlightRef.current) return;
      interactionInFlightRef.current = true;
      try {
        const selected = await openSelector();
        if (selected) {
          await selectSku(
            selected,
            parsedInput.quantity,
            parsedInput.hasQuantityPrefix && parsedInput.quantity < 0,
          );
        }
      } finally {
        interactionInFlightRef.current = false;
        refocusAfterWindow();
      }
      return;
    }

    await handleLookup(
      rawInputText,
      parsedInput.hasQuantityPrefix ? parsedInput.quantity : 1,
      true,
    );
  };

  const handleInputBlur = async () => {
    const currentText = internalRef.current?.value ?? skuText;
    const parsedInput = parseSkuInput(currentText);
    if (
      currentText !== selectedSku &&
      currentText.trim() &&
      !parsedInput.hasQuantityPrefix
    ) {
      await handleLookup(currentText);
    }
  };

  const handleSearchClick = async () => {
    if (interactionInFlightRef.current) return;
    interactionInFlightRef.current = true;
    try {
      const selected = await openSelector();
      if (selected) await selectSku(selected);
    } finally {
      interactionInFlightRef.current = false;
      refocusAfterWindow();
    }
  };

  const handleSearchHotkey = async () => {
    if (document.activeElement !== internalRef.current) return;
    await handleSearchClick();
  };

  useWindowCommands(
    [
      {
        id: `sku-input.${name}.open-selector`,
        hotkey: "Alt+K",
        label: "Abrir seleção de SKU",
        enabled: !disabled,
        run: handleSearchHotkey,
      },
    ],
    {
      scope: `sku-input.${name}`,
      target: internalRef,
    },
  );

  return (
    <Field data-invalid={!!error}>
      {label && (
        <FieldLabel htmlFor={name}>
          <div className="flex items-center gap-2">
            {label}
            <KbdGroup>
              <Kbd>Alt</Kbd>
              <Kbd>K</Kbd>
            </KbdGroup>
          </div>
        </FieldLabel>
      )}
      <div className="relative w-full">
        <Input
          ref={setRefs}
          id={name}
          value={skuText}
          disabled={disabled}
          onChange={handleInputChange}
          onKeyDown={handleInputKeyDown}
          onBlur={handleInputBlur}
          className="h-8 pr-10 text-xs"
          placeholder="Digite o código SKU e pressione Enter..."
        />
        <Button
          size="icon-xs"
          variant="ghost"
          type="button"
          disabled={disabled}
          tabIndex={-1}
          className="text-muted-foreground hover:text-foreground absolute top-1 right-1 h-6 w-6"
          onMouseDown={(event) => event.preventDefault()}
          onClick={handleSearchClick}
        >
          <Search className="size-4" />
        </Button>
      </div>
      {error && <FieldError>{error}</FieldError>}
    </Field>
  );
};
