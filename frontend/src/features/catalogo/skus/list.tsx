"use client";

import * as React from "react";

import { StatusBadge } from "@/components/ui/status-badge";
import { getActionsColumn } from "@/utils/table-columns";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/components/ui/data-table";
import { SkuResumo } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { FeatureHeader } from "@/components/ui/feature-header";
import { PackagePlus } from "lucide-react";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";

export function SkusList({
  items: skus,
  loading,
  searchTerm,
  page,
  totalPages,
  totalItems,
  selectionMode = false,
  onSearchChange,
  onAdd,
  onEdit,
  onSelect,
  onPageChange,
  searchInputRef,
}: FeatureListProps<SkuResumo>) {
  const listRef = React.useRef<HTMLDivElement>(null);
  
  useFeatureHotkeys({
    onAdd,
    listRef,
  });

  const columns: ColumnDef<SkuResumo>[] = [
    {
      accessorKey: "produtoId",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold text-muted-foreground">
          #{row.original.produtoId}
        </span>
      ),
    },
    {
      accessorKey: "sku",
      header: "Código SKU",
      cell: ({ row }) => (
        <span className="font-mono font-medium">{row.original.sku}</span>
      ),
    },
    {
      accessorKey: "produtoNome",
      header: "Produto",
      cell: ({ row }) => (
        <span className="font-medium">{row.original.produtoNome}</span>
      ),
    },
    {
      accessorKey: "estoque",
      header: "Estoque",
      cell: ({ row }) => (
        <span className="font-semibold text-muted-foreground">
          {Number(row.original.estoque).toLocaleString()}
        </span>
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
      cell: ({ row }) => <StatusBadge ativo={row.original.ativo} />,
    },
    getActionsColumn<SkuResumo>({
      onEdit,
      selectionMode,
      onSelect,
    }),
  ];

  return (
    <div ref={listRef} className="flex-1 min-h-0 flex flex-col gap-4">
      <FeatureHeader
        title="Selecionar Produto (SKU)"
        icon={<PackagePlus className="size-5" />}
        onAdd={onAdd}
        addButtonLabel="Novo Produto"
      />

      <DataTable
        columns={columns}
        data={skus}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        getRowId={(row) => row.sku}
        onRowSelect={selectionMode ? onSelect : undefined}
        onEditRow={onEdit}
        searchInputRef={searchInputRef}
      />
    </div>
  );
}
