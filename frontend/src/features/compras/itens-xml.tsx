"use client";

import React from "react";
import { useQuery } from "@tanstack/react-query";
import { Check, TriangleAlert } from "lucide-react";
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
import { Checkbox } from "@/ui/primitives";
import { EntityInput } from "@/ui/composites";
import { SkusFeature } from "@/features/catalogo/skus";
import { useUi } from "@/ui/imperative";
import { cn } from "@/lib/utils";

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

/** Campo de SKU digitável (código, EAN ou nome; Enter abre a busca). Selecionar vincula o item. */
function VinculoSku({
  item,
  onSelecionar,
}: {
  item: ItemXml;
  onSelecionar: (sku: Sku) => void;
}) {
  const ui = useUi();
  const { data } = useQuery({
    queryKey: ["skus", "por-codigo", item.sku],
    queryFn: () => skusApi.getBySku(item.sku!),
    enabled: !!item.sku,
    staleTime: 5 * 60_000,
  });
  return (
    <EntityInput<Sku, Sku, string>
      key={item.sku ? (data?.sku ?? "carregando") : "vazio"}
      name={`sku-item-${item.key}`}
      label={`Produto do item ${item.numeroItem ?? item.key}`}
      hideLabel
      placeholder="Digite o SKU, EAN ou nome..."
      initialItem={data ?? null}
     
      onSelectId={() => {}}
      onSelectItem={(sku) => {
        if (!sku || sku.sku === item.sku) return;
        if (!sku.ativo) {
          ui.feedback.notify({ type: "error", title: "Este SKU está inativo." });
          return;
        }
        onSelecionar(sku);
      }}
      modalTitle="Selecionar Produto (SKU)"
      getDisplayLabel={(s) => getFullSkuName(s)}
      getSearchTerm={(s) => s.sku}
      getId={(s) => s.sku}
      fetchById={async (id) => {
        try {
          return await skusApi.getBySku(id);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          const r = await skusApi.list(term.trim() || undefined, 1, 10);
          return r ? { itens: r.itens } : null;
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <SkusFeature {...props} />}
    />
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
            <label className="ml-auto flex cursor-pointer items-center gap-2 text-sm">
              <Checkbox
                checked={soPendentes}
                onCheckedChange={(v) => setSoPendentes(v === true)}
              />
              Só pendentes ({pendentes})
            </label>
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
            </TableRow>
          </TableHeader>
          <TableBody>
            {visiveis.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-6 text-center">
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
                      {!item.sku && item.sugestao && (
                        <span className="flex shrink-0 items-center gap-1">
                          <TriangleAlert className="size-4 text-warning" />
                          <span className="text-muted-foreground text-xs">Sugestão:</span>
                          <span className="font-mono text-xs">{item.sugestao}</span>
                          <Button
                            type="button"
                            size="icon-sm"
                            variant="outline"
                            title="Usar sugestão"
                            aria-label="Usar sugestão"
                            onClick={() => onVincular(item.key, item.sugestao!)}
                          >
                            <Check />
                          </Button>
                        </span>
                      )}
                      <div className="min-w-0 flex-1">
                        <VinculoSku
                          item={item}
                          onSelecionar={(sku) =>
                            onVincular(item.key, sku.sku, !!sku.produto?.unidadeMedida?.permiteDecimais)
                          }
                        />
                      </div>
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
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
