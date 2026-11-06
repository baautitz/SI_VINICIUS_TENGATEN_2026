"use client";

import { combinacaoDe } from "@/lib/atalhos";
import React from "react";
import { ColumnDef } from "@tanstack/react-table";
import { FileUp, PackagePlus } from "lucide-react";
import { Badge, Button, HotkeyKbd } from "@/ui/primitives";
import { DataTable, FeatureLayout } from "@/ui/composites";
import { FeatureHeader } from "@/ui/composites";
import { useWindowCommands } from "@/ui/imperative";
import { getActionsColumn, RowAction } from "@/utils/table-columns";
import { fireAndForget } from "@/lib/utils";
import { formatDateToLocal } from "@/utils/date-utils";
import type { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { compraSemSaldoDevolvivel, moeda, type Compra } from "./types";

interface ComprasListProps extends FeatureListProps<Compra> {
  onView: (item: Compra) => void;
  onImport: () => void;
  onReturn: (item: Compra) => void;
}

export function ComprasList({
  items,
  loading,
  searchTerm,
  page,
  totalPages,
  totalItems,
  onSearchChange,
  onAdd,
  onView,
  onDelete,
  onImport,
  onReturn,
  onPageChange,
}: ComprasListProps) {
  useWindowCommands([
    {
      id: "compras.importar-xml",
      hotkey: combinacaoDe("importarXml"),
      label: "Importar XML",
      run: (e) => {
        e.preventDefault();
        onImport();
      },
    },
  ]);

  const columns: ColumnDef<Compra>[] = [
    {
      accessorKey: "id",
      header: "Código",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">#{row.original.id}</span>
      ),
    },
    {
      id: "status",
      header: "Situação",
      size: 110,
      cell: ({ row }) => {
        const c = row.original;
        if (c.estornada) return <Badge variant="destructive">Estornada</Badge>;
        if (compraSemSaldoDevolvivel(c)) return <Badge variant="secondary">Devolvida</Badge>;
        if (c.itens.some((i) => Number(i.quantidadeDevolvida) > 0))
          return <Badge variant="outline">Devolvida parcialmente</Badge>;
        return null;
      },
    },
    {
      accessorKey: "dataCompra",
      header: "Data",
      cell: ({ row }) => formatDateToLocal(row.original.dataCompra),
    },
    {
      id: "fornecedor",
      header: "Fornecedor",
      cell: ({ row }) => (
        <span className="font-medium">
          {row.original.fornecedor?.nomeRazaosocial}
        </span>
      ),
    },
    {
      id: "emitente",
      header: "Emitente",
      cell: ({ row }) => (
        <span className="text-muted-foreground">
          {row.original.emitente?.nomeRazaoSocial}
        </span>
      ),
    },
    {
      id: "itens",
      header: () => <div className="text-right">Itens</div>,
      cell: ({ row }) => (
        <div className="text-right">{row.original.itens?.length ?? 0}</div>
      ),
    },
    {
      accessorKey: "valorTotal",
      header: () => <div className="text-right">Custo Total</div>,
      cell: ({ row }) => (
        <div className="text-right font-semibold">
          {moeda(Number(row.original.valorTotal))}
        </div>
      ),
    },
    getActionsColumn<Compra>({
      onView,
      extra: (item) => (
        <>
          {!compraSemSaldoDevolvivel(item) && (
            <RowAction kind="devolver" label="Devolver itens ao fornecedor" onClick={() => onReturn(item)} />
          )}
          {!item.estornada && (
            <RowAction kind="estornar" label="Estornar compra" onClick={() => onDelete(item)} />
          )}
        </>
      ),
    }),
  ];

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader
          title="Compras"
          icon={<PackagePlus />}
          onAdd={onAdd}
          addButtonLabel="Nova Compra"
          actions={
            <Button
              variant="outline"
              className="rounded-lg"
              onClick={() => fireAndForget(async () => onImport())}
            >
              <FileUp /> Importar XML <HotkeyKbd hotkey={combinacaoDe("importarXml")} />
            </Button>
          }
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
          searchPlaceholder="Pesquisar compras por fornecedor..."
          getRowId={(row) => row.id.toString()}
          onEditRow={onView}
          onDeleteRow={onDelete}
        />
      </FeatureLayout>
    </div>
  );
}
