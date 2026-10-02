"use client";

import * as React from "react";
import { getActionsColumn } from "@/utils/table-columns";
import { FeatureHeader } from "@/ui/composites";
import { Package } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/ui/composites";
import { FeatureLayout } from "@/ui/composites";
import { Badge } from "@/ui/primitives";
import { Produto } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function ProdutosList({
  items: produtos,
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
}: FeatureListProps<Produto>) {
  const columns: ColumnDef<Produto>[] = [
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "produto",
      header: "Produto",
    },
    {
      accessorKey: "categoria.categoria",
      header: "Categoria",
    },
    {
      accessorKey: "marca.marca",
      header: "Marca",
    },
    {
      accessorKey: "estoqueTotal",
      header: () => <div className="text-right">Estoque</div>,
      cell: ({ row }) => {
        const value = row.getValue("estoqueTotal");
        return (
          <div className="text-right font-mono text-xs">
            {value !== undefined && value !== null ? Number(value) : 0}
          </div>
        );
      },
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
    getActionsColumn<Produto>({
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
          title="Produtos"
          icon={<Package />}
          onAdd={onAdd}
          addButtonLabel="Novo Produto"
        />

        <DataTable
          columns={columns}
          data={produtos}
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
          getRowId={(row) => row.id.toString()}
          onRowSelect={selectionMode ? onSelect : undefined}
          onEditRow={onEdit}
          onDeleteRow={onDelete}
        />
      </FeatureLayout>
    </div>
  );
}
