"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { Undo2 } from "lucide-react";
import { WindowActions, useWindowCommands } from "@/ui/imperative";
import {
  Alert,
  AlertDescription,
  Badge,
  Button,
  HotkeyKbd,
  Spinner,
} from "@/ui/primitives";
import { vendasApi } from "@/api/vendas";
import { combinacaoDe } from "@/lib/atalhos";
import { useEstornarDevolucao } from "./estornar";
import { formatToLocal } from "@/utils/date-utils";

const brl = (v: number) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(
    v,
  );

export interface DevolucaoDetalheWindowProps {
  devolucaoId: number;
}

/** Visualização somente leitura de uma devolução (imutável). */
export function DevolucaoDetalheWindow({
  devolucaoId,
}: DevolucaoDetalheWindowProps) {
  const { data: d, isLoading } = useQuery({
    queryKey: ["devolucoes", "detail", devolucaoId],
    queryFn: () => vendasApi.getDevolucaoById(devolucaoId),
  });

  const estornarDevolucao = useEstornarDevolucao();
  const podeEstornar = !!d && d.status !== "ESTORNADA";

  useWindowCommands([
    {
      id: "devolucoes.estornar",
      hotkey: combinacaoDe("estornar"),
      label: "Estornar devolução",
      enabled: podeEstornar,
      run: (e: KeyboardEvent) => {
        e.preventDefault();
        void estornarDevolucao(devolucaoId);
      },
    },
  ]);

  if (isLoading)
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
    );

  if (!d)
    return (
      <p className="text-muted-foreground text-sm">Devolução não encontrada.</p>
    );

  return (
    <div className="flex flex-col gap-4">
      {d.status === "ESTORNADA" && (
        <Alert variant="destructive" role="status">
          <AlertDescription>
            <Badge variant="destructive" className="mr-2">
              Estornada
            </Badge>
            {d.estornadoEm ? `em ${formatToLocal(d.estornadoEm)}` : ""}:{" "}
            {d.motivoEstorno}
          </AlertDescription>
        </Alert>
      )}
      <div className="rounded-md border p-3">
        <div className="flex justify-between text-sm font-semibold">
          <span>
            Devolução #{d.id} em {formatToLocal(d.dataDevolucao)}
          </span>
          <span>{brl(d.valorTotal)}</span>
        </div>
        <p className="text-muted-foreground mt-1 text-xs">
          Venda de origem: #{d.vendaId}
        </p>
        <p className="text-muted-foreground mt-1 text-xs">Motivo: {d.motivo}</p>
        <ul className="mt-2 text-xs">
          {d.itens.map((i) => (
            <li key={i.id}>
              {i.sku}: {Number(i.quantidade)} x {brl(i.valorUnitario)} ={" "}
              {brl(i.valorTotal)}
            </li>
          ))}
        </ul>
      </div>
      <WindowActions>
        {podeEstornar && (
          <Button
            type="button"
            variant="destructive"
            onClick={() => void estornarDevolucao(devolucaoId)}
          >
            <Undo2 /> Estornar devolução{" "}
            <HotkeyKbd hotkey={combinacaoDe("estornar")} />
          </Button>
        )}
      </WindowActions>
    </div>
  );
}
