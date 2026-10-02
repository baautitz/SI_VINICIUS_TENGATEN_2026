"use client";

import { getActionsColumn } from "@/utils/table-columns";
import { FeatureHeader } from "@/ui/composites";
import { Map } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/ui/composites";

import { FeatureLayout } from "@/ui/composites";
import { Estado } from "./types";

import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function EstadosList({
  items: estados,
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
}: FeatureListProps<Estado>) {
  const columns: ColumnDef<Estado>[] = [
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "estado",
      header: "Estado",
      cell: ({ row }) => (
        <span className="font-medium">{row.getValue("estado")}</span>
      ),
    },
    {
      accessorKey: "uf",
      header: "UF",
    },
    {
      accessorKey: "pais",
      header: "País",
      cell: ({ row }) => row.original.pais.pais,
    },
    getActionsColumn<Estado>({ onEdit, onView, onDelete, selectionMode, onSelect }),
  ];

  return (
    <div className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
      <FeatureHeader
        title="Estados"
        icon={<Map />}
        onAdd={onAdd}
        addButtonLabel="Novo Estado"
      />

      <DataTable
        columns={columns}
        data={estados}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por estado, UF ou país..."
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
