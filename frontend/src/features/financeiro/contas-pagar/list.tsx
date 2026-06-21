"use client";

import React, { useRef } from "react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import { FeatureLayout } from "@/components/ui/feature-layout";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Badge } from "@/components/ui/badge";
import { useFeatureHotkeys } from "@/hooks/use-feature-hotkeys";
import { formatDateToLocal } from "@/utils/date-utils";
import { Landmark, Pencil, Trash2, Eye, Coins } from "lucide-react";
import { ContasPagar, ContasPagarParcela, statusTituloLabels, StatusTituloFinanceiro } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

interface ContasPagarListProps extends FeatureListProps<ContasPagar> {
  onBaixa: (contaId: number, parcela: ContasPagarParcela) => void;
}

export function ContasPagarList({
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
}: ContasPagarListProps) {
  const listRef = useRef<HTMLDivElement>(null);
  useFeatureHotkeys({ onAdd, listRef });

  const columns: ColumnDef<ContasPagar>[] = [
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
      id: "fornecedor",
      header: "Fornecedor",
      cell: ({ row }) => {
        const fornecedor = row.original.fornecedor;
        return (
          <span className="text-muted-foreground truncate max-w-xs block">
            {fornecedor?.nomeRazaosocial || "-"}
          </span>
        );
      },
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
        const parcelas = item.contasPagarParcelas;

        if (!parcelas || parcelas.length === 0) {
          return <span className="text-muted-foreground">-</span>;
        }

        if (item.status === "PAGO") {
          // Exibe a data de vencimento da última parcela
          const ultima = [...parcelas].sort(
            (a, b) => new Date(b.dataVencimento).getTime() - new Date(a.dataVencimento).getTime()
          )[0];
          return (
            <span className="text-muted-foreground">
              {formatDateToLocal(ultima.dataVencimento)}
            </span>
          );
        }

        // Acha o menor vencimento das parcelas em aberto ou parcial
        const abertas = parcelas.filter(
          (p) => p.status === "ABERTO" || p.status === "PARCIAL"
        );
        if (!abertas || abertas.length === 0) {
          return <span className="text-muted-foreground">-</span>;
        }
        const maisProxima = [...abertas].sort(
          (a, b) => new Date(a.dataVencimento).getTime() - new Date(b.dataVencimento).getTime()
        )[0];

        const dateStr = formatDateToLocal(maisProxima.dataVencimento);
        const isAtrasada = new Date(maisProxima.dataVencimento).getTime() < new Date().setHours(0, 0, 0, 0);

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
      cell: ({ row }) => {
        const status = row.getValue("status") as string;
        let variant: "default" | "secondary" | "destructive" | "outline" = "secondary";
        let className = "";

        if (status === "PAGO") {
          className = "bg-emerald-500 hover:bg-emerald-600 text-white border-none";
          variant = "default";
        } else if (status === "PARCIAL") {
          className = "bg-blue-500 hover:bg-blue-600 text-white border-none";
          variant = "default";
        } else if (status === "ABERTO") {
          className = "bg-amber-500 hover:bg-amber-600 text-white border-none";
          variant = "default";
        } else if (status === "CANCELADO") {
          variant = "destructive";
        }

        return (
          <Badge variant={variant} className={className}>
            {statusTituloLabels[status as StatusTituloFinanceiro] || status}
          </Badge>
        );
      },
    },
    {
      id: "actions",
      header: () => <div className="text-right px-4">Ações</div>,
      cell: ({ row }) => {
        const item = row.original;
        const canEdit = item.status === "ABERTO" || item.status === "PARCIAL";
        const firstUnpaid = item.contasPagarParcelas
          ?.filter((p) => p.status === "ABERTO" || p.status === "PARCIAL")
          ?.sort((a, b) => a.numeroParcela - b.numeroParcela)[0];

        return (
          <div className="flex justify-end gap-2 px-4">
            {firstUnpaid && (
              <Button
                size="icon-sm"
                variant="outline"
                className="text-green-600 hover:text-green-700 hover:bg-green-50 dark:hover:bg-green-950 dark:text-green-400"
                title="Pagar Parcela"
                onClick={() => onBaixa(item.id, firstUnpaid)}
              >
                <Coins className="h-4 w-4" />
              </Button>
            )}
            {canEdit ? (
              <>
                <Button
                  size="icon-sm"
                  variant="outline"
                  title="Editar Conta"
                  onClick={() => onEdit(item)}
                >
                  <Pencil className="h-4 w-4" />
                </Button>
                <Button
                  size="icon-sm"
                  variant="destructive"
                  title="Excluir Conta"
                  onClick={() => onDelete(item)}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </>
            ) : (
              <Button
                size="icon-sm"
                variant="outline"
                title="Visualizar Conta"
                onClick={() => onView(item)}
              >
                <Eye className="h-4 w-4" />
              </Button>
            )}
          </div>
        );
      },
    },
  ];

  return (
    <div ref={listRef} className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
        <FeatureHeader
          title="Contas a Pagar"
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
          searchPlaceholder="Pesquisar por descrição ou fornecedor..."
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
