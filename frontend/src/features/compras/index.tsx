"use client";
import { ITENS_POR_PAGINA } from "@/config/paginacao";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ComprasList } from "./list";
import {
  CompraWindow,
  type CompraWindowProps,
  type ImportacaoXml,
} from "./upsert";
import { ImportarXmlWindow } from "./importar";
import { EstornarCompraWindow, type EstornarCompraWindowProps } from "./estornar";
import {
  DevolucaoCompraTipoWindow,
  DevolucaoCompraWindow,
  type DevolucaoCompraWindowProps,
  type TipoDevolucaoCompra,
} from "./devolucao";
import { compraSemSaldoDevolvivel, type Compra, type CriarDevolucaoCompraValues } from "./types";
import { useFeatureList } from "@/hooks/use-feature-list";
import { comprasApi } from "@/api/compras";
import { abrirNfe } from "@/features/nfes";
import { useUi } from "@/ui/imperative";

export * from "./types";

export function ComprasFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Compra>();
  const { data, isLoading } = useQuery({
    queryKey: ["compras", list.deferredSearch, list.page, list.ordenacao],
    queryFn: async () => {
      const res = await comprasApi.list(
        list.deferredSearch.trim() || undefined,
        list.page,
        ITENS_POR_PAGINA,
        list.ordenacao,
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

  const devolver = async (c: Compra) => {
    if (compraSemSaldoDevolvivel(c)) {
      ui.feedback.notify({
        type: "info",
        title: c.estornada ? "Compra estornada não aceita devolução." : "Compra sem saldo a devolver.",
      });
      return;
    }
    const tipo = await ui.windows.open<TipoDevolucaoCompra, Record<string, never>>({
      component: DevolucaoCompraTipoWindow,
      props: {},
      title: `Devolução de compra #${c.id}`,
      size: "medium",
    });
    if (tipo.status !== "confirmed") return;

    const result = await ui.windows.open<CriarDevolucaoCompraValues, DevolucaoCompraWindowProps>({
      component: DevolucaoCompraWindow,
      props: { compraId: c.id, total: tipo.value === "total" },
      title: `Devolver Itens da Compra #${c.id}`,
      size: "large",
    });
    if (result.status !== "confirmed") return;

    try {
      const res = await comprasApi.createDevolucao(c.id, result.value);
      await Promise.all(
        ["compras", "skus", "produtos", "movimentacoes", "contas-pagar", "nfes"].map((k) =>
          queryClient.invalidateQueries({ queryKey: [k] }),
        ),
      );
      ui.feedback.notify({ type: "success", title: "Devolução de compra registrada com sucesso." });
      const nfe = res?.data?.nfeDevolucao;
      if (nfe) {
        // A nota original continua ativa; a devolução emite a sua própria NF-e.
        const abrirNota = await ui.windows.confirm({
          title: "NF-e de devolução emitida",
          description: `A devolução emitiu a NF-e de devolução (chave ${nfe.chaveAcesso}). Deseja abrir a NF-e?`,
          confirmLabel: "Abrir NF-e",
        });
        if (abrirNota) await abrirNfe(ui, nfe.nfeId);
      }
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível registrar a devolução.",
      });
    }
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
      onReturn={devolver}
      onPageChange={list.setPage}
        ordenacao={list.ordenacao}
        onOrdenacaoChange={list.alterarOrdenacao}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
    />
  );
}
