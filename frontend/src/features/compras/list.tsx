"use client";

import React from "react";
import { ColumnDef } from "@tanstack/react-table";
import { Eye, FileUp, PackagePlus } from "lucide-react";
import { Button, Kbd } from "@/ui/primitives";
import { DataTable, FeatureLayout } from "@/ui/composites";
import { FeatureHeader } from "@/ui/composites";
import { useWindowCommands } from "@/ui/imperative";
import { fireAndForget } from "@/lib/utils";
import { formatToLocal } from "@/utils/date-utils";
import type { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { moeda, type Compra } from "./types";

interface ComprasListProps extends FeatureListProps<Compra> {
  onView: (item: Compra) => void;
  onImport: () => void;
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
  onImport,
  onPageChange,
}: ComprasListProps) {
  useWindowCommands([
    {
      id: "compras.importar-xml",
      hotkey: "Alt+I",
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
      accessorKey: "dataCompra",
      header: "Data",
      cell: ({ row }) => formatToLocal(row.original.dataCompra),
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
    {
      id: "actions",
      header: () => <div className="px-4 text-right">Ações</div>,
      cell: ({ row }) => (
        <div className="flex justify-end gap-2 px-4">
          <Button
            size="icon-sm"
            variant="outline"
            title="Visualizar"
            onClick={() => fireAndForget(() => onView(row.original))}
          >
            <Eye className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader
          title="Compras"
          icon={<PackagePlus />}
          onAdd={onAdd}
          addButtonLabel="Nova Compra"
        />
        <div className="flex justify-end">
          <Button
            variant="outline"
            onClick={() => fireAndForget(async () => onImport())}
          >
            <FileUp /> Importar XML <Kbd>Alt+I</Kbd>
          </Button>
        </div>
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
        />
      </FeatureLayout>
    </div>
  );
}
