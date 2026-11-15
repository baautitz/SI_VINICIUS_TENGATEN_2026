"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { Boxes, ShoppingBag, Undo2 } from "lucide-react";
import { WindowActions, useUi, useWindowCommands } from "@/ui/imperative";
import { RelatedActions } from "@/components/related-actions";
import { useRelated, mapItens } from "@/hooks/use-related";
import { estoqueApi } from "@/api/estoque";
import { MovimentacoesUpsert } from "@/features/estoque/movimentacoes/upsert";
import { CompraWindow, type CompraWindowProps } from "@/features/compras/upsert";
import {
  Alert,
  AlertDescription,
  Badge,
  Button,
  HotkeyKbd,
  Spinner,
} from "@/ui/primitives";
import { comprasApi } from "@/api/compras";
import { combinacaoDe } from "@/lib/atalhos";
import { useEstornarDevolucaoCompra } from "./estornar";
import { formatToLocal } from "@/utils/date-utils";

const brl = (v: number) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(
    v,
  );

export interface DevolucaoCompraDetalheWindowProps {
  devolucaoId: number;
}

/** Visualização somente leitura de uma devolução (imutável). */
export function DevolucaoCompraDetalheWindow({
  devolucaoId,
}: DevolucaoCompraDetalheWindowProps) {
  const { data: d, isLoading } = useQuery({
    queryKey: ["devolucoes-compra", "detail", devolucaoId],
    queryFn: () => comprasApi.getDevolucaoById(devolucaoId),
  });

  const ui = useUi();
  const { openList } = useRelated();
  const estornarDevolucao = useEstornarDevolucaoCompra();
  const podeEstornar = !!d && d.status !== "ESTORNADA";

  useWindowCommands([
    {
      id: "devolucoes-compra.estornar",
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
      <p className="text-muted-foreground text-sm">Devolução de compra não encontrada.</p>
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
        <p className="text-muted-foreground mt-1 text-sm">
          Compra de origem: #{d.compraId}
        </p>
        <p className="text-muted-foreground mt-1 text-sm">Motivo: {d.motivo}</p>
        <ul className="mt-2 text-sm">
          {d.itens.map((i) => (
            <li key={i.id}>
              {i.sku}: {Number(i.quantidade)} x {brl(i.valorUnitario)} ={" "}
              {brl(i.valorTotal)}
            </li>
          ))}
        </ul>
      </div>
      <WindowActions>
        <RelatedActions
          actions={[
            {
              id: "devolucao-compra.compra",
              hotkey: combinacaoDe("origem"),
              label: `Compra #${d.compraId}`,
              icon: <ShoppingBag className="size-4" />,
              run: () =>
                ui.windows.open<true, CompraWindowProps>({
                  component: CompraWindow,
                  props: { compraId: d.compraId },
                  title: `Compra #${d.compraId}`,
                  size: "full",
                }),
            },
            {
              id: "devolucao-compra.movimentacoes",
              hotkey: combinacaoDe("verMovimentacao"),
              label: "Movimentações de estoque",
              icon: <Boxes className="size-4" />,
              run: () =>
                openList(
                  () => estoqueApi.listByOrigem("DEVOLUCAO_COMPRA", d.id).then(mapItens((x) => ({ id: x.id, descricao: `Movimentação #${x.id}`, detalhe: x.tipoMovimentacao }))),
                  estoqueApi.getById,
                  MovimentacoesUpsert,
                  "Visualizar Movimentação de Estoque",
                  "Devolução sem movimentação de estoque.",
                ),
            },
          ]}
        />
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
