"use client";

import React from "react";
import { SkusFeature } from "@/features/catalogo/skus";
import type { Sku } from "@/features/catalogo/skus/types";
import { useWindow, useUi } from "@/ui/imperative";

export function SkuSelecaoWindow({
  initialSearchTerm = "",
}: {
  initialSearchTerm?: string;
}) {
  const activeWindow = useWindow<Sku>();
  const ui = useUi();
  return (
    <div className="flex h-full min-h-0 flex-col">
      <SkusFeature
        selectionMode
        initialSearchTerm={initialSearchTerm}
        onSelect={(sku) => {
          if (!sku.ativo) {
            ui.feedback.notify({
              type: "error",
              title: "Este SKU está inativo.",
            });
            return;
          }
          activeWindow.resolve(sku);
        }}
      />
    </div>
  );
}
