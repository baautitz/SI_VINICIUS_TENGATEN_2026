"use client";

import React from "react";
import { Button, Kbd } from "@/ui/primitives";
import { useWindowCommands } from "@/ui/imperative";

export interface RelatedAction {
  id: string;
  hotkey: "Alt+V" | "Alt+R" | "Alt+T" | "Alt+L";
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
          {a.icon} {a.label} <Kbd>{a.hotkey}</Kbd>
        </Button>
      ))}
    </div>
  );
}
