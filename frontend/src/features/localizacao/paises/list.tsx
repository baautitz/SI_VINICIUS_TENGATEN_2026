"use client";

import { FeatureHeader } from "@/components/ui/feature-header";
import { Check, Globe, Pencil, Trash2 } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import { Checkbox } from "@/components/ui/checkbox";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { PaisDto } from "./types";

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
  onDelete,
  onSelect,
  onPageChange,
  rowSelection,
  onRowSelectionChange,
}: FeatureListProps<PaisDto>) {
  const columns: ColumnDef<PaisDto>[] = [
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
    },
    {
      accessorKey: "id",
      header: "ID",
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
      accessorKey: "siglaIso",
      header: "Sigla ISO",
    },
    {
      accessorKey: "ddi",
      header: "DDI",
    },
    {
      accessorKey: "moeda",
      header: "Moeda",
    },
    {
      accessorKey: "simboloMoeda",
      header: "Símbolo",
    },
    {
      id: "actions",
      header: () => <div className="text-right px-4">Ações</div>,
      cell: ({ row }) => {
        const item = row.original;
        return (
          <div className="flex justify-end gap-2 px-4">
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
      />
    </FeatureLayout>
  );
}
