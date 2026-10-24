"use client";

import * as React from "react";
import { Undo2 } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Badge } from "@/ui/primitives";
import { DataTable, FeatureLayout, FeatureHeader } from "@/ui/composites";
import { getActionsColumn, RowAction } from "@/utils/table-columns";
import type { DevolucaoResumo } from "@/features/vendas/types";
import type { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { formatToLocal } from "@/utils/date-utils";

interface DevolucoesListProps extends FeatureListProps<DevolucaoResumo> {
  onView: (item: DevolucaoResumo) => void;
  onEstornar: (item: DevolucaoResumo) => void | Promise<void>;
}

const brl = (v: number) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(v);

export function DevolucoesList({
  items,
  loading,
  searchTerm,
  page,
  totalPages,
  totalItems,
  onSearchChange,
  onAdd,
  onView,
  onEstornar,
  onPageChange,
  icon,
}: DevolucoesListProps) {
  const columns: ColumnDef<DevolucaoResumo>[] = [
    {
      accessorKey: "id",
      header: "Número",
      size: 80,
      cell: ({ row }) => <span className="font-semibold">#{row.original.id}</span>,
    },
    {
      id: "status",
      header: "Status",
      size: 110,
      cell: ({ row }) =>
        row.original.status === "ESTORNADA" ? (
          <Badge variant="destructive">Estornada</Badge>
        ) : (
          <Badge variant="secondary">Ativa</Badge>
        ),
    },
    {
      accessorKey: "dataDevolucao",
      header: "Data",
      cell: ({ row }) => <span>{formatToLocal(row.original.dataDevolucao)}</span>,
    },
    {
      accessorKey: "vendaId",
      header: "Venda de origem",
      cell: ({ row }) => <span>#{row.original.vendaId}</span>,
    },
    {
      accessorKey: "clienteNome",
      header: "Cliente",
      cell: ({ row }) => <span className="font-medium">{row.original.clienteNome}</span>,
    },
    {
      accessorKey: "valorTotal",
      header: () => <div className="text-right">Valor Total</div>,
      cell: ({ row }) => (
        <div className="text-right font-semibold">{brl(Number(row.original.valorTotal))}</div>
      ),
    },
    {
      accessorKey: "motivo",
      header: "Motivo",
      cell: ({ row }) => (
        <span className="text-muted-foreground">{row.original.motivo}</span>
      ),
    },
    getActionsColumn<DevolucaoResumo>({
      onView,
      extra: (item) =>
        item.status !== "ESTORNADA" && (
          <RowAction kind="estornar" label="Estornar devolução" onClick={() => onEstornar(item)} />
        ),
    }),
  ];

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader
          title="Devoluções"
          icon={icon ?? <Undo2 />}
          onAdd={onAdd}
          addButtonLabel="Nova devolução"
        />
        <DataTable
          columns={columns}
          data={items}
          loading={loading}
          pageCount={totalPages}
          pageIndex={page}
          onPageChange={onPageChange}
          totalItems={totalItems}
          globalFilter={searchTerm}
          onGlobalFilterChange={onSearchChange}
          searchPlaceholder="Pesquisar devoluções por cliente ou motivo..."
          getRowId={(row) => row.id.toString()}
          onEditRow={onView}
          onEstornarRow={onEstornar}
          podeEstornarRow={(item) => item.status !== "ESTORNADA"}
        />
      </FeatureLayout>
    </div>
  );
}
