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
import { EstornoWindow, type EstornoWindowProps } from "@/features/estoque/movimentacoes";
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

  const abrir = async (item: Balanco | null) => {
    const result = await ui.windows.open<"saved" | "closed", BalancoWindowProps>({
      component: BalancoWindow,
      props: { balancoId: item?.id ?? null },
      title: item ? `Balanço #${item.id}` : "Novo Balanço de Estoque",
      size: "full",
    });
    if (result.status !== "confirmed") return;
    await invalidate();
    ui.feedback.notify({
      type: "success",
      title: result.value === "closed" ? "Balanço fechado e ajustes lançados." : "Balanço salvo como rascunho.",
    });
  };

  // Aberto: só descarta. Fechado: estorna as movimentações geradas, então pede o motivo.
  const cancelar = async (item: Balanco) => {
    let motivo: string | undefined;
    if (item.status === "FECHADO") {
      const result = await ui.windows.open<string, EstornoWindowProps>({
        component: EstornoWindow,
        props: {
          pergunta: `Deseja realmente cancelar o balanço #${item.id}?`,
          aviso: "As movimentações de ajuste geradas pelo balanço serão estornadas.",
        },
        title: "Cancelar Balanço",
        size: "small",
      });
      if (result.status !== "confirmed") return;
      motivo = result.value;
    } else {
      const ok = await ui.windows.confirm({
        title: "Cancelar balanço",
        description: `Cancelar o balanço #${item.id}? Nenhum ajuste será lançado.`,
        confirmLabel: "Cancelar balanço",
        confirmVariant: "destructive",
      });
      if (!ok) return;
    }
    try {
      const res = await balancosApi.cancelar(item.id, motivo);
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
          {row.original.status !== "CANCELADO" && (
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
        <FeatureHeader title="Balanços de Estoque" icon={<ListChecks />} onAdd={() => abrir(null)} addButtonLabel="Novo Balanço" />
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
            if (item.status !== "CANCELADO") fireAndForget(() => cancelar(item));
          }}
        />
      </FeatureLayout>
    </div>
  );
}
