"use client";

import * as React from "react";

import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Sliders } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/components/ui/data-table";

import { FeatureLayout } from "@/components/ui/feature-layout";
import { SkuAtributoChaveResumo } from "./types";
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
}: FeatureListProps<SkuAtributoChaveResumo>) {
  const listRef = React.useRef<HTMLDivElement>(null);
  useFeatureHotkeys({ onAdd, listRef });

  const columns: ColumnDef<SkuAtributoChaveResumo>[] = [
    getSelectColumn<SkuAtributoChaveResumo>(),
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
      cell: ({ row }) => (
        <span className="font-medium">{row.getValue("chave")}</span>
      ),
    },
    {
      accessorKey: "valores",
      header: "Valores Possíveis",
      cell: ({ row }) => {
        const valores: string[] = row.getValue("valores") || [];
        return (
          <div className="flex flex-wrap gap-1 max-w-125">
            {valores.length > 0 ? (
              valores.map((v, i) => (
                <span
                  key={i}
                  className="inline-flex items-center px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-800 dark:text-slate-200 text-xs font-semibold"
                >
                  {v}
                </span>
              ))
            ) : (
              <span className="text-muted-foreground text-xs italic">
                Nenhum valor cadastrado
              </span>
            )}
          </div>
        );
      },
    },
    getActionsColumn<SkuAtributoChaveResumo>({ onEdit, onDelete, selectionMode, onSelect }),
  ];

  return (
    <div ref={listRef} className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
      <FeatureHeader
        title="Atributos"
        icon={<Sliders />}
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
        searchPlaceholder="Pesquisar por atributo..."
        rowSelection={rowSelection}
        onRowSelectionChange={onRowSelectionChange}
        selectAllAcrossPages={selectAllAcrossPages}
        onSelectAllAcrossPagesChange={onSelectAllAcrossPagesChange}
        getRowId={(row) => row.id.toString()}
        onEditRow={onEdit}
        onDeleteRow={onDelete}
        />
    </FeatureLayout>
    </div>
  );
}
