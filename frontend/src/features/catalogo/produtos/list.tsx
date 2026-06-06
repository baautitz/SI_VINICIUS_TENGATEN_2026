"use client";

import { FeatureHeader } from "@/components/ui/feature-header";
import { Check, Package, Pencil, Trash2 } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import { Checkbox } from "@/components/ui/checkbox";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { Badge } from "@/components/ui/badge";
import { ProdutoResumo } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function ProdutosList({
  items: produtos,
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
}: FeatureListProps<ProdutoResumo>) {
  const columns: ColumnDef<ProdutoResumo>[] = [
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
        <span className="font-semibold text-muted-foreground">#{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "produto",
      header: "Produto",
      cell: ({ row }) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-medium text-foreground">{row.getValue("produto")}</span>
        </div>
      ),
    },
    {
      accessorKey: "descricao",
      header: "Descrição",
      cell: ({ row }) => {
        const desc: string = row.getValue("descricao") || "";
        return (
          <span className="text-muted-foreground text-sm line-clamp-1 max-w-sm">
            {desc || "-"}
          </span>
        );
      },
    },
    {
      accessorKey: "ativo",
      header: "Status",
      size: 100,
      cell: ({ row }) => {
        const active: boolean = row.getValue("ativo");
        return (
          <Badge variant={active ? "default" : "secondary"} className={active ? "bg-emerald-500 hover:bg-emerald-600 text-white border-none" : ""}>
            {active ? "Ativo" : "Inativo"}
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
              title="Editar produto"
            >
              <Pencil className="h-4 w-4" />
            </Button>
            <Button
              size="icon-sm"
              variant="destructive"
              onClick={() => onDelete(item)}
              title="Excluir produto"
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
        title="Produtos"
        icon={<Package />}
        onAdd={onAdd}
        addButtonLabel="Novo Produto"
      />

      <DataTable
        columns={columns}
        data={produtos}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por nome ou descrição..."
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
