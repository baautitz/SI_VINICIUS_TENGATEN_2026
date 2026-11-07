"use client";

import * as React from "react";
import { FeatureHeader } from "@/ui/composites";
import { ShoppingBag } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/ui/composites";
import { FeatureLayout } from "@/ui/composites";
import { Badge } from "@/ui/primitives";
import { getActionsColumn, RowAction } from "@/utils/table-columns";
import type { Venda } from "./types";
import type { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { formatToLocal } from "@/utils/date-utils";

interface VendasListProps extends FeatureListProps<Venda> {
  onView: (item: Venda) => void;
}

export function VendasList({
  items: vendas,
  loading,
  searchTerm,
  page,
  totalPages,
  totalItems,
  onSearchChange,
  onAdd,
  onDelete,
  onView,
  onPageChange,
  ordenacao,
  onOrdenacaoChange,
  icon,
  selectionMode = false,
  onSelect,
}: VendasListProps) {
  const columns: ColumnDef<Venda>[] = [
    {
      accessorKey: "id", meta: { campoOrdenacao: "id" },
      header: "Código",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">#{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "dataVenda", meta: { campoOrdenacao: "dataVenda" },
      header: "Data/Hora",
      cell: ({ row }) => (
        <span>{formatToLocal(row.getValue("dataVenda"))}</span>
      ),
    },
    {
      accessorKey: "cliente.nomeRazaoSocial", meta: { campoOrdenacao: "cliente" },
      header: "Cliente",
      cell: ({ row }) => (
        <span className="font-medium">{row.original.cliente?.nomeRazaoSocial}</span>
      ),
    },
    {
      accessorKey: "emitente.nomeRazaoSocial", meta: { campoOrdenacao: "emitente" },
      header: "Emitente",
      cell: ({ row }) => (
        <span className="text-muted-foreground">{row.original.emitente?.nomeRazaoSocial}</span>
      ),
    },
    {
      id: "quantidadeItens", meta: { campoOrdenacao: "quantidadeItens" },
      header: () => <div className="text-right">Itens</div>,
      cell: ({ row }) => (
        <div className="text-right text-sm font-medium">{row.original.itens?.length ?? 0}</div>
      ),
    },
    {
      accessorKey: "valorTotal", meta: { campoOrdenacao: "valorTotal" },
      header: () => <div className="text-right">Valor Total</div>,
      cell: ({ row }) => {
        const total = Number(row.getValue("valorTotal"));
        return (
          <div className={"text-right font-semibold"}>
            {new Intl.NumberFormat("pt-BR", {
              style: "currency",
              currency: "BRL",
            }).format(total)}
          </div>
        );
      },
    },
    {
      accessorKey: "statusDevolucao",
      header: "Status",
      size: 100,
      cell: ({ row }) => {
        return (
          <Badge
            variant={
              row.original.statusDevolucao === "TOTAL"
                ? "destructive"
                : row.original.statusDevolucao === "PARCIAL"
                  ? "info"
                  : "success"
            }
          >
            {row.original.statusDevolucao === "TOTAL"
              ? "Devolvida"
              : row.original.statusDevolucao === "PARCIAL"
                ? "Devolvida parcialmente"
                : "Confirmada"}
          </Badge>
        );
      },
    },
    getActionsColumn<Venda>({
      onView,
      selectionMode,
      onSelect,
      extra: (item) =>
        !selectionMode &&
        item.statusDevolucao !== "TOTAL" && (
          <RowAction kind="estornar" label="Devolver itens" onClick={() => onDelete(item)} />
        ),
    }),
  ];

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader
          title="Vendas"
          icon={icon ?? <ShoppingBag />}
          onAdd={onAdd}
          addButtonLabel="Registrar Venda"
        />

        <DataTable
          columns={columns}
          data={vendas}
          loading={loading}
          pageCount={totalPages}
          pageIndex={page}
          onPageChange={onPageChange}
          ordenacao={ordenacao}
          onOrdenacaoChange={onOrdenacaoChange}
          totalItems={totalItems}
          globalFilter={searchTerm}
          onGlobalFilterChange={onSearchChange}
          searchPlaceholder="Pesquisar vendas por cliente ou emitente..."
          getRowId={(row) => row.id.toString()}
          onEditRow={onView}
          onDeleteRow={selectionMode ? undefined : onDelete}
          onRowSelect={selectionMode ? onSelect : undefined}
        />
      </FeatureLayout>
    </div>
  );
}
