"use client";

import * as React from "react";
import { Undo2 } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Badge } from "@/ui/primitives";
import { DataTable, FeatureLayout, FeatureHeader } from "@/ui/composites";
import { getActionsColumn, RowAction } from "@/utils/table-columns";
import type { DevolucaoCompraResumo } from "@/features/compras/types";
import type { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { formatToLocal } from "@/utils/date-utils";

interface DevolucoesCompraListProps extends Omit<FeatureListProps<DevolucaoCompraResumo>, "onAdd" | "onEdit" | "onDelete"> {
  onView: (item: DevolucaoCompraResumo) => void;
  onEstornar: (item: DevolucaoCompraResumo) => void | Promise<void>;
}

const brl = (v: number) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(v);

export function DevolucoesCompraList({
  items,
  loading,
  searchTerm,
  page,
  totalPages,
  totalItems,
  onSearchChange,
  onView,
  onEstornar,
  onPageChange,
  ordenacao,
  onOrdenacaoChange,
  icon,
}: DevolucoesCompraListProps) {
  const columns: ColumnDef<DevolucaoCompraResumo>[] = [
    {
      accessorKey: "id", meta: { campoOrdenacao: "id" },
      header: "Número",
      size: 80,
      cell: ({ row }) => <span className="font-semibold">#{row.original.id}</span>,
    },
    {
      id: "status", meta: { campoOrdenacao: "status" },
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
      accessorKey: "dataDevolucao", meta: { campoOrdenacao: "dataDevolucao" },
      header: "Data",
      cell: ({ row }) => <span>{formatToLocal(row.original.dataDevolucao)}</span>,
    },
    {
      accessorKey: "compraId", meta: { campoOrdenacao: "compraId" },
      header: "Compra de origem",
      cell: ({ row }) => <span>#{row.original.compraId}</span>,
    },
    {
      accessorKey: "fornecedorNome", meta: { campoOrdenacao: "fornecedorNome" },
      header: "Fornecedor",
      cell: ({ row }) => <span className="font-medium">{row.original.fornecedorNome}</span>,
    },
    {
      accessorKey: "valorTotal", meta: { campoOrdenacao: "valorTotal" },
      header: () => <div className="text-right">Valor Total</div>,
      cell: ({ row }) => (
        <div className="text-right font-semibold">{brl(Number(row.original.valorTotal))}</div>
      ),
    },
    {
      accessorKey: "motivo", meta: { campoOrdenacao: "motivo" },
      header: "Motivo",
      cell: ({ row }) => (
        <span className="text-muted-foreground">{row.original.motivo}</span>
      ),
    },
    getActionsColumn<DevolucaoCompraResumo>({
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
          title="Devoluções de compra"
          icon={icon ?? <Undo2 />}
        />
        <DataTable
          columns={columns}
          data={items}
          loading={loading}
          pageCount={totalPages}
          pageIndex={page}
          onPageChange={onPageChange}
          ordenacao={ordenacao}
          onOrdenacaoChange={onOrdenacaoChange}
          totalItems={totalItems}
          globalFilter={searchTerm}
          onGlobalFilterChange={onSearchChange}
          searchPlaceholder="Pesquisar devoluções por fornecedor ou motivo..."
          getRowId={(row) => row.id.toString()}
          onEditRow={onView}
          onEstornarRow={onEstornar}
          podeEstornarRow={(item) => item.status !== "ESTORNADA"}
        />
      </FeatureLayout>
    </div>
  );
}
