"use client";

import * as React from "react";

import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Check, ClipboardList, Pencil, Trash2, Eye, Ban } from "lucide-react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { Badge } from "@/components/ui/badge";
import {
  MovimentacaoEstoque,
  tipoMovimentacaoLabels,
  statusLabels,
} from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { formatToLocal } from "@/utils/date-utils";

interface MovimentacoesListProps extends FeatureListProps<MovimentacaoEstoque> {
  onConfirm: (item: MovimentacaoEstoque) => void;
  onCancel: (item: MovimentacaoEstoque) => void;
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
  onEdit,
  onDelete,
  onConfirm,
  onCancel,
  onView,
  onPageChange,
}: MovimentacoesListProps) {
  const listRef = React.useRef<HTMLDivElement>(null);
  useFeatureHotkeys({ onAdd, listRef });

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
      accessorKey: "status",
      header: "Status",
      cell: ({ row }) => {
        const status = row.getValue("status") as string;
        let variant: "default" | "secondary" | "destructive" | "outline" =
          "secondary";
        let className = "";

        if (status === "CONFIRMADA") {
          className =
            "bg-emerald-500 hover:bg-emerald-600 text-white border-none";
          variant = "default";
        } else if (status === "CANCELADA") {
          variant = "destructive";
        }

        return (
          <Badge variant={variant} className={className}>
            {statusLabels[status] || status}
          </Badge>
        );
      },
    },
    {
      accessorKey: "observacao",
      header: "Observação",
      cell: ({ row }) => (
        <span className="text-muted-foreground block max-w-xs truncate">
          {row.getValue("observacao") || "-"}
        </span>
      ),
    },
    {
      id: "valorTotal",
      header: () => <div className="text-right">Valor Total</div>,
      cell: ({ row }) => {
        const item = row.original;
        const total =
          item.valorTotal ??
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
    {
      id: "actions",
      header: () => <div className="px-4 text-right">Ações</div>,
      cell: ({ row }) => {
        const item = row.original;
        const isRascunho = item.status === "RASCUNHO";
        const isConfirmada = item.status === "CONFIRMADA";

        return (
          <div className="flex justify-end gap-2 px-4">
            {isRascunho ? (
              <>
                <Button
                  size="icon-sm"
                  variant="outline"
                  title="Editar Rascunho"
                  onClick={() => onEdit(item)}
                >
                  <Pencil className="h-4 w-4" />
                </Button>
                <Button
                  size="icon-sm"
                  variant="outline"
                  className="text-green-600 hover:text-green-700 dark:text-green-400 dark:hover:text-green-300"
                  title="Efetivar Movimentação"
                  onClick={() => onConfirm(item)}
                >
                  <Check className="h-4 w-4" />
                </Button>
                <Button
                  size="icon-sm"
                  variant="destructive"
                  title="Excluir Rascunho"
                  onClick={() => onDelete(item)}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </>
            ) : (
              <>
                <Button
                  size="icon-sm"
                  variant="outline"
                  title="Visualizar Detalhes"
                  onClick={() => onView(item)}
                >
                  <Eye className="h-4 w-4" />
                </Button>
                {isConfirmada && (
                  <Button
                    size="icon-sm"
                    variant="outline"
                    className="text-red-600 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
                    title="Estornar/Cancelar"
                    onClick={() => onCancel(item)}
                  >
                    <Ban className="h-4 w-4" />
                  </Button>
                )}
              </>
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
          title="Movimentações de Estoque"
          icon={<ClipboardList />}
          onAdd={onAdd}
          addButtonLabel="Nova Movimentação"
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
          onEditRow={(item) => {
            if (item.status === "RASCUNHO") {
              onEdit(item);
            } else {
              onView(item);
            }
          }}
          onDeleteRow={(item) => {
            if (item.status === "CONFIRMADA") {
              onCancel(item);
            } else if (item.status === "RASCUNHO") {
              onDelete(item);
            }
          }}
        />
      </FeatureLayout>
    </div>
  );
}
