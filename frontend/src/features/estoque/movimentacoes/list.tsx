"use client";

import * as React from "react";

import { FeatureHeader } from "@/ui/composites";
import { ClipboardList, Undo2 } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/ui/primitives";
import { DataTable } from "@/ui/composites";
import { FeatureLayout } from "@/ui/composites";
import { Badge } from "@/ui/primitives";
import {
  MovimentacaoEstoque,
  origemMovimentacaoLabels,
  podeEstornar,
  tipoMovimentacaoLabels,
} from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { formatToLocal } from "@/utils/date-utils";
import { fireAndForget } from "@/lib/utils";
import { getActionsColumn } from "@/utils/table-columns";

interface MovimentacoesListProps extends Omit<FeatureListProps<MovimentacaoEstoque>, "onEdit" | "onDelete"> {
  onEstornar: (item: MovimentacaoEstoque) => void;
  onView: (item: MovimentacaoEstoque) => void;
}

export function MovimentacoesList({
  items: movimentacoes,
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
}: MovimentacoesListProps) {
  const columns: ColumnDef<MovimentacaoEstoque>[] = [
    {
      accessorKey: "id",
      header: "Código",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "dataMovimentacao",
      header: "Data/Hora",
      cell: ({ row }) => (
        <span>{formatToLocal(row.getValue("dataMovimentacao"))}</span>
      ),
    },
    {
      accessorKey: "tipoMovimentacao",
      header: "Tipo",
      cell: ({ row }) => {
        const tipo = row.getValue("tipoMovimentacao") as string;
        return (
          <span className="font-medium">
            {tipoMovimentacaoLabels[tipo] || tipo}
          </span>
        );
      },
    },
    {
      id: "origem",
      header: "Origem",
      cell: ({ row }) => {
        const item = row.original;
        return (
          <span className="flex items-center gap-2">
            {origemMovimentacaoLabels[item.origemTipo] ?? item.origemTipo}
            {item.origemId ? (
              <span className="text-muted-foreground">#{item.origemId}</span>
            ) : null}
            {item.estornada && <Badge variant="destructive">Estornada</Badge>}
          </span>
        );
      },
    },
    {
      accessorKey: "motivo",
      header: "Motivo / Observação",
      cell: ({ row }) => (
        <span className="text-muted-foreground block max-w-xs truncate">
          {row.original.motivo || row.original.observacao || "-"}
        </span>
      ),
    },
    {
      id: "valorTotal",
      header: () => <div className="text-right">Valor Total</div>,
      cell: ({ row }) => {
        const item = row.original;
        const total =
          item.totalCusto ??
          item.movimentacoesEstoquesItens?.reduce(
            (sum, i) => sum + Number(i.quantidade) * Number(i.custoUnitario),
            0,
          );

        return (
          <div className="text-right font-medium">
            {new Intl.NumberFormat("pt-BR", {
              style: "currency",
              currency: "BRL",
            }).format(total || 0)}
          </div>
        );
      },
    },
    getActionsColumn<MovimentacaoEstoque>({
      onView,
      extra: (item) =>
        podeEstornar(item) && (
          <Button
            size="icon-sm"
            variant="outline"
            aria-label="Estornar"
            onClick={() => fireAndForget(() => onEstornar(item))}
          >
            <Undo2 className="size-4" />
          </Button>
        ),
    }),
  ];

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader
          title="Movimentações de Estoque"
          icon={<ClipboardList />}
          onAdd={onAdd}
          addButtonLabel="Lançamento Manual"
        />

        <DataTable
          columns={columns}
          data={movimentacoes}
          loading={loading}
          pageCount={totalPages}
          pageIndex={page}
          onPageChange={onPageChange}
          totalItems={totalItems}
          globalFilter={searchTerm}
          onGlobalFilterChange={onSearchChange}
          searchPlaceholder="Pesquisar movimentações..."
          getRowId={(row) => row.id.toString()}
          onEditRow={onView}
          onDeleteRow={onEstornar}
        />
      </FeatureLayout>
    </div>
  );
}
