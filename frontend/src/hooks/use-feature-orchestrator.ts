import { useQuery, useQueryClient } from "@tanstack/react-query";
import React, { useCallback } from "react";
import { RowSelectionState, OnChangeFn } from "@tanstack/react-table";
import { useFeatureList } from "./use-feature-list";

export interface FeatureListProps<TDto> {
  /** Ícone da entidade; a mesma lista pode ser renderizada em uma página ou em uma janela. */
  icon?: React.ReactNode;
  items: TDto[];
  loading: boolean;
  searchTerm: string;
  page: number;
  totalPages: number;
  totalItems: number;
  selectionMode?: boolean;
  onSearchChange: (value: string) => void;
  onAdd: () => void | Promise<void>;
  onEdit: (item: TDto) => void | Promise<void>;
  onView: (item: TDto) => void | Promise<void>;
  onDelete: (item: TDto) => void | Promise<void>;
  onSelect?: (item: TDto) => void | Promise<void>;
  onPageChange: (page: number) => void;
  rowSelection: RowSelectionState;
  onRowSelectionChange: OnChangeFn<RowSelectionState>;
  selectAllAcrossPages?: boolean;
  onSelectAllAcrossPagesChange?: (value: boolean) => void;
  searchInputRef?: React.RefObject<HTMLInputElement | null>;
}

interface UseFeatureOrchestratorProps<TDto> {
  queryKey: string;
  initialSearchTerm?: string;
  fetchPage: (
    searchTerm: string,
    page: number,
    pageSize: number,
  ) => Promise<{ itens: TDto[]; totalPages: number; totalItems: number }>;
  /** Async recipes supplied by the feature; windows and side effects live there. */
  recipes?: {
    create?: () => Promise<void>;
    edit?: (item: TDto) => Promise<void>;
    view?: (item: TDto) => Promise<void>;
    remove?: (item: TDto) => Promise<void>;
    select?: (item: TDto) => Promise<void>;
  };
  additionalKeysToInvalidate?: string[][];
}

/** Shared query/list adapter with no declarative window state. */
export function useFeatureOrchestrator<TDto>({
  queryKey,
  initialSearchTerm = "",
  fetchPage,
  recipes = {},
  additionalKeysToInvalidate = [],
}: UseFeatureOrchestratorProps<TDto>) {
  const list = useFeatureList<TDto>({ initialSearchTerm });
  const queryClient = useQueryClient();
  const { data, isLoading } = useQuery({
    queryKey: [queryKey, list.deferredSearch, list.page],
    queryFn: () => fetchPage(list.deferredSearch.trim(), list.page, 50),
  });

  const invalidate = useCallback(async () => {
    await Promise.all(
      [[queryKey], ...additionalKeysToInvalidate].map((key) =>
        queryClient.invalidateQueries({ queryKey: key }),
      ),
    );
  }, [additionalKeysToInvalidate, queryClient, queryKey]);

  const create = useCallback(async () => recipes.create?.(), [recipes]);
  const edit = useCallback(async (item: TDto) => recipes.edit?.(item), [recipes]);
  const view = useCallback(async (item: TDto) => recipes.view?.(item), [recipes]);
  const remove = useCallback(async (item: TDto) => recipes.remove?.(item), [recipes]);
  const select = useCallback(async (item: TDto) => recipes.select?.(item), [recipes]);
  const listRecipe = useCallback(async () => invalidate(), [invalidate]);

  return {
    listProps: {
      items: data?.itens ?? [],
      loading: isLoading,
      searchTerm: list.searchTerm,
      page: list.page,
      totalPages: data?.totalPages ?? 1,
      totalItems: data?.totalItems ?? 0,
      onSearchChange: list.handleSearchChange,
      onAdd: create,
      onEdit: edit,
      onView: view,
      onDelete: remove,
      onSelect: recipes.select ? select : undefined,
      onPageChange: list.setPage,
      rowSelection: list.rowSelection,
      onRowSelectionChange: list.setRowSelection,
      selectAllAcrossPages: list.selectAllAcrossPages,
      onSelectAllAcrossPagesChange: list.setSelectAllAcrossPages,
    } satisfies FeatureListProps<TDto>,
    recipes: { list: listRecipe, create, edit, view, remove, select },
    featureList: list,
  };
}
