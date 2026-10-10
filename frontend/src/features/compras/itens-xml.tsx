"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { Check, Link2, TriangleAlert } from "lucide-react";
import { skusApi } from "@/api/catalogo";
import { getFullSkuName, type Sku } from "@/features/catalogo/skus/types";
import {
  Badge,
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/ui/primitives";
import { useUi } from "@/ui/imperative";
import { cn } from "@/lib/utils";
import { SkuSelecaoWindow } from "./sku-selecao";

export interface ItemXml {
  key: number;
  numeroItem?: number;
  sku: string | null;
  skuOriginal?: string | null;
  sugestao?: string | null;
  /** Descrição do fornecedor, como veio na nota. */
  produtoNome: string;
  codigoProduto?: string | null;
  ean?: string | null;
  quantidade: number;
  valorUnitario: number;
  valorDesconto: number;
}

const moeda = (v: number) =>
  v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

/** Nome completo do SKU do sistema (busca por código, em cache). */
function NomeSku({ sku }: { sku: string }) {
  const { data } = useQuery({
    queryKey: ["skus", "por-codigo", sku],
    queryFn: () => skusApi.getBySku(sku),
    staleTime: 5 * 60_000,
  });
  return (
    <span className="min-w-0">
      <span className="block truncate text-sm font-semibold">
        {data ? getFullSkuName(data) : "Carregando..."}
      </span>
      <span className="text-muted-foreground font-mono text-xs">SKU {sku}</span>
    </span>
  );
}

/**
 * Etapa de produtos da importação por XML: o que veio na nota (esquerda) → produto do sistema (direita).
 * Quantidade e valores são do XML e não se editam aqui; só o vínculo é decidido pela pessoa.
 */
export function ItensXml({
  itens,
  onVincular,
}: {
  itens: ItemXml[];
  onVincular: (key: number, sku: string, permiteDecimais?: boolean) => void;
}) {
  const ui = useUi();
  const [soPendentes, setSoPendentes] = React.useState(false);
  const pendentes = itens.filter((i) => !i.sku).length;
  const visiveis = soPendentes ? itens.filter((i) => !i.sku) : itens;

  const buscar = async (item: ItemXml) => {
    const r = await ui.windows.open<Sku, { initialSearchTerm?: string }>({
      component: SkuSelecaoWindow,
      props: { initialSearchTerm: item.produtoNome },
      title: "Selecionar Produto (SKU)",
      size: "full",
    });
    if (r.status !== "confirmed") return;
    if (!r.value.ativo) {
      ui.feedback.notify({ type: "error", title: "Este SKU está inativo." });
      return;
    }
    onVincular(item.key, r.value.sku, !!r.value.produto?.unidadeMedida?.permiteDecimais);
  };

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center gap-3 text-sm">
        <Badge variant={pendentes ? "destructive" : "secondary"}>
          {itens.length - pendentes} de {itens.length} vinculados
        </Badge>
        {pendentes > 0 && (
          <>
            <span className="text-muted-foreground text-xs">
              Vincule cada item da nota a um produto do sistema para avançar.
            </span>
            <Button
              type="button"
              size="sm"
              variant="outline"
              className="ml-auto"
              onClick={() => setSoPendentes((v) => !v)}
            >
              {soPendentes ? "Mostrar todos" : `Só pendentes (${pendentes})`}
            </Button>
          </>
        )}
      </div>

      <div className="rounded-lg border bg-card overflow-clip">
        <Table pageScroll aria-label="Itens da nota">
          <TableHeader>
            <TableRow>
              <TableHead className="w-10">#</TableHead>
              <TableHead>Na nota (fornecedor)</TableHead>
              <TableHead>Produto no sistema</TableHead>
              <TableHead className="w-40 text-right">Qtd × valor</TableHead>
              <TableHead className="w-24 text-right">Ações</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {visiveis.length === 0 && (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-6 text-center">
                  Nenhum item.
                </TableCell>
              </TableRow>
            )}
            {visiveis.map((item, i) => {
              const total = item.quantidade * item.valorUnitario - item.valorDesconto;
              return (
                <TableRow
                  key={item.key}
                  data-sem-sku={!item.sku ? "" : undefined}
                  className={cn(!item.sku && "bg-destructive/10")}
                >
                  <TableCell className="text-muted-foreground">
                    {item.numeroItem ?? i + 1}
                  </TableCell>
                  <TableCell>
                    <span className="flex min-w-0 flex-col">
                      <span className="truncate text-sm">{item.produtoNome}</span>
                      <span className="text-muted-foreground truncate text-xs">
                        {[item.codigoProduto && `Cód. ${item.codigoProduto}`, item.ean && `EAN ${item.ean}`]
                          .filter(Boolean)
                          .join(" · ")}
                      </span>
                    </span>
                  </TableCell>
                  <TableCell>
                    <div className="flex min-w-0 items-center gap-2">
                      {item.sku ? (
                        <>
                          <Check className="size-4 shrink-0 text-emerald-600" />
                          <NomeSku sku={item.sku} />
                        </>
                      ) : item.sugestao ? (
                        <>
                          <TriangleAlert className="size-4 shrink-0 text-amber-600" />
                          <span className="flex min-w-0 flex-col">
                            <span className="text-muted-foreground text-xs">Sugestão</span>
                            <NomeSku sku={item.sugestao} />
                          </span>
                        </>
                      ) : (
                        <span className="text-destructive text-sm font-semibold">
                          Sem vínculo
                        </span>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    <span className="flex flex-col text-xs">
                      <span>
                        {item.quantidade.toLocaleString("pt-BR")} × {moeda(item.valorUnitario)}
                      </span>
                      <span className="font-semibold">{moeda(total)}</span>
                    </span>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center justify-end gap-1">
                      {!item.sku && item.sugestao && (
                        <Button
                          type="button"
                          size="icon"
                          variant="outline"
                          className="size-8"
                          title="Usar sugestão"
                          aria-label="Usar sugestão"
                          onClick={() => onVincular(item.key, item.sugestao!)}
                        >
                          <Check className="size-3.5" />
                        </Button>
                      )}
                      <Button
                        type="button"
                        size="icon"
                        variant="outline"
                        className={cn("size-8", !item.sku && "border-destructive text-destructive")}
                        title={item.sku ? "Trocar produto vinculado" : "Vincular produto"}
                        aria-label={item.sku ? "Trocar produto vinculado" : "Vincular produto"}
                        onClick={() => void buscar(item)}
                      >
                        <Link2 className="size-3.5" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
