"use client";

import React from "react";
import { VendaInput } from "@/components/entity-inputs/venda-input";
import { WindowActions, WindowCancelButton, useWindow } from "@/ui/imperative";
import { vendasApi } from "@/api/vendas";
import type { Venda } from "@/features/vendas/types";

/** Escolhe a venda de origem de uma nova devolução; resolve com a venda completa. */
export function NovaDevolucaoWindow() {
  const activeWindow = useWindow<Venda>();
  return (
    <div className="flex flex-col gap-4">
      <VendaInput
        name="vendaId"
        label="Venda de origem"
        onSelectId={() => {}}
        onSelectItem={(item) => {
          if (!item) return;
          // A lista traz venda parcial; o fluxo busca o detalhe, mas garante o status atual.
          void vendasApi.getById(item.id).then((v) => activeWindow.resolve(v ?? item));
        }}
      />
      <WindowActions>
        <WindowCancelButton />
      </WindowActions>
    </div>
  );
}
