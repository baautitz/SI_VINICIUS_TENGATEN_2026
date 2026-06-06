"use client";

import React, { useRef, useState } from "react";
import { Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ListDialog } from "@/components/ui/list-dialog";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import { skusApi } from "@/api/catalogo";
import { toast } from "sonner";
import { SkuResumo, SkusFeature } from "@/features/catalogo/skus";
import { NumberInput } from "@/components/ui/number-input";

import { Kbd, KbdGroup } from "@/components/ui/kbd";

interface SkuInputProps {
  name: string;
  label?: React.ReactNode;
  error?: string;
  initialSku?: string | null;
  onSelectSku: (sku: SkuResumo | null, quantidade?: number) => void;
  disabled?: boolean;
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
  const [isOpen, setIsOpen] = useState(false);
  const [skuText, setSkuText] = useState(initialSku ?? "");
  const [selectedSku, setSelectedSku] = useState<string | null>(initialSku);
  const [initialSearchTerm, setInitialSearchTerm] = useState("");
  const internalRef = useRef<HTMLInputElement>(null);

  const setRefs = React.useCallback(
    (el: HTMLInputElement | null) => {
      internalRef.current = el;
      if (ref) {
        if (typeof ref === "function") {
          ref(el);
        } else {
          (ref as React.RefObject<HTMLInputElement | null>).current = el;
        }
      }
    },
    [ref],
  );

  const [quantityModalItem, setQuantityModalItem] = useState<SkuResumo | null>(
    null,
  );
  const [quantityInput, setQuantityInput] = useState<number>(1);

  const wasOpen = useRef(isOpen);

  React.useEffect(() => {
    if (wasOpen.current && !isOpen) {
      setTimeout(() => {
        internalRef.current?.focus();
      }, 100);
    }
    wasOpen.current = isOpen;
  }, [isOpen]);

  const [prevInitialSku, setPrevInitialSku] = useState(initialSku);
  if (initialSku !== prevInitialSku) {
    setPrevInitialSku(initialSku);
    setSelectedSku(initialSku);
    setSkuText(initialSku ?? "");
  }

  const handleLookup = async (
    code: string,
    qtde: number = 1,
    refocusAfter = false,
  ) => {
    const trimmedCode = code.trim();
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
          toast.error(`O SKU "${trimmedCode}" está inativo.`);
          onSelectSku(null);
          setSelectedSku(null);
          setSkuText("");
          if (refocusAfter) {
            setTimeout(() => {
              internalRef.current?.focus();
            }, 50);
          }
          return;
        }
        onSelectSku(match, qtde);
        setSelectedSku(match.sku);
        setSkuText("");
        if (refocusAfter) {
          setTimeout(() => {
            internalRef.current?.focus();
          }, 50);
        }
      } else {
        onSelectSku(null);
        setSelectedSku(null);
        setInitialSearchTerm(trimmedCode);
        setIsOpen(true);
      }
    } catch {
      onSelectSku(null);
      setSelectedSku(null);
      setInitialSearchTerm(trimmedCode);
      setIsOpen(true);
    }
  };

  const selectItemFromList = (item: SkuResumo) => {
    if (!item.ativo) {
      toast.error("Este SKU está inativo.");
      return;
    }
    setQuantityModalItem(item);
    setQuantityInput(1);
  };

  const confirmQuantity = () => {
    if (quantityModalItem) {
      const qtde = quantityInput;
      if (!isNaN(qtde) && qtde !== 0) {
        onSelectSku(quantityModalItem, qtde);
        setSelectedSku(quantityModalItem.sku);
        setSkuText("");
        setIsOpen(false);
        setQuantityModalItem(null);
        setTimeout(() => {
          internalRef.current?.focus();
        }, 150);
      } else {
        toast.error("Quantidade inválida.");
      }
    }
  };

  return (
    <>
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
            onChange={(e) => setSkuText(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.altKey) {
                e.preventDefault();
                const text = skuText.trim();
                if (!text) {
                  setInitialSearchTerm("");
                  setIsOpen(true);
                } else {
                  let qtde = 1;
                  let codeToSearch = text;
                  if (text.includes("*")) {
                    const parts = text.split("*");
                    const parsedQtde = parseFloat(parts[0]);
                    if (!isNaN(parsedQtde) && parsedQtde !== 0) {
                      qtde = parsedQtde;
                      codeToSearch = parts.slice(1).join("*").trim();
                    }
                  }
                  handleLookup(codeToSearch, qtde, true);
                }
              }
              if (e.altKey && (e.key === "q" || e.key === "Q")) {
                e.preventDefault();
                e.stopPropagation();
                setIsOpen(true);
              }
            }}
            onBlur={() => {
              if (
                skuText !== selectedSku &&
                skuText.trim() &&
                !skuText.includes("*")
              ) {
                handleLookup(skuText);
              }
            }}
            className="pr-10 h-8 text-xs"
            placeholder="Digite o código SKU e pressione Enter..."
          />
          <Button
            size="icon-xs"
            variant="ghost"
            type="button"
            disabled={disabled}
            tabIndex={-1}
            className="absolute right-1 top-1 h-6 w-6 text-muted-foreground hover:text-foreground"
            onClick={() => {
              setInitialSearchTerm("");
              setIsOpen(true);
            }}
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
        }}
        title="Selecionar Produto (SKU)"
      >
        <SkusFeature
          selectionMode
          onSelect={selectItemFromList}
          initialSearchTerm={initialSearchTerm}
        />
      </ListDialog>

      <Dialog
        open={!!quantityModalItem}
        onOpenChange={(o) => {
          if (!o) setQuantityModalItem(null);
        }}
      >
        <DialogContent className="max-w-[320px]">
          <DialogHeader>
            <DialogTitle>Informe a Quantidade</DialogTitle>
            <DialogDescription>
              SKU selecionado: <strong>{quantityModalItem?.sku}</strong>
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <FieldLabel htmlFor="qty-input">Quantidade a adicionar</FieldLabel>
            <NumberInput
              id="qty-input"
              autoFocus
              className="mt-1.5"
              value={quantityInput}
              decimals={quantityModalItem?.permiteDecimais ? 4 : 0}
              onNumberChange={(num) => setQuantityInput(num)}
              onKeyDown={(e) => {
                if (e.key === "Enter" && !e.altKey) {
                  e.preventDefault();
                  confirmQuantity();
                }
              }}
            />
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setQuantityModalItem(null)}
            >
              Cancelar <Kbd>Esc</Kbd>
            </Button>
            <Button onClick={confirmQuantity}>
              Confirmar
              <KbdGroup className="ml-2">
                <Kbd>Enter</Kbd>
              </KbdGroup>
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
};
