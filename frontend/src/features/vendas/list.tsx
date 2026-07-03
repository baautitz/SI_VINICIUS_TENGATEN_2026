"use client";

import * as React from "react";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { FeatureHeader } from "@/components/ui/feature-header";
import { ShoppingBag, Eye, Trash2 } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import { FeatureLayout } from "@/components/ui/feature-layout";
import type { VendasResumo } from "./types";
import type { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { formatToLocal } from "@/utils/date-utils";

interface VendasListProps extends FeatureListProps<VendasResumo> {
  onView: (item: VendasResumo) => void;
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

  const columns: ColumnDef<VendasResumo>[] = [
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
      accessorKey: "clienteNome",
      header: "Cliente",
      cell: ({ row }) => (
        <span className="font-medium">{row.getValue("clienteNome")}</span>
      ),
    },
    {
      accessorKey: "emitenteNome",
      header: "Emitente",
      cell: ({ row }) => (
        <span className="text-muted-foreground">{row.getValue("emitenteNome")}</span>
      ),
    },
    {
      accessorKey: "quantidadeItens",
      header: () => <div className="text-right">Itens</div>,
      cell: ({ row }) => (
        <div className="text-right font-mono text-xs">{row.getValue("quantidadeItens")}</div>
      ),
    },
    {
      accessorKey: "valorTotal",
      header: () => <div className="text-right">Valor Total</div>,
      cell: ({ row }) => {
        const total = Number(row.getValue("valorTotal"));
        return (
          <div className="text-right font-semibold text-emerald-600 dark:text-emerald-400">
            {new Intl.NumberFormat("pt-BR", {
              style: "currency",
              currency: "BRL",
            }).format(total)}
          </div>
        );
      },
    },
    {
      id: "actions",
      header: () => <div className="px-4 text-right">Ações</div>,
      cell: ({ row }) => {
        const item = row.original;
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
            <Button
              size="icon-sm"
              variant="destructive"
              title="Cancelar Venda"
              onClick={() => onDelete(item)}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
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
