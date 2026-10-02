"use client";

import React from "react";
import { ColumnDef } from "@tanstack/react-table";
import { Eye, FileText } from "lucide-react";
import { Button } from "@/ui/primitives";
import { DataTable, FeatureLayout } from "@/ui/composites";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/ui/primitives";
import { fireAndForget } from "@/lib/utils";
import { formatToLocal } from "@/utils/date-utils";
import { NfeStatusBadge } from "./status-badge";
import {
  brl, statusLabels, tipoLabels, type NfeResumo, type StatusNfe, type TipoOperacaoNfe,
} from "./types";

export const TODOS = "TODOS";

interface Props {
  items: NfeResumo[];
  loading: boolean;
  error: boolean;
  searchTerm: string;
  page: number;
  totalPages: number;
  totalItems: number;
  tipo: string;
  status: string;
  onTipoChange: (v: TipoOperacaoNfe | typeof TODOS) => void;
  onStatusChange: (v: StatusNfe | typeof TODOS) => void;
  onSearchChange: (v: string) => void;
  onPageChange: (p: number) => void;
  onView: (item: NfeResumo) => void;
}

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

export function NfesList({
  items, loading, error, searchTerm, page, totalPages, totalItems, tipo, status,
  onTipoChange, onStatusChange, onSearchChange, onPageChange, onView,
}: Props) {
  const columns: ColumnDef<NfeResumo>[] = [
    { id: "numero", header: "Número", size: 100, cell: ({ row }) => <span className="font-semibold">{row.original.numero}/{row.original.serie}</span> },
    { id: "tipo", header: "Tipo", cell: ({ row }) => tipoLabels[row.original.tipoOperacao] },
    { id: "emissao", header: "Emissão", cell: ({ row }) => formatToLocal(row.original.dataEmissao) },
    {
      id: "chave", header: "Chave de acesso",
      cell: ({ row }) => <span className="font-mono text-xs text-muted-foreground">{row.original.chaveAcesso ?? "—"}</span>,
    },
    { id: "total", header: () => <div className="text-right">Total</div>, cell: ({ row }) => <div className="text-right font-semibold">{brl(row.original.valorTotal)}</div> },
    { id: "status", header: "Status", cell: ({ row }) => <NfeStatusBadge status={row.original.statusNfe} /> },
    {
      id: "actions", header: () => <div className="px-4 text-right">Ações</div>,
      cell: ({ row }) => (
        <div className="flex justify-end gap-2 px-4">
          <Button size="icon-sm" variant="outline" title="Visualizar" onClick={() => fireAndForget(() => onView(row.original))}>
            <Eye className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <FeatureLayout>
        <div className="flex items-center gap-2">
          <div className="bg-primary/10 text-primary rounded-lg p-2 [&_svg]:size-5"><FileText /></div>
          <h1 className="text-foreground text-xl font-bold tracking-tight">Notas Fiscais</h1>
        </div>
        {error && (
          <p role="alert" className="text-sm text-destructive">
            Não foi possível carregar as notas fiscais. Verifique a conexão com o servidor e tente novamente.
          </p>
        )}
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
          searchPlaceholder="Pesquisar por número, chave ou nome..."
          getRowId={(row) => row.id.toString()}
          onEditRow={onView}
          actions={
            <>
              <Filtro valor={tipo} onChange={(v) => onTipoChange(v as TipoOperacaoNfe)} rotulo="Tipo" opcoes={tipoLabels} />
              <Filtro valor={status} onChange={(v) => onStatusChange(v as StatusNfe)} rotulo="Status" opcoes={statusLabels} />
            </>
          }
        />
      </FeatureLayout>
    </div>
  );
}
