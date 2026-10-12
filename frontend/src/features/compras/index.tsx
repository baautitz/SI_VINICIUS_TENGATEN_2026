"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ComprasList } from "./list";
import {
  CompraWindow,
  type CompraWindowProps,
  type ImportacaoXml,
} from "./upsert";
import { ImportarXmlWindow } from "./importar";
import { EstornarCompraWindow, type EstornarCompraWindowProps } from "./estornar";
import type { Compra } from "./types";
import { useFeatureList } from "@/hooks/use-feature-list";
import { comprasApi } from "@/api/compras";
import { useUi } from "@/ui/imperative";

export * from "./types";

export function ComprasFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Compra>();
  const { data, isLoading } = useQuery({
    queryKey: ["compras", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await comprasApi.list(
        list.deferredSearch.trim() || undefined,
        list.page,
        50,
      );
      return {
        itens: res?.itens ?? [],
        totalPages: res?.totalDePaginas ?? 1,
        totalItems: res?.totalDeItens ?? 0,
      };
    },
  });

  const abrir = async (props: CompraWindowProps, title: string) => {
    const result = await ui.windows.open<true, CompraWindowProps>({
      component: CompraWindow,
      props,
      title,
      size: "full",
    });
    if (result.status !== "confirmed") return;
    await Promise.all(
      [
        "compras",
        "skus",
        "produtos",
        "movimentacoes",
        "contas-pagar",
        "fornecedores",
      ].map((k) => queryClient.invalidateQueries({ queryKey: [k] })),
    );
    ui.feedback.notify({
      type: "success",
      title: "Compra registrada com sucesso.",
    });
  };

  const estornar = async (c: Compra) => {
    if (c.estornada) return;
    const r = await ui.windows.open<true, EstornarCompraWindowProps>({
      component: EstornarCompraWindow,
      props: { compraId: c.id, temNfe: !!c.nfeId },
      title: `Estornar compra #${c.id}`,
      size: "small",
    });
    if (r.status !== "confirmed") return;
    await Promise.all(
      ["compras", "skus", "produtos", "movimentacoes", "contas-pagar", "nfes"].map((k) =>
        queryClient.invalidateQueries({ queryKey: [k] }),
      ),
    );
    ui.feedback.notify({ type: "success", title: `Compra #${c.id} estornada.` });
  };

  const importar = async () => {
    const r = await ui.windows.open<ImportacaoXml, Record<string, never>>({
      component: ImportarXmlWindow,
      props: {},
      title: "Importar XML da NF-e",
      size: "large",
    });
    if (r.status === "confirmed")
      await abrir(
        { importacao: r.value },
        `Importação da NF-e ${r.value.previa.dados.numero}`,
      );
  };

  return (
    <ComprasList
      items={data?.itens ?? []}
      loading={isLoading}
      searchTerm={list.searchTerm}
      page={list.page}
      totalPages={data?.totalPages ?? 1}
      totalItems={data?.totalItems ?? 0}
      onSearchChange={list.handleSearchChange}
      onAdd={() => abrir({}, "Nova compra")}
      onEdit={(c) => abrir({ compraId: c.id }, `Compra #${c.id}`)}
      onView={(c) => abrir({ compraId: c.id }, `Compra #${c.id}`)}
      onDelete={estornar}
      onImport={importar}
      onPageChange={list.setPage}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
    />
  );
}
