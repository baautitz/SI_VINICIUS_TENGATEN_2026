"use client";

import * as React from "react";
import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Package } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/components/ui/data-table";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { Badge } from "@/components/ui/badge";
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
  onDelete,
  onSelect,
  onPageChange,
  rowSelection,
  onRowSelectionChange,
  selectAllAcrossPages,
  onSelectAllAcrossPagesChange,
}: FeatureListProps<Produto>) {
  const listRef = React.useRef<HTMLDivElement>(null);
  useFeatureHotkeys({ onAdd, listRef });

  const columns: ColumnDef<Produto>[] = [
    getSelectColumn<Produto>(),
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
      header: "Estoque",
      cell: ({ row }) => row.getValue("estoqueTotal") || 0,
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
    getActionsColumn<Produto>({ onEdit, onDelete, selectionMode, onSelect }),
  ];

  return (
    <div ref={listRef} className="flex-1 min-h-0 flex flex-col h-full">
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
