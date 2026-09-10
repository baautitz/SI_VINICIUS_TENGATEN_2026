"use client";

import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { FeatureHeader } from "@/ui/composites";
import { MapPin } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/ui/composites";

import { FeatureLayout } from "@/ui/composites";
import { Cidade } from "./types";

import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function CidadesList({
  items: cidades,
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
}: FeatureListProps<Cidade>) {
  const columns: ColumnDef<Cidade>[] = [
    getSelectColumn<Cidade>(),
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "cidade",
      header: "Cidade",
      cell: ({ row }) => (
        <span className="font-medium">{row.getValue("cidade")}</span>
      ),
    },
    {
      accessorKey: "ddd",
      header: "DDD",
    },
    {
      accessorKey: "estado",
      header: "Estado",
      cell: ({ row }) => {
        const item = row.original;
        return `${item.estado.estado} (${item.estado.uf})`;
      },
    },
    getActionsColumn<Cidade>({ onEdit, onView, onDelete, selectionMode, onSelect }),
  ];

  return (
    <div className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
      <FeatureHeader
        title="Cidades"
        icon={<MapPin />}
        onAdd={onAdd}
        addButtonLabel="Nova Cidade"
      />

      <DataTable
        columns={columns}
        data={cidades}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por cidade, DDD ou estado..."
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
