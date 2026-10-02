"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { BookOpen } from "lucide-react";
import { estoqueApi } from "@/api/estoque";
import { SkuInput } from "@/components/entity-inputs/sku-input";
import { getFullSkuName } from "@/features/catalogo/skus/types";
import { origemMovimentacaoLabels, tipoMovimentacaoLabels } from "@/features/estoque/movimentacoes/types";
import { FeatureLayout } from "@/ui/composites";
import { Button } from "@/ui/primitives";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/ui/primitives";
import { formatToLocal } from "@/utils/date-utils";
import { cn } from "@/lib/utils";

const fmt = (n: number) => Number(n).toLocaleString("pt-BR", { maximumFractionDigits: 4 });
const brl = (n: number) => Number(n).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

// Kardex: histórico do razão de um SKU, com saldo antes/depois de cada movimentação.
export function KardexFeature() {
  const [sku, setSku] = React.useState<{ codigo: string; nome: string } | null>(null);
  const [page, setPage] = React.useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ["movimentacoes", "kardex", sku?.codigo, page],
    queryFn: () => estoqueApi.kardex(sku!.codigo, page, 50),
    enabled: !!sku,
  });
  const totalPages = data?.totalDePaginas ?? 1;

  return (
    <FeatureLayout>
      <h2 className="flex items-center gap-2 text-2xl font-bold tracking-tight">
        <BookOpen className="text-muted-foreground" />
        Kardex
      </h2>
      <div className="max-w-md">
        <SkuInput
          name="kardex-sku"
          label="SKU"
          onSelectSku={(s) => {
            if (!s) return;
            setSku({ codigo: s.sku, nome: getFullSkuName(s) });
            setPage(1);
          }}
        />
      </div>
      {sku && <p className="text-sm font-medium">{sku.codigo} — {sku.nome}</p>}

      <div className="bg-card overflow-x-auto rounded-lg border">
        <Table className="w-full">
          <TableHeader className="bg-muted border-b">
            <TableRow className="hover:bg-transparent">
              <TableHead className="px-4 py-2">Data/Hora</TableHead>
              <TableHead className="px-4 py-2">Origem</TableHead>
              <TableHead className="px-4 py-2">Motivo</TableHead>
              <TableHead className="px-4 py-2 text-right">Anterior</TableHead>
              <TableHead className="px-4 py-2 text-right">Movimento</TableHead>
              <TableHead className="px-4 py-2 text-right">Saldo</TableHead>
              <TableHead className="px-4 py-2 text-right">Custo Unit.</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {!sku || (data?.itens ?? []).length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-muted-foreground px-4 py-8 text-center">
                  {!sku ? "Selecione um SKU para ver o histórico." : isLoading ? "Carregando..." : "Nenhuma movimentação para este SKU."}
                </TableCell>
              </TableRow>
            ) : (
              data!.itens.map((l) => (
                <TableRow key={`${l.movimentacaoId}`}>
                  <TableCell className="px-4 py-2 text-sm">{formatToLocal(l.dataMovimentacao)}</TableCell>
                  <TableCell className="px-4 py-2 text-sm">
                    {origemMovimentacaoLabels[l.origemTipo]}
                    {l.origemId ? <span className="text-muted-foreground"> #{l.origemId}</span> : null}
                  </TableCell>
                  <TableCell className="text-muted-foreground max-w-xs truncate px-4 py-2 text-sm">{l.motivo || "-"}</TableCell>
                  <TableCell className="text-muted-foreground px-4 py-2 text-right text-sm">{fmt(l.quantidadeAnterior)}</TableCell>
                  <TableCell className={cn("px-4 py-2 text-right text-sm font-bold", l.tipoMovimentacao === "ENTRADA" ? "text-emerald-600" : "text-destructive")} title={tipoMovimentacaoLabels[l.tipoMovimentacao]}>
                    {l.tipoMovimentacao === "ENTRADA" ? "+" : "-"}{fmt(l.quantidade)}
                  </TableCell>
                  <TableCell className="px-4 py-2 text-right text-sm font-bold">{fmt(l.quantidadePosterior)}</TableCell>
                  <TableCell className="px-4 py-2 text-right text-sm">{brl(l.custoUnitario)}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {sku && totalPages > 1 && (
        <div className="flex items-center justify-end gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Anterior</Button>
          <span className="text-sm">Página {page} de {totalPages}</span>
          <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>Próxima</Button>
        </div>
      )}
    </FeatureLayout>
  );
}
