"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { atributosApi } from "@/api/catalogo";
import { useUi } from "@/ui/imperative";
import {
  MultiEntityInput,
  MultiEntityItem,
} from "@/ui/composites";
import { AtributosUpsert } from "@/features/catalogo/atributos/upsert";
import type { AtributosUpsertProps } from "@/features/catalogo/atributos/upsert";
import type { SkuAtributoChave } from "@/features/catalogo/atributos/types";

interface AtributoValorMultiInputProps {
  chaveId: number;
  selectedValues: Array<{ id: number; valor: string }>;
  onChange: (newVals: Array<{ id: number; valor: string }>) => void;
  disabled?: boolean;
}

export function AtributoValorMultiInput({
  chaveId,
  selectedValues,
  onChange,
  disabled = false,
}: AtributoValorMultiInputProps) {
  const ui = useUi();
  const queryClient = useQueryClient();

  const { data: detail, isLoading } = useQuery({
    queryKey: ["atributos", "multi-input-detail", chaveId],
    queryFn: () => atributosApi.getById(chaveId),
    enabled: chaveId > 0,
  });

  const availableItems: MultiEntityItem[] = (
    detail?.skuAtributosValores ?? []
  ).map((v) => ({
    id: v.id,
    label: v.valor,
  }));

  const selectedItems: MultiEntityItem[] = selectedValues.map((v) => ({
    id: v.id,
    label: v.valor,
  }));

  const handleChange = (items: MultiEntityItem[]) => {
    onChange(items.map((i) => ({ id: i.id, valor: i.label })));
  };

  const handleCreateValue = async (newVal: string) => {
    if (!detail) return;
    const trimmed = newVal.trim();
    if (!trimmed) return;

    if (
      detail.skuAtributosValores.some(
        (v) => v.valor.toLowerCase() === trimmed.toLowerCase(),
      )
    ) {
      return;
    }

    const payload = {
      chave: detail.chave,
      valores: [...detail.skuAtributosValores.map((v) => v.valor), trimmed],
    };

    try {
      const res = await atributosApi.update(chaveId, payload);
      if (!res.success || !res.data) {
        ui.feedback.notifyError(res, {
          fallbackTitle: "Não foi possível criar o valor do atributo.",
        });
        return;
      }

      await queryClient.invalidateQueries({
        queryKey: ["atributos", "multi-input-detail", chaveId],
      });
      await queryClient.invalidateQueries({
        queryKey: ["atributos", "selector-detail", chaveId],
      });

      const newCreated = res.data.skuAtributosValores.find(
        (v) => v.valor.toLowerCase() === trimmed.toLowerCase(),
      );

      if (newCreated) {
        onChange([
          ...selectedValues,
          { id: newCreated.id, valor: newCreated.valor },
        ]);
      }
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível criar o valor do atributo.",
      });
    }
  };

  const editingResumo: SkuAtributoChave | null = detail
    ? {
        id: detail.id,
        chave: detail.chave,
        skuAtributosValores: detail.skuAtributosValores ?? [],
      }
    : null;

  const handleEditEntity = async () => {
    if (!editingResumo) return;

    try {
      const result = await ui.windows.open<true, AtributosUpsertProps>({
        component: AtributosUpsert,
        props: { editingItem: editingResumo },
        title: "Editar Atributo",
      });

      if (result.status !== "confirmed") return;

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["atributos", "multi-input-detail", chaveId],
        }),
        queryClient.invalidateQueries({
          queryKey: ["atributos", "selector-detail", chaveId],
        }),
      ]);

      const refreshedDetail = queryClient.getQueryData<SkuAtributoChave>([
        "atributos",
        "multi-input-detail",
        chaveId,
      ]);
      if (!refreshedDetail) return;

      const valuesById = new Map(
        refreshedDetail.skuAtributosValores.map((value) => [value.id, value]),
      );
      const refreshedSelection = selectedValues.flatMap((value) => {
        const refreshedValue = valuesById.get(value.id);
        return refreshedValue
          ? [{ id: refreshedValue.id, valor: refreshedValue.valor }]
          : [];
      });

      if (
        refreshedSelection.length !== selectedValues.length ||
        refreshedSelection.some(
          (value, index) =>
            value.id !== selectedValues[index]?.id ||
            value.valor !== selectedValues[index]?.valor,
        )
      ) {
        onChange(refreshedSelection);
      }
    } catch (error) {
      ui.feedback.notifyError(error, {
        fallbackTitle: "Não foi possível atualizar os valores do atributo.",
      });
    }
  };

  return (
    <MultiEntityInput
      name={`atributo-valores-${chaveId}`}
      label="Valores Selecionados"
      placeholder="Buscar valor..."
      selectedItems={selectedItems}
      availableItems={availableItems}
      onChange={handleChange}
      onCreateOption={handleCreateValue}
      loading={isLoading}
      onEditEntity={() => void handleEditEntity()}
      editLabel="Editar Atributo"
      disabled={disabled}
    />
  );
}
