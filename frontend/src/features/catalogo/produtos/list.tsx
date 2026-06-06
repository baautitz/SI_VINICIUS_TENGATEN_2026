"use client";

import * as React from "react";

import { StatusBadge } from "@/components/ui/status-badge";
import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Package } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/components/ui/data-table";

import { FeatureLayout } from "@/components/ui/feature-layout";

import { ProdutoResumo } from "./types";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
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
}: FeatureListProps<ProdutoResumo>) {
  const listRef = React.useRef<HTMLDivElement>(null);

  useFeatureHotkeys({
    onAdd,
    listRef,
  });

  const columns: ColumnDef<ProdutoResumo>[] = [
    getSelectColumn<ProdutoResumo>(),
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold text-muted-foreground">
          #{row.getValue("id")}
        </span>
      ),
    },
    {
      accessorKey: "produto",
      header: "Produto",
      cell: ({ row }) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-medium text-foreground">
            {row.getValue("produto")}
          </span>
        </div>
      ),
    },
    {
      accessorKey: "descricao",
      header: "Descrição",
      cell: ({ row }) => {
        const desc: string = row.getValue("descricao") || "";
        return (
          <span className="text-muted-foreground text-sm line-clamp-1 max-w-sm">
            {desc || "-"}
          </span>
        );
      },
    },
    {
      accessorKey: "estoqueTotal",
      header: "Estoque",
      size: 100,
      cell: ({ row }) => (
        <span className="font-semibold text-muted-foreground">
          {Number(row.original.estoqueTotal ?? 0).toLocaleString()}
        </span>
      ),
    },
    {
      accessorKey: "ativo",
      header: "Status",
      size: 100,
      cell: ({ row }) => (
        <StatusBadge ativo={row.getValue("ativo") as boolean} />
      ),
    },
    getActionsColumn<ProdutoResumo>({
      onEdit,
      onDelete,
      selectionMode,
      onSelect,
    }),
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
          searchPlaceholder="Pesquisar por nome ou descrição..."
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
