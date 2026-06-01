"use client";

import { BairrosList } from "./list";
import { BairrosUpsert } from "./upsert";
import { BairroResumo } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";
import { bairrosApi } from "@/api/localizacao";

export * from "./types";

interface BairrosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (bairro: BairroResumo) => void;
  initialSearchTerm?: string;
}

export function BairrosFeature({
  selectionMode = false,
  onSelect,
  initialSearchTerm = "",
}: BairrosFeatureProps) {
  const {
    listProps,
    upsertProps,
    deleteDialogProps,
    featureList: list,
  } = useFeatureOrchestrator<BairroResumo>({
    queryKey: "bairros",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const res = await bairrosApi.list(searchTerm || undefined, page, pageSize);
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens,
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (id) => {
      await bairrosApi.delete(id);
    },
  });

  return (
    <>
      <BairrosList
        {...listProps}
        selectionMode={selectionMode}
        onSelect={onSelect}
      />

      {list.isUpsertOpen && (
        <BairrosUpsert key={list.editingItem?.id ?? "new"} {...upsertProps} />
      )}

      <DeleteDialog
        {...deleteDialogProps}
        title="Excluir Bairro"
        description={
          <p>
            Deseja realmente excluir o bairro{" "}
            <strong>{list.itemToDelete?.bairro}</strong> de{" "}
            <strong>
              {list.itemToDelete?.cidadeNome} ({list.itemToDelete?.uf})
            </strong>
            ? Esta ação não poderá ser desfeita.
          </p>
        }
      />
    </>
  );
}
