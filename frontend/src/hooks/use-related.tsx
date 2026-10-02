"use client";

import React from "react";
import { useUi, useWindow } from "@/ui/imperative";
import type { RelacionadoItem } from "@/api/relacionados";

function RelacionadosWindow({ itens }: { itens: RelacionadoItem[] }) {
  const win = useWindow<number>();
  return (
    <ul className="flex flex-col gap-1">
      {itens.map((i, idx) => (
        <li key={i.id}>
          <button
            type="button"
            autoFocus={idx === 0}
            className="hover:bg-accent focus:bg-accent flex w-full items-center justify-between rounded px-3 py-2 text-left text-sm"
            onClick={() => win.resolve(i.id)}
          >
            <span>{i.descricao}</span>
            {i.detalhe && <span className="text-muted-foreground">{i.detalhe}</span>}
          </button>
        </li>
      ))}
    </ul>
  );
}

/** Abre um registro relacionado em leitura; ids sob demanda, lista de escolha quando há mais de um. */
export function useRelated() {
  const ui = useUi();

  const openView = async (
    fetchItem: () => Promise<unknown>,
    component: React.ComponentType<any>,
    title: string,
  ) => {
    try {
      const item = await fetchItem();
      if (!item) return;
      await ui.windows.open({
        component,
        props: { editingItem: item, readOnly: true },
        title,
        size: "full",
      });
    } catch (error) {
      ui.feedback.notifyError(error, { fallbackTitle: `Não foi possível abrir: ${title}.` });
    }
  };

  const openList = async (
    fetchItems: () => Promise<RelacionadoItem[] | undefined>,
    fetchById: (id: number) => Promise<unknown>,
    component: React.ComponentType<any>,
    title: string,
    vazio: string,
  ) => {
    try {
      const itens = (await fetchItems()) ?? [];
      if (!itens.length) return ui.feedback.notify({ type: "info", title: vazio });
      let id = itens[0].id;
      if (itens.length > 1) {
        const r = await ui.windows.open<number, { itens: RelacionadoItem[] }>({
          component: RelacionadosWindow,
          props: { itens },
          title,
          size: "medium",
        });
        if (r.status !== "confirmed") return;
        id = r.value;
      }
      await openView(() => fetchById(id), component, title);
    } catch (error) {
      ui.feedback.notifyError(error, { fallbackTitle: `Não foi possível abrir: ${title}.` });
    }
  };

  return { openView, openList };
}
