"use client";

import React from "react";
import { Search } from "lucide-react";
import { skusApi } from "@/api/catalogo";
import { SkusFeature } from "@/features/catalogo/skus";
import type { Sku } from "@/features/catalogo/skus/types";
import { Button, Input } from "@/ui/primitives";
import { useWindow, useUi } from "@/ui/imperative";

function SkuSelecaoWindow({
  initialSearchTerm = "",
}: {
  initialSearchTerm?: string;
}) {
  const activeWindow = useWindow<Sku>();
  const ui = useUi();
  return (
    <div className="flex h-full min-h-0 flex-col p-4">
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

/** Campo de SKU de uma linha: Enter com código busca; Enter vazio (ou a lupa) abre a seleção. */
export function SkuPicker({
  value,
  onPick,
  disabled,
  inputProps,
}: {
  value: string | null;
  onPick: (sku: Sku) => void;
  disabled?: boolean;
  inputProps?: React.ComponentProps<typeof Input>;
}) {
  const ui = useUi();
  const busy = React.useRef(false);

  const abrir = async (termo: string, input?: HTMLInputElement | null) => {
    if (busy.current) return;
    busy.current = true;
    try {
      let sku: Sku | null = null;
      if (termo) {
        try {
          sku = await skusApi.getBySku(termo);
        } catch {
          sku = null; // sem correspondência exata: cai na seleção
        }
      }
      if (sku && !sku.ativo) {
        ui.feedback.notify({
          type: "error",
          title: `O SKU "${termo}" está inativo.`,
        });
        return;
      }
      if (!sku) {
        const r = await ui.windows.open<Sku, { initialSearchTerm?: string }>({
          component: SkuSelecaoWindow,
          props: { initialSearchTerm: termo },
          title: "Selecionar Produto (SKU)",
          size: "full",
          chrome: "plain",
        });
        if (r.status === "confirmed") sku = r.value;
      }
      if (sku) onPick(sku);
    } finally {
      busy.current = false;
      requestAnimationFrame(() => input?.focus());
    }
  };

  return (
    <div className="relative min-w-40">
      <Input
        {...inputProps}
        key={value ?? ""}
        defaultValue={value ?? ""}
        disabled={disabled}
        placeholder="SKU (Enter busca)"
        className="h-7 pr-8 font-mono text-xs"
        onKeyDown={(e) => {
          if (e.key !== "Enter" || e.altKey || e.repeat) return;
          e.preventDefault();
          const termo = e.currentTarget.value.trim();
          if (termo && termo === value) return;
          void abrir(termo, e.currentTarget);
        }}
      />
      <Button
        type="button"
        size="icon-xs"
        variant="ghost"
        tabIndex={-1}
        disabled={disabled}
        title="Selecionar SKU"
        aria-label="Selecionar SKU"
        className="absolute top-0.5 right-0.5 size-6"
        onClick={() => void abrir("")}
      >
        <Search className="size-3.5" />
      </Button>
    </div>
  );
}
