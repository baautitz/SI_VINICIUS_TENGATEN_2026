"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { Spinner } from "@/ui/primitives";
import { vendasApi } from "@/api/vendas";
import { formatToLocal } from "@/utils/date-utils";

const brl = (v: number) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(v);

export interface DevolucaoDetalheWindowProps {
  devolucaoId: number;
}

/** Visualização somente leitura de uma devolução (imutável). */
export function DevolucaoDetalheWindow({ devolucaoId }: DevolucaoDetalheWindowProps) {
  const { data: d, isLoading } = useQuery({
    queryKey: ["devolucoes", "detail", devolucaoId],
    queryFn: () => vendasApi.getDevolucaoById(devolucaoId),
  });

  if (isLoading)
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );

  if (!d) return <p className="text-muted-foreground text-sm">Devolução não encontrada.</p>;

  return (
    <div className="rounded-md border p-3">
      <div className="flex justify-between text-sm font-semibold">
        <span>
          Devolução #{d.id} em {formatToLocal(d.dataDevolucao)}
        </span>
        <span>{brl(d.valorTotal)}</span>
      </div>
      <p className="text-muted-foreground mt-1 text-xs">Venda de origem: #{d.vendaId}</p>
      <p className="text-muted-foreground mt-1 text-xs">Motivo: {d.motivo}</p>
      <ul className="mt-2 text-xs">
        {d.itens.map((i) => (
          <li key={i.id}>
            {i.sku}: {Number(i.quantidade)} x {brl(i.valorUnitario)} = {brl(i.valorTotal)}
          </li>
        ))}
      </ul>
    </div>
  );
}
