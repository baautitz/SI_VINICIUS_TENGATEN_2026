"use client";

import React from "react";
import { Button, HotkeyKbd } from "@/ui/primitives";
import { useWindowCommands } from "@/ui/imperative";
import type { Combinacao } from "@/lib/atalhos";

export interface RelatedAction {
  id: string;
  hotkey: Combinacao;
  label: string;
  icon: React.ReactNode;
  run: () => unknown;
}

/** Atalhos para registros relacionados no rodapé (use dentro de WindowActions, só em leitura). */
export function RelatedActions({ actions }: { actions: RelatedAction[] }) {
  useWindowCommands(
    actions.map((a) => ({
      id: a.id,
      hotkey: a.hotkey,
      label: a.label,
      run: (event: KeyboardEvent) => {
        event.preventDefault();
        void a.run();
      },
    })),
  );
  if (!actions.length) return null;
  return (
    <div className="mr-auto flex gap-2">
      {actions.map((a) => (
        <Button key={a.id} type="button" variant="outline" onClick={() => void a.run()}>
          {a.icon} {a.label} <HotkeyKbd hotkey={a.hotkey} />
        </Button>
      ))}
    </div>
  );
}
