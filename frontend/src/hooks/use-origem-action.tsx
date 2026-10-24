"use client";

import React from "react";
import { ListChecks, ShoppingBag, ShoppingCart, Undo2 } from "lucide-react";
import { useUi } from "@/ui/imperative";
import { combinacaoDe } from "@/lib/atalhos";
import { useRelated } from "@/hooks/use-related";
import type { RelatedAction } from "@/components/related-actions";
import { vendasApi } from "@/api/vendas";
import { CompraWindow, type CompraWindowProps } from "@/features/compras/upsert";
import { VendasUpsertForm } from "@/features/vendas/upsert";
import { BalancoWindow, type BalancoWindowProps } from "@/features/estoque/balancos/window";

/** Tipos de documento que originam títulos financeiros e movimentações de estoque. */
export type OrigemDocumento = "COMPRA" | "VENDA" | "DEVOLUCAO_VENDA" | "BALANCO";

const rotulos: Record<OrigemDocumento, string> = {
  COMPRA: "Compra",
  VENDA: "Venda",
  DEVOLUCAO_VENDA: "Devolução de venda",
  BALANCO: "Balanço",
};

/**
 * Ação (botão dinâmico, atalho `origem` do registro) que abre em leitura o documento que gerou o registro: o rótulo e o destino
 * seguem o tipo de origem. Lista vazia para origem manual, desconhecida ou sem id.
 */
export function useOrigemAction(origemTipo: string | null | undefined, origemId: number | null | undefined): RelatedAction[] {
  const ui = useUi();
  const related = useRelated();

  if (!origemId || !(origemTipo && origemTipo in rotulos)) return [];
  const tipo = origemTipo as OrigemDocumento;

  const abrir = () => {
    switch (tipo) {
      case "COMPRA":
        return ui.windows.open<true, CompraWindowProps>({
          component: CompraWindow,
          props: { compraId: origemId },
          title: `Compra #${origemId}`,
          size: "full",
        });
      case "VENDA":
        return related.openView(() => vendasApi.getById(origemId), VendasUpsertForm, `Detalhes da Venda #${origemId}`);
      case "DEVOLUCAO_VENDA":
        // O documento de origem é a devolução; ela é aberta pela venda a que pertence.
        return related.openView(
          async () => vendasApi.getById((await vendasApi.getDevolucaoById(origemId)).vendaId),
          VendasUpsertForm,
          "Detalhes da Venda",
        );
      case "BALANCO":
        return ui.windows.open<"saved" | "closed", BalancoWindowProps>({
          component: BalancoWindow,
          props: { balancoId: origemId },
          title: `Balanço #${origemId}`,
          size: "full",
        });
    }
  };

  const icone: Record<OrigemDocumento, React.ReactNode> = {
    COMPRA: <ShoppingBag className="size-4" />,
    VENDA: <ShoppingCart className="size-4" />,
    DEVOLUCAO_VENDA: <Undo2 className="size-4" />,
    BALANCO: <ListChecks className="size-4" />,
  };

  return [
    {
      id: `origem.${tipo.toLowerCase()}`,
      hotkey: combinacaoDe("origem"),
      label: `${rotulos[tipo]} #${origemId}`,
      icon: icone[tipo],
      run: abrir,
    },
  ];
}
