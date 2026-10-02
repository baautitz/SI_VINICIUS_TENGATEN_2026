"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { NfesList, TODOS } from "./list";
import { NfeDetalheWindow } from "./detalhe";
import type { NfeResumo, StatusNfe, TipoOperacaoNfe } from "./types";
import { useFeatureList } from "@/hooks/use-feature-list";
import { nfesApi } from "@/api/nfes";
import { useUi } from "@/ui/imperative";

export * from "./types";

/** Abre o detalhe da NF-e; a lista recarrega se algo mudou (cancelamento). */
export async function abrirNfe(ui: ReturnType<typeof useUi>, id: number, numero?: number) {
  return ui.windows.open<true, { nfeId: number }>({
    component: NfeDetalheWindow,
    props: { nfeId: id },
    title: numero ? `NF-e ${numero}` : `NF-e #${id}`,
    size: "full",
  });
}

export function NfesFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<NfeResumo>();
  const [tipo, setTipo] = React.useState<string>(TODOS);
  const [status, setStatus] = React.useState<string>(TODOS);
  const { data, isLoading, isError } = useQuery({
    queryKey: ["nfes", "lista", list.deferredSearch, tipo, status, list.page],
    queryFn: async () => {
      const res = await nfesApi.list({
        search: list.deferredSearch.trim() || undefined,
        tipo: tipo === TODOS ? undefined : (tipo as TipoOperacaoNfe),
        status: status === TODOS ? undefined : (status as StatusNfe),
        page: list.page,
        pageSize: 50,
      });
      return {
        itens: res?.itens ?? [],
        totalPages: res?.totalDePaginas ?? 1,
        totalItems: res?.totalDeItens ?? 0,
      };
    },
  });

  const filtro = (set: (v: string) => void) => (v: string) => {
    set(v);
    list.setPage(1);
  };

  return (
    <NfesList
      items={data?.itens ?? []}
      loading={isLoading}
      error={isError}
      searchTerm={list.searchTerm}
      page={list.page}
      totalPages={data?.totalPages ?? 1}
      totalItems={data?.totalItems ?? 0}
      tipo={tipo}
      status={status}
      onTipoChange={filtro(setTipo)}
      onStatusChange={filtro(setStatus)}
      onSearchChange={list.handleSearchChange}
      onPageChange={list.setPage}
      onView={async (n) => {
        await abrirNfe(ui, n.id, n.numero);
        await queryClient.invalidateQueries({ queryKey: ["nfes"] });
      }}
    />
  );
}
