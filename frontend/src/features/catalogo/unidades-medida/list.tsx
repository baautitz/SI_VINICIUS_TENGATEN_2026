"use client";

import { getActionsColumn } from "@/utils/table-columns";
import { FeatureHeader } from "@/ui/composites";
import { Scale } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/ui/composites";
import { FeatureLayout } from "@/ui/composites";
import { Badge } from "@/ui/primitives";
import { UnidadeMedida } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function UnidadesMedidaList({
  items: unidades,
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
}: FeatureListProps<UnidadeMedida>) {
  const columns: ColumnDef<UnidadeMedida>[] = [
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
      accessorKey: "permiteDecimais",
      header: "Decimais",
      cell: ({ row }) => (row.getValue("permiteDecimais") ? "Sim" : "Não"),
    },
    {
      accessorKey: "ativo",
      header: "Status",
      size: 100,
      cell: ({ row }) => {
        const ativo = row.getValue("ativo") as boolean;
        return (
          <Badge variant={ativo ? "default" : "secondary"}>
            {ativo ? "Ativo" : "Inativo"}
          </Badge>
        );
      },
    },
    getActionsColumn<UnidadeMedida>({
      onEdit,
      onDelete,
      selectionMode,
      onSelect,
    }),
  ];

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader
          title="Unidades de Medida"
          icon={<Scale />}
          onAdd={onAdd}
          addButtonLabel="Nova Unidade"
        />

        <DataTable
          columns={columns}
          data={unidades}
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
