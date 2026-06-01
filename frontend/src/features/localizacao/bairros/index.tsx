"use client";

import { BairrosClient } from "@/api/client";
import { API_URL } from "@/api/url";
import { BairrosList } from "./list";
import { BairrosUpsert } from "./upsert";
import { BairroDto } from "./types";
import { DeleteDialog } from "@/components/ui/delete-dialog";
import { useFeatureOrchestrator } from "@/hooks/use-feature-orchestrator";

export type { BairroDto };

interface BairrosFeatureProps {
  selectionMode?: boolean;
  onSelect?: (bairro: BairroDto) => void;
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
  } = useFeatureOrchestrator<BairroDto>({
    queryKey: "bairros",
    initialSearchTerm,
    fetchPage: async (searchTerm, page, pageSize) => {
      const client = new BairrosClient(API_URL);
      const res = await client.getBairros(
        searchTerm || undefined,
        page,
        pageSize,
      );
      if (!res?.itens) return { itens: [], totalPages: 1, totalItems: 0 };

      return {
        itens: res.itens as unknown as BairroDto[],
        totalPages: res.totalDePaginas ?? 1,
        totalItems: res.totalDeItens ?? 0,
      };
    },
    deleteItem: async (id) => {
      await new BairrosClient(API_URL).deleteBairro(id);
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
