"use client";

import React from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { VendasList } from "./list";
import { VendasUpsertForm, type VendasUpsertProps } from "./upsert";
import type { Venda } from "./types";
import { useFeatureList } from "@/hooks/use-feature-list";
import { vendasApi } from "@/api/vendas";
import { useWindow, useWindowCommands, useUi } from "@/ui/imperative";
import { Button, Kbd, KbdGroup } from "@/ui/primitives";
import { Textarea } from "@/ui/primitives";
import { Field, FieldError, FieldLabel } from "@/ui/primitives";

export * from "./types";

export function VendasFeature() {
  const ui = useUi();
  const queryClient = useQueryClient();
  const list = useFeatureList<Venda>();
  const { data, isLoading } = useQuery({
    queryKey: ["vendas", list.deferredSearch, list.page],
    queryFn: async () => {
      const res = await vendasApi.list(
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

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["vendas"] }),
      queryClient.invalidateQueries({ queryKey: ["skus"] }),
      queryClient.invalidateQueries({ queryKey: ["produtos"] }),
      queryClient.invalidateQueries({ queryKey: ["contas-receber"] }),
      queryClient.invalidateQueries({ queryKey: ["movimentacoes"] }),
    ]);
  };

  // Backend nao possui edicao de venda: venda existente abre sempre em leitura.
  const openVenda = async (item: Venda | null, readOnly = !!item) => {
    const result = await ui.windows.open<true, VendasUpsertProps>({
      component: VendasUpsertForm,
      props: { editingItem: item, readOnly },
      title: item ? "Detalhes da Venda" : "Nova venda",
      size: "full",
    });

    if (result.status === "confirmed") {
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: "Venda registrada com sucesso.",
      });
    }
  };

  const cancelVenda = async (item: Venda) => {
    // Delete na linha chega aqui mesmo sem o botão visível.
    if (item.dataCancelamento) {
      ui.feedback.notify({ type: "info", title: "Venda já está cancelada." });
      return;
    }
    const result = await ui.windows.open<string, VendasCancelWindowProps>({
      component: VendasCancelWindow,
      props: { venda: item },
      title: "Cancelar Venda",
      size: "small",
    });
    if (result.status !== "confirmed") return;

    try {
      await vendasApi.cancel(item.id, result.value);
      await invalidate();
      ui.feedback.notify({
        type: "success",
        title: "Venda cancelada com sucesso.",
      });
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível cancelar a venda.",
      });
    }
  };

  return (
    <VendasList
      items={data?.itens ?? []}
      loading={isLoading}
      searchTerm={list.searchTerm}
      page={list.page}
      totalPages={data?.totalPages ?? 1}
      totalItems={data?.totalItems ?? 0}
      onSearchChange={list.handleSearchChange}
      onAdd={() => openVenda(null)}
      onEdit={(item) => openVenda(item, true)}
      onView={(item) => openVenda(item, true)}
      onDelete={cancelVenda}
      onPageChange={list.setPage}
      rowSelection={list.rowSelection}
      onRowSelectionChange={list.setRowSelection}
      selectAllAcrossPages={list.selectAllAcrossPages}
      onSelectAllAcrossPagesChange={list.setSelectAllAcrossPages}
    />
  );
}

export interface VendasCancelWindowProps {
  venda: Venda;
}

function VendasCancelWindow({ venda }: VendasCancelWindowProps) {
  const activeWindow = useWindow<string>();
  const [error, setError] = React.useState("");

  // Campo não controlado: digitar não re-renderiza a janela; o valor é lido ao
  // confirmar e ao consultar se há alterações não salvas.
  const motivoRef = React.useRef<HTMLTextAreaElement | null>(null);
  const registerMotivo = React.useCallback(
    (element: HTMLTextAreaElement | null) => {
      motivoRef.current = element;
      if (!element) return;
      activeWindow.setDirtyCheck(() => element.value.trim().length > 0);
      return () => activeWindow.setDirtyCheck(null);
    },
    [activeWindow],
  );

  const confirm = React.useCallback(() => {
    const value = (motivoRef.current?.value ?? "").trim();
    if (!value) {
      setError("Motivo do cancelamento é obrigatório.");
      return;
    }
    if (value.length < 5) {
      setError("O motivo deve ter pelo menos 5 caracteres.");
      return;
    }
    activeWindow.resolve(value);
  }, [activeWindow]);

  useWindowCommands(
    React.useMemo(
      () => [
        {
          id: "vendas.cancel.confirm",
          hotkey: "Alt+Enter" as const,
          label: "Confirmar cancelamento",
          run: (event: KeyboardEvent) => {
            event.preventDefault();
            confirm();
          },
        },
      ],
      [confirm],
    ),
  );

  return (
    <div className="flex flex-col gap-4">
      <p className="text-destructive text-xs font-semibold">
        Esta ação reverterá as movimentações de estoque físicas dos itens e
        excluirá a conta a receber gerada. Se alguma parcela já tiver
        recebimento (total ou parcial), o cancelamento será recusado.
      </p>
      <p className="text-sm">
        Tem certeza que deseja cancelar a venda <strong>#{venda.id}</strong>?
      </p>
      <Field data-invalid={!!error}>
        <FieldLabel htmlFor="motivo-cancelamento">
          Motivo do Cancelamento
        </FieldLabel>
        <Textarea
          id="motivo-cancelamento"
          ref={registerMotivo}
          onChange={(event) => {
            if (error && event.target.value.trim().length >= 5) setError("");
          }}
          placeholder="Informe o motivo (mínimo de 5 caracteres)..."
          rows={3}
          maxLength={500}
        />
        {error && <FieldError>{error}</FieldError>}
      </Field>
      <div data-window-actions className="flex justify-end gap-2">
        <Button
          type="button"
          variant="outline"
          onClick={() => activeWindow.dismiss("cancel")}
        >
          Cancelar <Kbd>Esc</Kbd>
        </Button>
        <Button type="button" variant="destructive" onClick={confirm}>
          Confirmar Cancelamento
          <KbdGroup className="ml-2">
            <Kbd>Alt</Kbd>
            <Kbd>Enter</Kbd>
          </KbdGroup>
        </Button>
      </div>
    </div>
  );
}
