"use client";

import React, { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { atributosApi } from "@/api/catalogo";
import {
  MultiEntityInput,
  MultiEntityItem,
} from "@/components/ui/multi-entity-input";
import { AtributosUpsert } from "@/features/catalogo/atributos/upsert";
import type { SkuAtributoChaveResumo } from "@/features/catalogo/atributos/types";

interface AtributoValorMultiInputProps {
  chaveId: number;
  selectedValues: Array<{ id: number; valor: string }>;
  onChange: (newVals: Array<{ id: number; valor: string }>) => void;
}

export function AtributoValorMultiInput({
  chaveId,
  selectedValues,
  onChange,
}: AtributoValorMultiInputProps) {
  const queryClient = useQueryClient();
  const [upsertOpen, setUpsertOpen] = useState(false);

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
      valores: [
        ...detail.skuAtributosValores.map((v) => v.valor),
        trimmed,
      ],
    };

    try {
      const res = await atributosApi.update(chaveId, payload);
      if (res.success && res.data) {
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
      }
    } catch (err) {
      console.error("Erro ao criar valor de atributo:", err);
    }
  };

  const editingResumo: SkuAtributoChaveResumo | null = detail
    ? {
        id: detail.id,
        chave: detail.chave,
        valores: detail.skuAtributosValores?.map((v) => v.valor) ?? [],
      }
    : null;

  return (
    <>
      <MultiEntityInput
        name={`atributo-valores-${chaveId}`}
        label="Valores Selecionados"
        placeholder="Buscar valor..."
        selectedItems={selectedItems}
        availableItems={availableItems}
        onChange={handleChange}
        onCreateOption={handleCreateValue}
        loading={isLoading}
        onEditEntity={() => setUpsertOpen(true)}
        editLabel="Editar Atributo"
      />

      {upsertOpen && (
        <AtributosUpsert
          open={upsertOpen}
          editingItem={editingResumo}
          onClose={() => setUpsertOpen(false)}
          onSuccess={() => {
            queryClient.invalidateQueries({
              queryKey: ["atributos", "multi-input-detail", chaveId],
            });
            queryClient.invalidateQueries({
              queryKey: ["atributos", "selector-detail", chaveId],
            });
            setUpsertOpen(false);
          }}
        />
      )}
    </>
  );
}
