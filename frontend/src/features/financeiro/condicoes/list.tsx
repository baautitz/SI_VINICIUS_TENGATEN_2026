"use client";

import * as React from "react";
import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Receipt } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/components/ui/data-table";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { Badge } from "@/components/ui/badge";
import { CondicaoPagamento } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function CondicoesList({
  items: condicoes,
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
}: FeatureListProps<CondicaoPagamento>) {
  const listRef = React.useRef<HTMLDivElement>(null);
  useFeatureHotkeys({ onAdd, listRef });

  const columns: ColumnDef<CondicaoPagamento>[] = [
    getSelectColumn<CondicaoPagamento>(),
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
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
      accessorKey: "metodoPagamento.descricao",
      header: "Método de Pagamento",
      cell: ({ row }) => (
        <span>{row.original.metodoPagamento?.descricao ?? "-"}</span>
      ),
    },
    {
      accessorKey: "entradaMinimaPercentual",
      header: () => <div className="text-right">Entrada Mín.</div>,
      size: 110,
      cell: ({ row }) => {
        const val = row.getValue("entradaMinimaPercentual") as number;
        return <div className="text-right font-medium">{val.toFixed(2)}%</div>;
      },
    },
    {
      accessorKey: "descontoPercentual",
      header: () => <div className="text-right">Desc. / Acrésc.</div>,
      size: 140,
      cell: ({ row }) => {
        const desc = row.original.descontoPercentual;
        const acr = row.original.acrescimoPercentual;
        return (
          <div className="flex flex-col text-right text-xs">
            <span className="font-medium text-emerald-600">
              Desc: {desc.toFixed(2)}%
            </span>
            <span className="font-medium text-amber-600">
              Acrésc: {acr.toFixed(2)}%
            </span>
          </div>
        );
      },
    },
    {
      accessorKey: "taxaJurosPercentual",
      header: () => <div className="text-right">Juros / Multa</div>,
      size: 140,
      cell: ({ row }) => {
        const juros = row.original.taxaJurosPercentual;
        const multa = row.original.multaPercentual;
        return (
          <div className="text-muted-foreground flex flex-col text-right text-xs">
            <span>Juros: {juros.toFixed(2)}%</span>
            <span>Multa: {multa.toFixed(2)}%</span>
          </div>
        );
      },
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
    getActionsColumn<CondicaoPagamento>({
      onEdit,
      onDelete,
      selectionMode,
      onSelect,
    }),
  ];

  return (
    <div ref={listRef} className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader
          title="Condições de Pagamento"
          icon={<Receipt />}
          onAdd={onAdd}
          addButtonLabel="Nova Condição"
        />

        <DataTable
          columns={columns}
          data={condicoes}
          loading={loading}
          pageCount={totalPages}
          pageIndex={page}
          onPageChange={onPageChange}
          totalItems={totalItems}
          globalFilter={searchTerm}
          onGlobalFilterChange={onSearchChange}
          searchPlaceholder="Pesquisar por descrição..."
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
