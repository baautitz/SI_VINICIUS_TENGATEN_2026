"use client";

import { origemTituloLabels } from "../origem";
import React from "react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/ui/primitives";
import { DataTable, FeatureLayout, FeatureHeader } from "@/ui/composites";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/ui/primitives";
import { getActionsColumn } from "@/utils/table-columns";
import { FiltrosAvancadosPopover, type FiltrosAvancados } from "../components/filtros-avancados";
import type { DocumentoFiltro } from "../components/documento-filtro";
import { StatusTituloBadge } from "../components/status-titulo-badge";
import { formatDateToLocal } from "@/utils/date-utils";
import { Landmark, Check, RotateCcw, X } from "lucide-react";
import { ContasPagar, statusTituloLabels } from "./types";
import { FeatureListProps } from "@/hooks/use-feature-orchestrator";
import { fireAndForget } from "@/lib/utils";

export const TODOS = "TODOS";

interface ContasPagarListProps extends FeatureListProps<ContasPagar> {
  status: string;
  origem: string;
  documento: DocumentoFiltro | null;
  avancados: FiltrosAvancados;
  onAvancadosChange: (v: FiltrosAvancados) => void;
  onStatusChange: (v: string) => void;
  onOrigemChange: (v: string) => void;
  onDocumentoChange: (v: DocumentoFiltro | null) => void;
  onBaixa: (item: ContasPagar) => void;
  onEstorno: (item: ContasPagar) => void;
}

const brl = (v: number) => new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(v);

const Filtro = ({ valor, onChange, rotulo, opcoes }: {
  valor: string; onChange: (v: string) => void; rotulo: string; opcoes: Record<string, string>;
}) => (
  <Select value={valor} onValueChange={onChange}>
    <SelectTrigger aria-label={rotulo} className="h-8 w-44 rounded-lg">
      <SelectValue />
    </SelectTrigger>
    <SelectContent>
      <SelectItem value={TODOS}>{rotulo}: todos</SelectItem>
      {Object.entries(opcoes).map(([k, v]) => (
        <SelectItem key={k} value={k}>{v}</SelectItem>
      ))}
    </SelectContent>
  </Select>
);

export function ContasPagarList({
  items: contas, loading, searchTerm, page, totalPages, totalItems,
  status, origem, documento, avancados, onAvancadosChange,
  onSearchChange, onAdd, onEdit, onDelete, onView, onPageChange,
  onStatusChange, onOrigemChange, onDocumentoChange, onBaixa, onEstorno,
}: ContasPagarListProps) {
  const emAberto = (i: ContasPagar) => i.status === "ABERTO" || i.status === "PARCIAL";
  // Só lançamento manual, em aberto e sem pagamento, pode ser editado/excluído.
  const editavel = (i: ContasPagar) => i.origemTipo === "MANUAL" && i.status === "ABERTO" && i.valorPago === 0;

  const extra = (item: ContasPagar) => (
    <>
      {emAberto(item) && (
        <Button size="icon-sm" variant="ghost" aria-label="Pagar parcela" title="Pagar parcela" onClick={() => fireAndForget(() => onBaixa(item))}>
          <Check />
        </Button>
      )}
      {item.valorPago > 0 && item.status !== "CANCELADO" && (
        <Button size="icon-sm" variant="ghost" aria-label="Estornar pagamento" title="Estornar pagamento" onClick={() => fireAndForget(() => onEstorno(item))}>
          <RotateCcw />
        </Button>
      )}
    </>
  );
  const acoes = getActionsColumn<ContasPagar>({ onEdit, onDelete, extra });
  const acoesLeitura = getActionsColumn<ContasPagar>({ onView, extra });

  const columns: ColumnDef<ContasPagar>[] = [
    {
      id: "documento",
      header: "Documento",
      cell: ({ row }) => {
        const i = row.original;
        return (
          <button
            type="button"
            className="text-left hover:underline"
            title="Ver todas as parcelas deste documento"
            onClick={() => onDocumentoChange({ numeroDocumento: i.numeroDocumento, origemTipo: i.origemTipo, origemId: i.origemId ?? null })}
          >
            <span className="text-muted-foreground">{origemTituloLabels[i.origemTipo] ?? i.origemTipo}</span>{" "}
            <span className="font-semibold text-foreground">{i.numeroDocumento}</span>
          </button>
        );
      },
    },
    {
      id: "parcela",
      header: "Parcela",
      size: 80,
      cell: ({ row }) => <span>{row.original.numeroParcela}/{row.original.totalParcelas}</span>,
    },
    {
      accessorKey: "descricao",
      header: "Descrição",
      cell: ({ row }) => <span className="font-medium text-foreground truncate max-w-xs block">{row.original.descricao}</span>,
    },
    {
      id: "fornecedor",
      header: "Fornecedor / Cliente",
      cell: ({ row }) => (
        <span className="text-muted-foreground truncate max-w-xs block">
          {row.original.fornecedor?.nomeRazaosocial || row.original.clienteNome || "-"}
        </span>
      ),
    },
    {
      id: "vencimento",
      header: "Vencimento",
      cell: ({ row }) => {
        const i = row.original;
        const atrasada = emAberto(i) && new Date(i.dataVencimento).getTime() < new Date().setHours(0, 0, 0, 0);
        return <span className={atrasada ? "text-destructive font-bold" : "text-foreground"}>{formatDateToLocal(i.dataVencimento)}</span>;
      },
    },
    {
      accessorKey: "valor",
      header: () => <div className="text-right font-semibold">Valor</div>,
      cell: ({ row }) => <div className="text-right font-semibold text-foreground">{brl(Number(row.original.valor))}</div>,
    },
    {
      accessorKey: "valorPago",
      header: () => <div className="text-right font-semibold">Pago</div>,
      cell: ({ row }) => <div className="text-right text-foreground">{brl(Number(row.original.valorPago))}</div>,
    },
    {
      accessorKey: "valorSaldo",
      header: () => <div className="text-right font-semibold">Saldo</div>,
      cell: ({ row }) => <div className="text-right font-semibold text-foreground">{brl(Number(row.original.valorSaldo))}</div>,
    },
    {
      accessorKey: "status",
      header: "Status",
      cell: ({ row }) => <StatusTituloBadge status={row.original.status} />,
    },
    {
      id: "actions",
      header: () => <div className="text-right px-4">Ações</div>,
      cell: (ctx) => {
        const i = ctx.row.original;
        // Editar/excluir só no manual aberto sem pagamento; senão só visualizar.
        const col = editavel(i) ? acoes : acoesLeitura;
        return col.cell && typeof col.cell === "function" ? col.cell(ctx) : null;
      },
    },
  ];

  return (
    <div className="flex-1 min-h-0 flex flex-col h-full">
      <FeatureLayout>
        <FeatureHeader title="Contas a Pagar" icon={<Landmark />} onAdd={onAdd} addButtonLabel="Nova Conta" />

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
          searchPlaceholder="Pesquisar por descrição, documento, nome, CPF/CNPJ, valor ou data..."
          getRowId={(row) => row.id.toString()}
          onEditRow={(item) => (editavel(item) ? onEdit(item) : onView(item))}
          onDeleteRow={(item) => {
            if (editavel(item)) onDelete(item);
          }}
          actions={
            <>
              <Filtro valor={status} onChange={onStatusChange} rotulo="Status" opcoes={statusTituloLabels} />
              <Filtro valor={origem} onChange={onOrigemChange} rotulo="Origem" opcoes={origemTituloLabels} />
              <FiltrosAvancadosPopover valor={avancados} onChange={onAvancadosChange} />
              {documento && (
                <Button size="sm" variant="outline" onClick={() => onDocumentoChange(null)}>
                  Parcelas do documento {documento.numeroDocumento} <X />
                </Button>
              )}
            </>
          }
        />
      </FeatureLayout>
    </div>
  );
}
