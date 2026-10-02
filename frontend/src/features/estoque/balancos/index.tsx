"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ColumnDef } from "@tanstack/react-table";
import { Ban, Eye, ListChecks } from "lucide-react";
import { balancosApi } from "@/api/estoque";
import { useFeatureList } from "@/hooks/use-feature-list";
import { useUi } from "@/ui/imperative";
import { Badge, Button } from "@/ui/primitives";
import { DataTable, FeatureHeader, FeatureLayout } from "@/ui/composites";
import { formatToLocal } from "@/utils/date-utils";
import { fireAndForget } from "@/lib/utils";
import { Balanco, statusBalancoLabels } from "./types";
import { BalancoWindow, type BalancoWindowProps } from "./window";

export function BalancosFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Balanco>();
  const { data, isLoading } = useQuery({
    queryKey: ["balancos", list.page],
    queryFn: async () => {
      const res = await balancosApi.list(list.page, 50);
      return {
        itens: res?.itens ?? [],
        totalPages: res?.totalDePaginas ?? 1,
        totalItems: res?.totalDeItens ?? 0,
      };
    },
  });

  const invalidate = () =>
    Promise.all(
      ["balancos", "movimentacoes", "skus", "produtos"].map((key) =>
        queryClient.invalidateQueries({ queryKey: [key] }),
      ),
    );

  const abrir = async (item: Balanco) => {
    const result = await ui.windows.open<true, BalancoWindowProps>({
      component: BalancoWindow,
      props: { balancoId: item.id },
      title: `Balanço #${item.id}`,
      size: "full",
    });
    await invalidate();
    if (result.status === "confirmed")
      ui.feedback.notify({ type: "success", title: "Balanço fechado e ajustes lançados." });
  };

  // Abre o balanço com todos os SKUs ativos (saldo do sistema congelado na abertura).
  // ponytail: sem escolha de SKUs na tela; a API já aceita "skus" para contagem parcial.
  const novo = async () => {
    const ok = await ui.windows.confirm({
      title: "Novo balanço",
      description: "Abrir um balanço com todos os SKUs ativos? Você informará a contagem física na próxima tela.",
      confirmLabel: "Abrir balanço",
    });
    if (!ok) return;
    try {
      const res = await balancosApi.create({});
      if (res.success === false || !res.data) {
        ui.feedback.notifyError(res, { fallbackTitle: "Não foi possível abrir o balanço." });
        return;
      }
      await invalidate();
      await abrir(res.data);
    } catch (error) {
      ui.feedback.notifyError(error, { fallbackTitle: "Não foi possível abrir o balanço." });
    }
  };

  const cancelar = async (item: Balanco) => {
    const ok = await ui.windows.confirm({
      title: "Cancelar balanço",
      description: `Cancelar o balanço #${item.id}? Nenhum ajuste será lançado.`,
      confirmLabel: "Cancelar balanço",
      confirmVariant: "destructive",
    });
    if (!ok) return;
    try {
      const res = await balancosApi.cancelar(item.id);
      if (res.success === false) {
        ui.feedback.notifyError(res, { fallbackTitle: "Não foi possível cancelar o balanço." });
        return;
      }
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Balanço cancelado." });
    } catch (error) {
      ui.feedback.notifyError(error, { fallbackTitle: "Não foi possível cancelar o balanço." });
    }
  };

  const columns: ColumnDef<Balanco>[] = [
    { accessorKey: "id", header: "Código", size: 80, cell: ({ row }) => <span className="font-semibold">{row.original.id}</span> },
    { accessorKey: "dataAbertura", header: "Abertura", cell: ({ row }) => formatToLocal(row.original.dataAbertura) },
    {
      accessorKey: "dataFechamento",
      header: "Fechamento",
      cell: ({ row }) => (row.original.dataFechamento ? formatToLocal(row.original.dataFechamento) : "-"),
    },
    {
      accessorKey: "status",
      header: "Status",
      cell: ({ row }) => {
        const status = row.original.status;
        return (
          <Badge
            variant={status === "CANCELADO" ? "destructive" : status === "FECHADO" ? "default" : "secondary"}
            className={status === "FECHADO" ? "border-none bg-emerald-500 text-white hover:bg-emerald-600" : ""}
          >
            {statusBalancoLabels[status]}
          </Badge>
        );
      },
    },
    { id: "itens", header: "Itens", cell: ({ row }) => row.original.itens.length },
    {
      accessorKey: "observacao",
      header: "Observação",
      cell: ({ row }) => <span className="text-muted-foreground">{row.original.observacao || "-"}</span>,
    },
    {
      id: "actions",
      header: () => <div className="px-4 text-right">Ações</div>,
      cell: ({ row }) => (
        <div className="flex justify-end gap-2 px-4">
          <Button size="icon-sm" variant="outline" title={row.original.status === "ABERTO" ? "Contar" : "Visualizar"} onClick={() => fireAndForget(() => abrir(row.original))}>
            <Eye className="h-4 w-4" />
          </Button>
          {row.original.status === "ABERTO" && (
            <Button size="icon-sm" variant="outline" className="text-red-600" title="Cancelar balanço" onClick={() => fireAndForget(() => cancelar(row.original))}>
              <Ban className="h-4 w-4" />
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <FeatureHeader title="Balanços de Estoque" icon={<ListChecks />} onAdd={novo} addButtonLabel="Novo Balanço" />
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
          searchPlaceholder="Pesquisar balanços..."
          getRowId={(row) => row.id.toString()}
          onEditRow={abrir}
          onDeleteRow={(item) => {
            if (item.status === "ABERTO") fireAndForget(() => cancelar(item));
          }}
        />
      </FeatureLayout>
    </div>
  );
}
