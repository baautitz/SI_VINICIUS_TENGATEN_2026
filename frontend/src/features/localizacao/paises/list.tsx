"use client";

import { getActionsColumn } from "@/utils/table-columns";
import { FeatureHeader } from "@/ui/composites";
import { Globe } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/ui/composites";

import { FeatureLayout } from "@/ui/composites";
import { Pais } from "./types";

import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function PaisesList({
  items: paises,
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
}: FeatureListProps<Pais>) {
  const columns: ColumnDef<Pais>[] = [
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "pais",
      header: "País",
      cell: ({ row }) => (
        <span className="font-medium">{row.getValue("pais")}</span>
      ),
    },
    {
      accessorKey: "codigoIsoPais",
      header: "Código ISO do país",
    },
    {
      accessorKey: "ddi",
      header: "DDI",
    },
    {
      accessorKey: "codigoIsoMoeda",
      header: "Código ISO da moeda",
    },
    {
      accessorKey: "simboloMoeda",
      header: "Símbolo",
    },
    getActionsColumn<Pais>({ onEdit, onView, onDelete, selectionMode, onSelect }),
  ];

  return (
    <div className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
      <FeatureHeader
        title="Países"
        icon={<Globe />}
        onAdd={onAdd}
        addButtonLabel="Novo País"
      />

      <DataTable
        columns={columns}
        data={paises}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por nome ou sigla..."
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
