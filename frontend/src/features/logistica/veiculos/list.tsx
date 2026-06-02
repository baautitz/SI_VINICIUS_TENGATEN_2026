"use client";

import { FeatureHeader } from "@/components/ui/feature-header";
import { Check, Car, Pencil, Trash2 } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import { Checkbox } from "@/components/ui/checkbox";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { VeiculoResumo } from "./types";

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
}: FeatureListProps<VeiculoResumo>) {
  const columns: ColumnDef<VeiculoResumo>[] = [
    {
      id: "select",
      header: ({ table }) => (
        <Checkbox
          checked={
            table.getIsAllPageRowsSelected() ||
            (table.getIsSomePageRowsSelected() && "indeterminate")
          }
          onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
          aria-label="Selecionar tudo"
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          checked={row.getIsSelected()}
          onCheckedChange={(value) => row.toggleSelected(!!value)}
          aria-label="Selecionar linha"
        />
      ),
      enableHiding: false,
      size: 50,
    },
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
            <span className="font-medium">{item.placa} / {item.estadoSigla}</span>
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
      accessorKey: "transportadoraNome",
      header: "Transportadora",
      cell: ({ row }) => (
        <span className="text-muted-foreground">{row.getValue("transportadoraNome") || "N/A"}</span>
      ),
    },
    {
      accessorKey: "ativo",
      header: "Status",
      cell: ({ row }) => {
        const ativo = row.getValue("ativo") as boolean;
        return (
          <span className={ativo ? "text-green-600" : "text-red-600"}>
            {ativo ? "Ativo" : "Inativo"}
          </span>
        );
      },
    },
    {
      id: "actions",
      header: () => <div className="text-right px-4">Ações</div>,
      cell: ({ row }) => {
        const item = row.original;
        return (
          <div className="flex justify-end gap-2 px-4 items-center">
            <Button
              size="icon-sm"
              variant="outline"
              onClick={() => onEdit(item)}
            >
              <Pencil className="h-4 w-4" />
            </Button>
            <Button
              size="icon-sm"
              variant="destructive"
              onClick={() => onDelete(item)}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
            {selectionMode && (
              <Button
                size="sm"
                variant="secondary"
                onClick={() => onSelect?.(item)}
              >
                <Check className="mr-2 h-4 w-4" />
                Selecionar
              </Button>
            )}
          </div>
        );
      },
    },
  ];

  return (
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
      />
    </FeatureLayout>
  );
}
