"use client";

import * as React from "react";

import { StatusBadge } from "@/ui/composites";
import { getActionsColumn } from "@/utils/table-columns";
import { FeatureHeader } from "@/ui/composites";
import { Car } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/ui/composites";

import { FeatureLayout } from "@/ui/composites";
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
  onView,
  onDelete,
  onSelect,
  onPageChange,
  ordenacao,
  onOrdenacaoChange,
  rowSelection,
  onRowSelectionChange,
  selectAllAcrossPages,
  onSelectAllAcrossPagesChange,
}: FeatureListProps<Veiculo>) {
  const columns: ColumnDef<Veiculo>[] = [
    {
      accessorKey: "id", meta: { campoOrdenacao: "id" },
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "placa", meta: { campoOrdenacao: "placa" },
      header: "Placa",
      cell: ({ row }) => {
        const item = row.original;
        return (
          <div className="flex flex-col">
            <span className="font-medium">
              {item.placa} / {item.estado.uf}
            </span>
            {item.marcaModelo && (
              <span className="text-sm text-muted-foreground">
                {item.marcaModelo}
              </span>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: "transportadora", meta: { campoOrdenacao: "transportadora" },
      header: "Transportadora",
      cell: ({ row }) => {
        const item = row.original;
        return (
          <span className="text-muted-foreground">
            {item.transportadora?.nomeRazaosocial || "N/A"}
          </span>
        );
      },
    },
    {
      accessorKey: "ativo", meta: { campoOrdenacao: "ativo" },
      header: "Status",
      cell: ({ row }) => <StatusBadge ativo={row.getValue("ativo") as boolean} />,
    },
    getActionsColumn<Veiculo>({
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
          ordenacao={ordenacao}
          onOrdenacaoChange={onOrdenacaoChange}
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
