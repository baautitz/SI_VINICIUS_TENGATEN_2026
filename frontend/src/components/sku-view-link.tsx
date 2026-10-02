"use client";

import React from "react";
import { skusApi, produtosApi } from "@/api/catalogo";
import { ProdutosUpsert } from "@/features/catalogo/produtos/upsert";
import { useRelated } from "@/hooks/use-related";

/** Código do SKU clicável (só se `enabled`, ex.: item já salvo): abre o produto (editável). */
export function SkuViewLink({ sku, children, enabled = true }: { sku: string; children?: React.ReactNode; enabled?: boolean }) {
  const { openView } = useRelated();
  if (!enabled) return <>{children ?? sku}</>;
  return (
    <button
      type="button"
      title="Abrir produto"
      className="hover:underline"
      onClick={() =>
        openView(
          async () => {
            const s = await skusApi.getBySku(sku);
            return s?.produto?.id ? produtosApi.getById(s.produto.id) : null;
          },
          ProdutosUpsert,
          "Editar Produto",
          false,
        )
      }
    >
      {children ?? sku}
    </button>
  );
}
