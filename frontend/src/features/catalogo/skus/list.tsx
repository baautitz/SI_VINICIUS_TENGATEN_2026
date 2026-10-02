"use client";

import * as React from "react";
import { getActionsColumn } from "@/utils/table-columns";
import { FeatureHeader } from "@/ui/composites";
import { Package } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/ui/composites";
import { FeatureLayout } from "@/ui/composites";
import { Badge } from "@/ui/primitives";
import { Sku, getFullSkuName } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

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
  onView,
  onDelete,
  onSelect,
  onPageChange,
  rowSelection,
  onRowSelectionChange,
  selectAllAcrossPages,
  onSelectAllAcrossPagesChange,
}: FeatureListProps<Sku>) {
  const columns: ColumnDef<Sku>[] = [
    {
      accessorKey: "sku",
      header: "SKU",
      cell: ({ row }) => (
        <span className="font-mono text-xs font-semibold">{row.getValue("sku")}</span>
      ),
    },
    {
      id: "produto",
      header: "Produto",
      cell: ({ row }) => {
        const sku = row.original;
        const fullName = getFullSkuName(sku);
        const [produtoNome, variacao] = fullName.split(" - ");
        
        return (
          <div className="flex flex-col">
            <span className="font-medium">{produtoNome}</span>
            {variacao && (
              <span className="text-xs text-muted-foreground">{variacao}</span>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: "preco",
      header: () => <div className="text-right">Preço</div>,
      cell: ({ row }) => (
        <div className="text-right">
          {Number(row.getValue("preco")).toLocaleString("pt-BR", {
            style: "currency",
            currency: "BRL",
          })}
        </div>
      ),
    },
    {
      accessorKey: "estoque",
      header: () => <div className="text-right">Estoque</div>,
      cell: ({ row }) => (
        <div className="text-right">
          {Number(row.getValue("estoque")).toLocaleString("pt-BR")}
        </div>
      ),
    },
    {
      accessorKey: "ativo",
      header: "Status",
      size: 100,
      cell: ({ row }) => {
        const ativo = row.getValue("ativo") as boolean;
        return (
          <Badge variant={ativo ? "default" : "secondary"}>
            {ativo ? "Ativo" : "Inativo"}
          </Badge>
        );
      },
    },
    getActionsColumn<Sku>({
      onEdit,
      onView,
      onDelete,
      selectionMode,
      onSelect,
    }),
  ];

  return (
    <div className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
        <FeatureHeader
          title="SKUs"
          icon={<Package />}
          onAdd={onAdd}
          addButtonLabel="Novo SKU"
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
          searchPlaceholder="Pesquisar..."
          rowSelection={rowSelection}
          onRowSelectionChange={onRowSelectionChange}
          selectAllAcrossPages={selectAllAcrossPages}
          onSelectAllAcrossPagesChange={onSelectAllAcrossPagesChange}
          getRowId={(row) => row.sku}
          onRowSelect={selectionMode ? onSelect : undefined}
          onEditRow={onEdit}
          onDeleteRow={onDelete}
        />
      </FeatureLayout>
    </div>
  );
}
