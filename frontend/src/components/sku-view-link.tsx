"use client";

import React from "react";
import { skusApi, produtosApi } from "@/api/catalogo";
import { abrirProdutoUpsert } from "@/features/catalogo/produtos/upsert";
import { useUi } from "@/ui/imperative";

/** Código do SKU clicável (só se `enabled`, ex.: item já salvo): abre o produto (editável). */
export function SkuViewLink({ sku, children, enabled = true }: { sku: string; children?: React.ReactNode; enabled?: boolean }) {
  const ui = useUi();
  if (!enabled) return <>{children ?? sku}</>;
  return (
    <button
      type="button"
      title="Abrir produto"
      className="hover:underline"
      onClick={async () => {
        try {
          const s = await skusApi.getBySku(sku);
          if (!s?.produto?.id) return;
          await abrirProdutoUpsert(ui, { editingItem: await produtosApi.getById(s.produto.id) });
        } catch (error) {
          ui.feedback.notifyError(error, { fallbackTitle: "Não foi possível abrir o produto." });
        }
      }}
    >
      {children ?? sku}
    </button>
  );
}
