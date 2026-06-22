"use client";

import * as React from "react";

import { StatusBadge } from "@/components/ui/status-badge";
import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Car } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/components/ui/data-table";

import { FeatureLayout } from "@/components/ui/feature-layout";
import { Veiculo } from "./types";

import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function VeiculosList({
  items: veiculos,
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
}: FeatureListProps<Veiculo>) {
  const listRef = React.useRef<HTMLDivElement>(null);
  useFeatureHotkeys({ onAdd, listRef });

  const columns: ColumnDef<Veiculo>[] = [
    getSelectColumn<Veiculo>(),
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "placa",
      header: "Placa",
      cell: ({ row }) => {
        const item = row.original;
        return (
          <div className="flex flex-col">
            <span className="font-medium">{item.placa} / {item.estado.uf}</span>
            {item.marcaModelo && (
              <span className="text-xs text-muted-foreground">
                {item.marcaModelo}
              </span>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: "transportadora",
      header: "Transportadora",
      cell: ({ row }) => {
        const item = row.original;
        return (
          <span className="text-muted-foreground">{item.transportadora?.nomeRazaosocial || "N/A"}</span>
        );
      },
    },
    {
      accessorKey: "ativo",
      header: "Status",
      cell: ({ row }) => <StatusBadge ativo={row.getValue("ativo") as boolean} />,
    },
    getActionsColumn<Veiculo>({ onEdit, onDelete, selectionMode, onSelect }),
  ];

  return (
    <div ref={listRef} className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
      <FeatureHeader
        title="Veículos"
        icon={<Car />}
        onAdd={onAdd}
        addButtonLabel="Novo Veículo"
      />

      <DataTable
        columns={columns}
        data={veiculos}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por placa ou marca/modelo..."
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
