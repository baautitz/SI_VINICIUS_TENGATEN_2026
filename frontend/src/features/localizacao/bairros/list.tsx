"use client";

import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { FeatureHeader } from "@/ui/composites";
import { Milestone } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/ui/composites";

import { FeatureLayout } from "@/ui/composites";
import { Bairro } from "./types";

import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function BairrosList({
  items: bairros,
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
}: FeatureListProps<Bairro>) {
  const columns: ColumnDef<Bairro>[] = [
    getSelectColumn<Bairro>(),
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "bairro",
      header: "Bairro",
      cell: ({ row }) => (
        <span className="font-medium">{row.getValue("bairro")}</span>
      ),
    },
    {
      accessorKey: "cidade",
      header: "Cidade",
      cell: ({ row }) => {
        const item = row.original;
        return `${item.cidade.cidade} (${item.cidade.estado.uf})`;
      },
    },
    getActionsColumn<Bairro>({ onEdit, onView, onDelete, selectionMode, onSelect }),
  ];

  return (
    <div className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
      <FeatureHeader
        title="Bairros"
        icon={<Milestone />}
        onAdd={onAdd}
        addButtonLabel="Novo Bairro"
      />

      <DataTable
        columns={columns}
        data={bairros}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por bairro ou cidade..."
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
