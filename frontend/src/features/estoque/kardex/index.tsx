"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { ColumnDef } from "@tanstack/react-table";
import { BookOpen } from "lucide-react";
import { estoqueApi } from "@/api/estoque";
import { useFeatureList } from "@/hooks/use-feature-list";
import { MovimentacoesUpsert, type MovimentacoesUpsertProps } from "@/features/estoque/movimentacoes/upsert";
import {
  KardexLinha,
  origemMovimentacaoLabels,
  tipoMovimentacaoLabels,
} from "@/features/estoque/movimentacoes/types";
import { DataTable, FeatureHeader, FeatureLayout } from "@/ui/composites";
import { useUi } from "@/ui/imperative";
import { formatToLocal } from "@/utils/date-utils";
import { cn } from "@/lib/utils";

const fmt = (n: number) => Number(n).toLocaleString("pt-BR", { maximumFractionDigits: 4 });
const brl = (n: number) => Number(n).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

// Kardex: razão de estoque por SKU, com saldo antes/depois de cada movimentação.
export function KardexFeature() {
  const ui = useUi();
  const list = useFeatureList<KardexLinha>();
  const { data, isLoading } = useQuery({
    queryKey: ["movimentacoes", "kardex", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await estoqueApi.kardex(list.deferredSearch.trim() || undefined, list.page, 50);
      return {
        itens: res?.itens ?? [],
        totalPages: res?.totalDePaginas ?? 1,
        totalItems: res?.totalDeItens ?? 0,
      };
    },
  });

  const openMovimentacao = async (linha: KardexLinha) => {
    const movimentacao = await estoqueApi.getById(linha.movimentacaoId);
    await ui.windows.open<true, MovimentacoesUpsertProps>({
      component: MovimentacoesUpsert,
      props: { editingItem: movimentacao, readOnly: true },
      title: `Visualizar Movimentação #${linha.movimentacaoId}`,
      size: "full",
    });
  };

  const columns: ColumnDef<KardexLinha>[] = [
    {
      accessorKey: "dataMovimentacao",
      header: "Data/Hora",
      cell: ({ row }) => formatToLocal(row.original.dataMovimentacao),
    },
    {
      accessorKey: "sku",
      header: "SKU",
      cell: ({ row }) => <span className="font-mono font-semibold">{row.original.sku}</span>,
    },
    { accessorKey: "produtoNome", header: "Produto" },
    {
      id: "origem",
      header: "Origem",
      cell: ({ row }) => (
        <span>
          {origemMovimentacaoLabels[row.original.origemTipo] ?? row.original.origemTipo}
          {row.original.origemId ? <span className="text-muted-foreground"> #{row.original.origemId}</span> : null}
        </span>
      ),
    },
    {
      accessorKey: "motivo",
      header: "Motivo",
      cell: ({ row }) => (
        <span className="text-muted-foreground block max-w-xs truncate">{row.original.motivo || "-"}</span>
      ),
    },
    {
      accessorKey: "quantidadeAnterior",
      header: () => <div className="text-right">Anterior</div>,
      cell: ({ row }) => <div className="text-muted-foreground text-right">{fmt(row.original.quantidadeAnterior)}</div>,
    },
    {
      accessorKey: "quantidade",
      header: () => <div className="text-right">Movimento</div>,
      cell: ({ row }) => {
        const entrada = row.original.tipoMovimentacao === "ENTRADA";
        return (
          <div
            className={cn("text-right font-bold", entrada ? "text-emerald-600" : "text-destructive")}
            title={tipoMovimentacaoLabels[row.original.tipoMovimentacao]}
          >
            {entrada ? "+" : "-"}
            {fmt(row.original.quantidade)}
          </div>
        );
      },
    },
    {
      accessorKey: "quantidadePosterior",
      header: () => <div className="text-right">Saldo</div>,
      cell: ({ row }) => <div className="text-right font-bold">{fmt(row.original.quantidadePosterior)}</div>,
    },
    {
      accessorKey: "custoUnitario",
      header: () => <div className="text-right">Custo Unit.</div>,
      cell: ({ row }) => <div className="text-right">{brl(row.original.custoUnitario)}</div>,
    },
  ];

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader title="Kardex" icon={<BookOpen />} />
        <DataTable
          columns={columns}
          data={data?.itens ?? []}
          loading={isLoading}
          pageCount={data?.totalPages ?? 1}
          pageIndex={list.page}
          onPageChange={list.setPage}
          totalItems={data?.totalItems ?? 0}
          globalFilter={list.searchTerm}
          onGlobalFilterChange={list.handleSearchChange}
          searchPlaceholder="Pesquisar por SKU ou produto..."
          getRowId={(row) => `${row.movimentacaoId}-${row.sku}`}
          onEditRow={(row) => void openMovimentacao(row)}
        />
      </FeatureLayout>
    </div>
  );
}
