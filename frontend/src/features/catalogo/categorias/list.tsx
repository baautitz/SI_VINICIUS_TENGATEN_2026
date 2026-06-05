"use client";

import { FeatureHeader } from "@/components/ui/feature-header";
import { Check, Layers, Pencil, Trash2 } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import { Checkbox } from "@/components/ui/checkbox";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { Badge } from "@/components/ui/badge";
import { CategoriaResumo } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function CategoriasList({
  items: categorias,
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
}: FeatureListProps<CategoriaResumo>) {
  const columns: ColumnDef<CategoriaResumo>[] = [
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
      accessorKey: "categoria",
      header: "Categoria",
      cell: ({ row }) => (
        <span className="font-medium">{row.getValue("categoria")}</span>
      ),
    },
    {
      accessorKey: "ativo",
      header: "Status",
      cell: ({ row }) => {
        const ativo = row.getValue("ativo");
        return (
          <Badge
            variant={ativo ? "default" : "secondary"}
            className={ativo ? "bg-emerald-500 hover:bg-emerald-600 text-white border-none" : ""}
          >
            {ativo ? "Ativo" : "Inativo"}
          </Badge>
        );
      },
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
        title="Categorias"
        icon={<Layers />}
        onAdd={onAdd}
        addButtonLabel="Nova Categoria"
      />

      <DataTable
        columns={columns}
        data={categorias}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por categoria..."
        rowSelection={rowSelection}
        onRowSelectionChange={onRowSelectionChange}
        selectAllAcrossPages={selectAllAcrossPages}
        onSelectAllAcrossPagesChange={onSelectAllAcrossPagesChange}
        getRowId={(row) => row.id.toString()}
      />
    </FeatureLayout>
  );
}
