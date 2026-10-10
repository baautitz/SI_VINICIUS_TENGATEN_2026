"use client";
import React from "react";
import { useQuery } from "@tanstack/react-query";
import { useUi } from "@/ui/imperative";
import { EntityInput } from "@/ui/composites";
import { skusApi, produtosApi } from "@/api/catalogo";
import { ProdutosUpsert } from "@/features/catalogo/produtos/upsert";
import type { Sku } from "@/features/catalogo/skus/types";

/**
 * SKU de uma linha de tabela: campo de entidade desabilitado que abre o cadastro (produto do SKU) com
 * Enter ou clique. O código fica no campo; o nome do produto vem na coluna ao lado.
 */
export function SkuViewInput({ sku, name }: { sku: string; name: string }) {
  const ui = useUi();
  const { data: item } = useQuery({
    queryKey: ["skus", "por-codigo", sku],
    queryFn: () => skusApi.getBySku(sku),
    staleTime: 5 * 60_000,
  });
  const inicial: Sku | null = item ?? null;

  return (
    <div className="w-36 min-w-36">
      <EntityInput<Sku, Sku, string>
        key={inicial?.sku ?? "carregando"}
        name={name}
        label={`SKU ${sku}`}
        hideLabel
        disabled
        initialItem={inicial}
        onSelectId={() => {}}
        onView={async (s) => {
          if (!s.produto?.id) return;
          try {
            const produto = await produtosApi.getById(s.produto.id);
            await ui.windows.open({
              component: ProdutosUpsert,
              props: { editingItem: produto, readOnly: false },
              title: "Editar Produto",
              size: "full",
            });
          } catch (error) {
            ui.feedback.notifyError(error, {
              fallbackTitle: "Não foi possível abrir o produto.",
            });
          }
        }}
        modalTitle="Selecionar SKU"
        getDisplayLabel={(s) => s.sku}
        getSearchTerm={(s) => s.sku}
        getId={(s) => s.sku}
        fetchById={async (id) => {
          try {
            return await skusApi.getBySku(id);
          } catch {
            return null;
          }
        }}
        fetchList={async () => null}
        renderFeature={() => null}
      />
    </div>
  );
}
