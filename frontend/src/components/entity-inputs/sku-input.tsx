"use client";

import React, { useRef, useState } from "react";
import { Search, Plus } from "lucide-react";
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
import { skusApi, SkuResumo } from "@/api/catalogo";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { DataTable } from "@/components/ui/data-table";
import { ColumnDef } from "@tanstack/react-table";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { ProdutosUpsert } from "@/features/catalogo/produtos/upsert";

interface SkuInputProps {
  name: string;
  label?: string;
  error?: string;
  initialSku?: string | null;
  onSelectSku: (sku: SkuResumo | null, quantidade?: number) => void;
  disabled?: boolean;
}

export function SkuInput({
  name,
  label = "Produto/SKU",
  error,
  initialSku = null,
  onSelectSku,
  disabled = false,
}: SkuInputProps) {
  const queryClient = useQueryClient();
  const [isOpen, setIsOpen] = useState(false);
  const [isProductCreateOpen, setIsProductCreateOpen] = useState(false);
  const [skuText, setSkuText] = useState(initialSku ?? "");
  const [selectedSku, setSelectedSku] = useState<string | null>(initialSku);
  const [searchTerm, setSearchTerm] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const searchInputRef = useRef<HTMLInputElement>(null);
  
  const [quantityModalItem, setQuantityModalItem] = useState<SkuResumo | null>(null);
  const [quantityInput, setQuantityInput] = useState("1");

  // Sync prop changes during render to avoid cascading renders in useEffect
  const [prevInitialSku, setPrevInitialSku] = useState(initialSku);
  if (initialSku !== prevInitialSku) {
    setPrevInitialSku(initialSku);
    setSelectedSku(initialSku);
    setSkuText(initialSku ?? "");
  }

  // Query to fetch list of SKUs inside the modal
  const { data, isLoading } = useQuery({
    queryKey: ["skus", "list", searchTerm, page],
    queryFn: () => skusApi.list(searchTerm || undefined, page, pageSize),
    enabled: isOpen,
  });

  const inputRef = useRef<HTMLInputElement>(null);

  const handleLookup = async (code: string, qtde: number = 1, refocusAfter = false) => {
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
          if (refocusAfter) inputRef.current?.focus();
          return;
        }
        onSelectSku(match, qtde);
        setSelectedSku(match.sku);
        setSkuText(""); // Clear text to be ready for next SKU immediately, matching POS behavior
        if (refocusAfter) inputRef.current?.focus();
      } else {
        onSelectSku(null);
        setSelectedSku(null);
        setSearchTerm(trimmedCode);
        setPage(1);
        setIsOpen(true);
      }
    } catch {
      onSelectSku(null);
      setSelectedSku(null);
      setSearchTerm(trimmedCode);
      setPage(1);
      setIsOpen(true);
    }
  };

  const selectItemFromList = (item: SkuResumo) => {
    setQuantityModalItem(item);
    setQuantityInput("1");
  };

  const confirmQuantity = () => {
    if (quantityModalItem) {
      const qtde = parseFloat(quantityInput);
      if (!isNaN(qtde) && qtde !== 0) {
        onSelectSku(quantityModalItem, qtde);
        setSelectedSku(quantityModalItem.sku);
        setSkuText(""); // clear main input ready for next
        setIsOpen(false);
        setQuantityModalItem(null);
        inputRef.current?.focus();
      } else {
        toast.error("Quantidade inválida.");
      }
    }
  };

  const columns: ColumnDef<SkuResumo>[] = [
    {
      accessorKey: "sku",
      header: "Código SKU",
      cell: ({ row }) => (
        <span className="font-mono font-medium">{row.original.sku}</span>
      ),
    },
    {
      accessorKey: "estoque",
      header: "Estoque",
      cell: ({ row }) => (
        <span>{Number(row.original.estoque).toLocaleString()}</span>
      ),
    },
    {
      accessorKey: "preco",
      header: "Preço",
      cell: ({ row }) => (
        <span>
          {Number(row.original.preco).toLocaleString("pt-BR", {
            style: "currency",
            currency: "BRL",
          })}
        </span>
      ),
    },
    {
      accessorKey: "ativo",
      header: "Status",
      cell: ({ row }) => (
        <Badge
          variant={row.original.ativo ? "default" : "secondary"}
          className={row.original.ativo ? "bg-emerald-500 hover:bg-emerald-600 text-white border-none" : ""}
        >
          {row.original.ativo ? "Ativo" : "Inativo"}
        </Badge>
      ),
    },
    {
      id: "actions",
      header: () => <div className="text-right px-4">Ação</div>,
      cell: ({ row }) => (
        <div className="flex justify-end px-4">
          <Button
            size="sm"
            onClick={() => selectItemFromList(row.original)}
            disabled={!row.original.ativo}
          >
            Selecionar
          </Button>
        </div>
      ),
    },
  ];

  return (
    <>
      <Field data-invalid={!!error}>
        {label && <FieldLabel htmlFor={name}>{label}</FieldLabel>}
        <div className="relative w-full">
          <Input
            ref={inputRef}
            id={name}
            value={skuText}
            disabled={disabled}
            onChange={(e) => setSkuText(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                const text = skuText.trim();
                if (!text) {
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
            }}
            onBlur={() => {
              if (skuText !== selectedSku && skuText.trim() && !skuText.includes("*")) {
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
        }}
        title="Selecionar Produto (SKU)"
      >
        <div className="flex flex-col gap-4 h-full flex-1">
          <div className="flex items-center justify-between gap-2 shrink-0">
            <Input
              ref={searchInputRef}
              placeholder="Pesquisar por SKU ou código..."
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setPage(1);
              }}
              inputSize="large"
              className="text-xs"
            />
            <Button
              type="button"
              onClick={() => setIsProductCreateOpen(true)}
              className="h-8"
            >
              <Plus data-icon="inline-start" />
              Criar Novo Produto
            </Button>
          </div>

          <div className="flex-1 min-h-0 flex flex-col">
            <DataTable
              columns={columns}
              data={data?.itens ?? []}
              loading={isLoading}
              pageCount={data?.totalDePaginas ?? 1}
              pageIndex={page}
              onPageChange={setPage}
              totalItems={data?.totalDeItens ?? 0}
              getRowId={(row) => row.sku}
              onRowSelect={selectItemFromList}
              searchInputRef={searchInputRef}
            />
          </div>
        </div>
      </ListDialog>

      <Dialog open={!!quantityModalItem} onOpenChange={(o) => { if (!o) setQuantityModalItem(null); }}>
        <DialogContent className="max-w-[320px]">
          <DialogHeader>
            <DialogTitle>Informe a Quantidade</DialogTitle>
            <DialogDescription>
              SKU selecionado: <strong>{quantityModalItem?.sku}</strong>
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <FieldLabel htmlFor="qty-input">Quantidade a adicionar</FieldLabel>
            <Input
              id="qty-input"
              type="number"
              autoFocus
              className="mt-1.5"
              value={quantityInput}
              step="any"
              onChange={(e) => setQuantityInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  confirmQuantity();
                }
              }}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setQuantityModalItem(null)}>Cancelar</Button>
            <Button onClick={confirmQuantity}>Confirmar</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {isProductCreateOpen && (
        <ProdutosUpsert
          open={isProductCreateOpen}
          editingItem={null}
          onClose={() => setIsProductCreateOpen(false)}
          onSuccess={() => {
            setIsProductCreateOpen(false);
            queryClient.invalidateQueries({ queryKey: ["skus"] });
            toast.success("Produto cadastrado com sucesso!");
          }}
        />
      )}
    </>
  );
}
