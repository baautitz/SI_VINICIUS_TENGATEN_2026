"use client";

import { StatusBadge } from "@/components/ui/status-badge";
import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useHotkeys } from "react-hotkeys-hook";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Scale } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/components/ui/data-table";

import { FeatureLayout } from "@/components/ui/feature-layout";

import { UnidadeMedidaResumo } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function UnidadesMedidaList({
  items: unidadesMedida,
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
}: FeatureListProps<UnidadeMedidaResumo>) {
  useHotkeys("alt+n", (e) => {
    e.preventDefault();
    if (!selectionMode && document.querySelector('[role="dialog"]')) return;
    onAdd();
  }, { enableOnFormTags: true }, [selectionMode, onAdd]);

  const columns: ColumnDef<UnidadeMedidaResumo>[] = [
    getSelectColumn<UnidadeMedidaResumo>(),
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "sigla",
      header: "Sigla",
      cell: ({ row }) => (
        <span className="font-medium">{row.getValue("sigla")}</span>
      ),
    },
    {
      accessorKey: "descricao",
      header: "Descrição",
    },
    {
      accessorKey: "categoria",
      header: "Categoria",
    },
    {
      accessorKey: "ativo",
      header: "Status",
      cell: ({ row }) => <StatusBadge ativo={row.getValue("ativo") as boolean} />,
    },
    getActionsColumn<UnidadeMedidaResumo>({ onEdit, onDelete, selectionMode, onSelect }),
  ];

  return (
    <FeatureLayout>
      <FeatureHeader
        title="Unidades de Medida"
        icon={<Scale />}
        onAdd={onAdd}
        addButtonLabel="Nova Unidade"
      />

      <DataTable
        columns={columns}
        data={unidadesMedida}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por sigla, descrição ou categoria..."
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
