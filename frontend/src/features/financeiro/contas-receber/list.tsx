"use client";

import { origemTituloLabels } from "../origem";
import React from "react";
import { ColumnDef, flexRender } from "@tanstack/react-table";
import { Button } from "@/ui/primitives";
import { DataTable } from "@/ui/composites";
import { FeatureLayout } from "@/ui/composites";
import { FeatureHeader } from "@/ui/composites";
import { getActionsColumn } from "@/utils/table-columns";
import { StatusTituloBadge } from "../components/status-titulo-badge";
import { formatDateToLocal } from "@/utils/date-utils";
import { Landmark, Check } from "lucide-react";
import { ContasReceber, ContasReceberParcela } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { fireAndForget } from "@/lib/utils";

interface ContasReceberListProps extends FeatureListProps<ContasReceber> {
  onBaixa: (contaId: number, parcela: ContasReceberParcela) => void;
}

export function ContasReceberList({
  items: contas,
  loading,
  searchTerm,
  page,
  totalPages,
  totalItems,
  onSearchChange,
  onAdd,
  onEdit,
  onDelete,
  onView,
  onPageChange,
  onBaixa,
}: ContasReceberListProps) {
  const extra = (item: ContasReceber) => {
    const firstUnpaid = item.contasReceberParcelas
      ?.filter((p) => p.status === "ABERTO" || p.status === "PARCIAL")
      ?.sort((a, b) => a.numeroParcela - b.numeroParcela)[0];
    return (
      firstUnpaid && (
        <Button
          size="icon-sm"
          variant="ghost"
          aria-label="Receber parcela"
          title="Receber parcela"
          onClick={() => fireAndForget(() => onBaixa(item.id, firstUnpaid))}
        >
          <Check />
        </Button>
      )
    );
  };
  const editColumn = getActionsColumn({ onEdit, onDelete, extra });
  const viewColumn = getActionsColumn({ onView, extra });

  const columns: ColumnDef<ContasReceber>[] = [
    {
      accessorKey: "id",
      header: "Código",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold text-foreground">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "descricao",
      header: "Descrição",
      cell: ({ row }) => (
        <span className="font-medium text-foreground truncate max-w-xs block">
          {row.getValue("descricao")}
        </span>
      ),
    },
    {
      id: "cliente",
      header: "Cliente",
      cell: ({ row }) => {
        const cliente = row.original.cliente;
        return (
          <span className="text-muted-foreground truncate max-w-xs block">
            {cliente?.nomeRazaoSocial || "-"}
          </span>
        );
      },
    },
    {
      id: "origem",
      header: "Origem",
      cell: ({ row }) => (
        <span>
          {origemTituloLabels[row.original.origemTipo] ?? row.original.origemTipo}
          {row.original.origemId ? <span className="text-muted-foreground"> #{row.original.origemId}</span> : null}
        </span>
      ),
    },
    {
      accessorKey: "dataEmissao",
      header: "Emissão",
      cell: ({ row }) => {
        const val = row.getValue("dataEmissao") as string;
        return <span>{val ? formatDateToLocal(val) : "-"}</span>;
      },
    },
    {
      id: "vencimento",
      header: "Vencimento",
      cell: ({ row }) => {
        const item = row.original;
        if (item.status === "PAGO") {
          return <span className="text-muted-foreground text-xs font-semibold">Quitado</span>;
        }
        const abertas = item.contasReceberParcelas?.filter(
          (p) => p.status === "ABERTO" || p.status === "PARCIAL"
        );
        if (!abertas || abertas.length === 0) {
          return <span>-</span>;
        }
        const maisProxima = [...abertas].sort(
          (a, b) => new Date(a.dataVencimento).getTime() - new Date(b.dataVencimento).getTime()
        )[0];

        const dateStr = formatDateToLocal(maisProxima.dataVencimento);
        const hoje = new Date();
        const hojeLocal = `${hoje.getFullYear()}-${String(hoje.getMonth() + 1).padStart(2, "0")}-${String(hoje.getDate()).padStart(2, "0")}`;
        const isAtrasada = String(maisProxima.dataVencimento).slice(0, 10) < hojeLocal;

        return (
          <span className={isAtrasada ? "text-destructive font-bold" : "text-foreground font-semibold"}>
            {dateStr}
          </span>
        );
      },
    },
    {
      accessorKey: "valorOriginal",
      header: () => <div className="text-right font-semibold">Original</div>,
      cell: ({ row }) => {
        const valor = Number(row.getValue("valorOriginal") || 0);
        return (
          <div className="text-right font-semibold text-foreground">
            {new Intl.NumberFormat("pt-BR", {
              style: "currency",
              currency: "BRL",
            }).format(valor)}
          </div>
        );
      },
    },
    {
      accessorKey: "valorSaldo",
      header: () => <div className="text-right font-semibold">Saldo</div>,
      cell: ({ row }) => {
        const valor = Number(row.getValue("valorSaldo") || 0);
        return (
          <div className="text-right font-semibold text-foreground">
            {new Intl.NumberFormat("pt-BR", {
              style: "currency",
              currency: "BRL",
            }).format(valor)}
          </div>
        );
      },
    },
    {
      accessorKey: "status",
      header: "Status",
      cell: ({ row }) => <StatusTituloBadge status={row.getValue("status") as string} />,
    },
    {
      id: "actions",
      header: () => <div className="text-right px-4">Ações</div>,
      cell: (ctx) => {
        const item = ctx.row.original;
        const canEdit = item.status === "ABERTO" || item.status === "PARCIAL";
        return flexRender((canEdit ? editColumn : viewColumn).cell, ctx);
      },
    },
  ];

  return (
    <div className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
        <FeatureHeader
          title="Contas a Receber"
          icon={<Landmark />}
          onAdd={onAdd}
          addButtonLabel="Nova Conta"
        />

        <DataTable
          columns={columns}
          data={contas}
          loading={loading}
          pageCount={totalPages}
          pageIndex={page}
          onPageChange={onPageChange}
          totalItems={totalItems}
          globalFilter={searchTerm}
          onGlobalFilterChange={onSearchChange}
          searchPlaceholder="Pesquisar por descrição ou cliente..."
          getRowId={(row) => row.id.toString()}
          onEditRow={(item) => {
            if (item.status === "ABERTO" || item.status === "PARCIAL") {
              onEdit(item);
            } else {
              onView(item);
            }
          }}
          onDeleteRow={(item) => {
            if (item.status === "ABERTO" || item.status === "PARCIAL") {
              onDelete(item);
            }
          }}
        />
      </FeatureLayout>
    </div>
  );
}
