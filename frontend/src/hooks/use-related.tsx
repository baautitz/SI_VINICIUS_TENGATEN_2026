"use client";

import React from "react";
import { useUi, useWindow } from "@/ui/imperative";
import type { PaginatedResult } from "@/api/types";

export interface RelacionadoItem {
  id: number;
  descricao: string;
  detalhe?: string | null;
}

/** Converte uma página de um módulo em itens de escolha para openList. */
export const mapItens =
  <T,>(fn: (x: T) => RelacionadoItem) =>
  (r: PaginatedResult<T>) =>
    r.itens.map(fn);

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

/**
 * Abre um registro relacionado; ids sob demanda, lista de escolha quando há mais de um.
 * `readOnly` é true por padrão; passe false quando a entidade de destino permite edição.
 */
export function useRelated() {
  const ui = useUi();

  const openView = async (
    fetchItem: () => Promise<unknown>,
    component: React.ComponentType<any>,
    title: string,
    readOnly = true,
  ) => {
    try {
      const item = await fetchItem();
      if (!item) return;
      await ui.windows.open({
        component,
        props: { editingItem: item, readOnly },
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
    readOnly = true,
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
      await openView(() => fetchById(id), component, title, readOnly);
    } catch (error) {
      ui.feedback.notifyError(error, { fallbackTitle: `Não foi possível abrir: ${title}.` });
    }
  };

  return { openView, openList };
}
