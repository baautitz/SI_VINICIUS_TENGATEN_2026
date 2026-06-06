"use client";

import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useHotkeys } from "react-hotkeys-hook";
import { FeatureHeader } from "@/components/ui/feature-header";
import { MapPin } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/components/ui/data-table";

import { FeatureLayout } from "@/components/ui/feature-layout";
import { CidadeDto } from "./types";

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
  onDelete,
  onSelect,
  onPageChange,
  rowSelection,
  onRowSelectionChange,
  selectAllAcrossPages,
  onSelectAllAcrossPagesChange,
}: FeatureListProps<CidadeDto>) {
  useHotkeys("alt+n", (e) => {
    e.preventDefault();
    if (!selectionMode && document.querySelector('[role="dialog"]')) return;
    onAdd();
  }, { enableOnFormTags: true }, [selectionMode, onAdd]);

  const columns: ColumnDef<CidadeDto>[] = [
    getSelectColumn<CidadeDto>(),
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
      accessorKey: "estadoNome",
      header: "Estado",
      cell: ({ row }) => {
        const item = row.original;
        return `${item.estadoNome} (${item.uf})`;
      },
    },
    getActionsColumn<CidadeDto>({ onEdit, onDelete, selectionMode, onSelect }),
  ];

  return (
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
      />
    </FeatureLayout>
  );
}
