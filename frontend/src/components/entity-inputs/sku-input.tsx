"use client";

import React, { useRef, useState } from "react";
import { Search, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ListDialog } from "@/components/ui/list-dialog";
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
  onSelectSku: (sku: SkuResumo | null) => void;
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

  const handleLookup = async (code: string, refocusAfter = false) => {
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
        onSelectSku(match);
        setSelectedSku(match.sku);
        setSkuText(match.sku);
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

  const selectItem = (item: SkuResumo) => {
    onSelectSku(item);
    setSelectedSku(item.sku);
    setSkuText(item.sku);
    setIsOpen(false);
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
            onClick={() => selectItem(row.original)}
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
                if (!skuText.trim()) {
                  setIsOpen(true);
                } else {
                  handleLookup(skuText, true);
                }
              }
            }}
            onBlur={() => {
              if (skuText !== selectedSku) {
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
            />
          </div>
        </div>
      </ListDialog>

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
