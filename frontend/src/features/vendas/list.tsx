"use client";

import * as React from "react";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { FeatureHeader } from "@/components/ui/feature-header";
import { ShoppingBag, Eye, Trash2 } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
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
}: VendasListProps) {
  const listRef = React.useRef<HTMLDivElement>(null);
  useFeatureHotkeys({ onAdd, listRef });

  const columns: ColumnDef<Venda>[] = [
    {
      accessorKey: "id",
      header: "Código",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">#{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "dataVenda",
      header: "Data/Hora",
      cell: ({ row }) => (
        <span>{formatToLocal(row.getValue("dataVenda"))}</span>
      ),
    },
    {
      accessorKey: "cliente.nomeRazaoSocial",
      header: "Cliente",
      cell: ({ row }) => (
        <span className="font-medium">{row.original.cliente?.nomeRazaoSocial}</span>
      ),
    },
    {
      accessorKey: "emitente.nomeRazaoSocial",
      header: "Emitente",
      cell: ({ row }) => (
        <span className="text-muted-foreground">{row.original.emitente?.nomeRazaoSocial}</span>
      ),
    },
    {
      id: "quantidadeItens",
      header: () => <div className="text-right">Itens</div>,
      cell: ({ row }) => (
        <div className="text-right text-xs font-medium">{row.original.itens?.length ?? 0}</div>
      ),
    },
    {
      accessorKey: "valorTotal",
      header: () => <div className="text-right">Valor Total</div>,
      cell: ({ row }) => {
        const total = Number(row.getValue("valorTotal"));
        return (
          <div className={cn(
            "text-right font-semibold text-emerald-600 dark:text-emerald-400",
            row.original.dataCancelamento && "line-through text-muted-foreground/60 dark:text-muted-foreground/50"
          )}>
            {new Intl.NumberFormat("pt-BR", {
              style: "currency",
              currency: "BRL",
            }).format(total)}
          </div>
        );
      },
    },
    {
      accessorKey: "dataCancelamento",
      header: "Status",
      size: 100,
      cell: ({ row }) => {
        const isCanceled = !!row.original.dataCancelamento;
        return (
          <Badge variant={isCanceled ? "destructive" : "outline"}>
            {isCanceled ? "Cancelada" : "Confirmada"}
          </Badge>
        );
      },
    },
    {
      id: "actions",
      header: () => <div className="px-4 text-right">Ações</div>,
      cell: ({ row }) => {
        const item = row.original;
        const isCanceled = !!item.dataCancelamento;
        return (
          <div className="flex justify-end gap-2 px-4">
            <Button
              size="icon-sm"
              variant="outline"
              title="Visualizar Detalhes"
              onClick={() => onView(item)}
            >
              <Eye className="h-4 w-4" />
            </Button>
            {!isCanceled && (
              <Button
                size="icon-sm"
                variant="destructive"
                title="Cancelar Venda"
                onClick={() => onDelete(item)}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
          </div>
        );
      },
    },
  ];

  return (
    <div ref={listRef} className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader
          title="Vendas"
          icon={<ShoppingBag />}
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
          totalItems={totalItems}
          globalFilter={searchTerm}
          onGlobalFilterChange={onSearchChange}
          searchPlaceholder="Pesquisar vendas por cliente ou emitente..."
          getRowId={(row) => row.id.toString()}
          onEditRow={onView}
          onDeleteRow={onDelete}
        />
      </FeatureLayout>
    </div>
  );
}
