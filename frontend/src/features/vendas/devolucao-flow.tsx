"use client";

import { useQueryClient } from "@tanstack/react-query";
import {
  DevolucaoTipoWindow,
  DevolucaoWindow,
  type DevolucaoWindowProps,
  type TipoDevolucao,
} from "./devolucao";
import type { CriarDevolucaoValues, Venda } from "./types";
import { vendasApi } from "@/api/vendas";
import { useUi } from "@/ui/imperative";
import { abrirNfe } from "@/features/nfes";

/**
 * Fluxo de realizar devolução de uma venda: tipo (parcial/total), itens, registro, invalidação
 * das queries e aviso de NF-e ativa. Aceita venda parcial da listagem (a janela busca o detalhe).
 * Retorna true se a devolução foi registrada.
 */
export function useDevolucaoFlow() {
  const ui = useUi();
  const queryClient = useQueryClient();

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["vendas"] }),
      queryClient.invalidateQueries({ queryKey: ["devolucoes"] }),
      queryClient.invalidateQueries({ queryKey: ["skus"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
      queryClient.invalidateQueries({ queryKey: ["contas-receber"] }),
      queryClient.invalidateQueries({ queryKey: ["contas-pagar"] }),
      queryClient.invalidateQueries({ queryKey: ["movimentacoes"] }),
      queryClient.invalidateQueries({ queryKey: ["nfes"] }),
    ]);
  };

  const realizarDevolucao = async (venda: Venda): Promise<boolean> => {
    if (venda.statusDevolucao === "TOTAL") {
      ui.feedback.notify({ type: "info", title: "Venda já foi totalmente devolvida." });
      return false;
    }
    const tipo = await ui.windows.open<TipoDevolucao, Record<string, never>>({
      component: DevolucaoTipoWindow,
      props: {},
      title: `Devolução de venda #${venda.id}`,
      size: "medium",
    });
    if (tipo.status !== "confirmed") return false;

    const result = await ui.windows.open<CriarDevolucaoValues, DevolucaoWindowProps>({
      component: DevolucaoWindow,
      props: { venda, total: tipo.value === "total" },
      title: `Devolver Itens da Venda #${venda.id}`,
      size: "large",
    });
    if (result.status !== "confirmed") return false;

    try {
      const res = await vendasApi.createDevolucao(venda.id, result.value);
      await invalidate();
      ui.feedback.notify({ type: "success", title: "Devolução de venda registrada com sucesso." });
      const nfe = res?.data?.nfeDevolucao;
      if (nfe) {
        // A nota original continua ativa; a devolução emite a sua própria NF-e.
        const r = await ui.windows.confirm({
          title: "NF-e de devolução emitida",
          description: `A devolução emitiu a NF-e de devolução (chave ${nfe.chaveAcesso}). Deseja abrir a NF-e?`,
          confirmLabel: "Abrir NF-e",
        });
        if (r) await abrirNfe(ui, nfe.nfeId);
      }
      return true;
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível registrar a devolução.",
      });
      return false;
    }
  };

  return { realizarDevolucao };
}
