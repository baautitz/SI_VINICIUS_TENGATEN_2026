"use client";

import * as React from "react";
import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Landmark } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/components/ui/data-table";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { Badge } from "@/components/ui/badge";
import { MetodoPagamento } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function MetodosList({
  items: metodos,
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
}: FeatureListProps<MetodoPagamento>) {
  const listRef = React.useRef<HTMLDivElement>(null);
  useFeatureHotkeys({ onAdd, listRef });

  const columns: ColumnDef<MetodoPagamento>[] = [
    getSelectColumn<MetodoPagamento>(),
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "codigo",
      header: "Código",
      size: 100,
      cell: ({ row }) => (
        <span className="font-mono font-medium">{row.getValue("codigo")}</span>
      ),
    },
    {
      accessorKey: "descricao",
      header: "Descrição",
      cell: ({ row }) => (
        <span className="font-medium">{row.getValue("descricao")}</span>
      ),
    },
    {
      accessorKey: "ativo",
      header: "Status",
      size: 100,
      cell: ({ row }) => {
        const ativo = row.getValue("ativo") as boolean;
        return (
          <Badge variant={ativo ? "outline" : "secondary"}>
            {ativo ? "Ativo" : "Inativo"}
          </Badge>
        );
      },
    },
    getActionsColumn<MetodoPagamento>({ onEdit, onDelete, selectionMode, onSelect }),
  ];

  return (
    <div ref={listRef} className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
        <FeatureHeader
          title="Métodos de Pagamento"
          icon={<Landmark />}
          onAdd={onAdd}
          addButtonLabel="Novo Método"
        />

        <DataTable
          columns={columns}
          data={metodos}
          loading={loading}
          pageCount={totalPages}
          pageIndex={page}
          onPageChange={onPageChange}
          totalItems={totalItems}
          globalFilter={searchTerm}
          onGlobalFilterChange={onSearchChange}
          searchPlaceholder="Pesquisar por código ou descrição..."
          rowSelection={rowSelection}
          onRowSelectionChange={rowSelection ? onRowSelectionChange : undefined}
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
