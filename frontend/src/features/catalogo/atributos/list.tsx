"use client";

import * as React from "react";
import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { FeatureHeader } from "@/components/ui/feature-header";
import { SlidersHorizontal } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/components/ui/data-table";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { SkuAtributoChave } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function AtributosList({
  items: atributos,
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
}: FeatureListProps<SkuAtributoChave>) {
  const listRef = React.useRef<HTMLDivElement>(null);
  useFeatureHotkeys({ onAdd, listRef });

  const columns: ColumnDef<SkuAtributoChave>[] = [
    getSelectColumn<SkuAtributoChave>(),
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "chave",
      header: "Atributo",
    },
    {
      accessorKey: "skuAtributosValores",
      header: "Valores",
      cell: ({ row }) => {
        const item = row.original;
        return (
          <span className="text-muted-foreground truncate block max-w-sm">
            {item.skuAtributosValores?.map(v => v.valor).join(", ")}
          </span>
        );
      },
    },
    getActionsColumn<SkuAtributoChave>({ onEdit, onDelete, selectionMode, onSelect }),
  ];

  return (
    <div ref={listRef} className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
        <FeatureHeader
          title="Atributos"
          icon={<SlidersHorizontal />}
          onAdd={onAdd}
          addButtonLabel="Novo Atributo"
        />

        <DataTable
          columns={columns}
          data={atributos}
          loading={loading}
          pageCount={totalPages}
          pageIndex={page}
          onPageChange={onPageChange}
          totalItems={totalItems}
          globalFilter={searchTerm}
          onGlobalFilterChange={onSearchChange}
          searchPlaceholder="Pesquisar..."
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
