"use client";
import React from "react";
import { useUi } from "@/ui/imperative";
import { EntityInput } from "@/ui/composites";
import { CompraWindow, type CompraWindowProps } from "@/features/compras/upsert";
import type { Compra } from "@/features/compras/types";
import { comprasApi } from "@/api/compras";

interface CompraInputProps {
  name: string;
  label?: string;
  initialItem?: Compra | null;
  onSelectId?: (id: number | null) => void;
}

/**
 * Referência a uma compra. Desabilitado (uso atual: origem de um documento) abre a compra em leitura com
 * Enter ou clique; a busca de seleção ainda não existe porque a listagem de compras não tem modo seleção.
 */
export function CompraInput({ name, label = "Compra", initialItem, onSelectId }: CompraInputProps) {
  const ui = useUi();
  return (
    <EntityInput<Compra, Compra>
      name={name}
      label={label}
      disabled
      initialItem={initialItem}
      onSelectId={onSelectId ?? (() => {})}
      onView={(item) =>
        ui.windows.open<true, CompraWindowProps>({
          component: CompraWindow,
          props: { compraId: item.id },
          title: `Compra #${item.id}`,
          size: "full",
        })
      }
      modalTitle="Selecionar Compra"
      getDisplayLabel={(item) => `#${item.id} - ${item.fornecedor?.nomeRazaosocial ?? ""}`}
      getSearchTerm={(item) => String(item.id)}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await comprasApi.getById(id);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          return await comprasApi.list(term.trim() || undefined, 1, 10);
        } catch {
          return null;
        }
      }}
      renderFeature={() => null}
    />
  );
}
