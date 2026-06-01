import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { RowSelectionState, OnChangeFn } from "@tanstack/react-table";
import { useFeatureList } from "./use-feature-list";

export interface FeatureListProps<TDto> {
  items: TDto[];
  loading: boolean;
  searchTerm: string;
  page: number;
  totalPages: number;
  totalItems: number;
  selectionMode?: boolean;
  onSearchChange: (val: string) => void;
  onAdd: () => void;
  onEdit: (item: TDto) => void;
  onDelete: (item: TDto) => void;
  onSelect?: (item: TDto) => void;
  onPageChange: (page: number) => void;
  rowSelection: RowSelectionState;
  onRowSelectionChange: OnChangeFn<RowSelectionState>;
  selectAllAcrossPages?: boolean;
  onSelectAllAcrossPagesChange?: (value: boolean) => void;
}

interface UseFeatureOrchestratorProps<TDto extends { id: number }> {
  queryKey: string;
  initialSearchTerm?: string;
  fetchPage: (
    searchTerm: string,
    page: number,
    pageSize: number,
  ) => Promise<{ itens: TDto[]; totalPages: number; totalItems: number }>;
  deleteItem: (id: number) => Promise<void>;
}

export function useFeatureOrchestrator<TDto extends { id: number }>({
  queryKey,
  initialSearchTerm = "",
  fetchPage,
  deleteItem,
}: UseFeatureOrchestratorProps<TDto>) {
  const list = useFeatureList<TDto>({ initialSearchTerm });
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: [queryKey, list.deferredSearch, list.page],
    queryFn: async () =>
      await fetchPage(list.deferredSearch.trim(), list.page, 10),
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: number) => await deleteItem(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [queryKey] });
      list.setDeleteDialogVisible(false);
    },
    onError: (e) => {
      console.error(e);
    },
  });

  const handleConfirmDelete = () => {
    if (list.itemToDelete) {
      deleteMutation.mutate(list.itemToDelete.id);
    }
  };

  return {
    listProps: {
      items: data?.itens ?? [],
      loading: isLoading,
      searchTerm: list.searchTerm,
      page: list.page,
      totalPages: data?.totalPages ?? 1,
      totalItems: data?.totalItems ?? 0,
      onSearchChange: list.handleSearchChange,
      onAdd: list.handleCreate,
      onEdit: list.handleEdit,
      onDelete: list.handleDeleteClick,
      onPageChange: list.setPage,
      rowSelection: list.rowSelection,
      onRowSelectionChange: list.setRowSelection,
      selectAllAcrossPages: list.selectAllAcrossPages,
      onSelectAllAcrossPagesChange: list.setSelectAllAcrossPages,
    },
    upsertProps: {
      open: list.isUpsertOpen,
      editingItem: list.editingItem,
      onClose: () => list.setIsUpsertOpen(false),
      onSuccess: () => {},
    },
    deleteDialogProps: {
      open: list.deleteDialogOpen,
      onOpenChange: list.setDeleteDialogVisible,
      onConfirm: handleConfirmDelete,
      loading: deleteMutation.isPending,
      itemToDelete: list.itemToDelete,
    },
    featureList: list,
  };
}
