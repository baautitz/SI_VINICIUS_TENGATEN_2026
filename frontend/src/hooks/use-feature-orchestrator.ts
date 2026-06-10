import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { RowSelectionState, OnChangeFn } from "@tanstack/react-table";
import { useFeatureList } from "./use-feature-list";
import { toast } from "sonner";
import { extractApiErrors } from "@/utils/api-error";

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
  onView: (item: TDto) => void;
  onDelete: (item: TDto) => void;
  onSelect?: (item: TDto) => void;
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
  fetchById?: (id: string | number) => Promise<TDto>;
  deleteItem?: (item: TDto) => Promise<void>;
  additionalKeysToInvalidate?: string[][];
}

export function useFeatureOrchestrator<TDto extends { id?: string | number; sku?: string }>({
  queryKey,
  initialSearchTerm = "",
  fetchPage,
  fetchById,
  deleteItem,
  additionalKeysToInvalidate = [],
}: UseFeatureOrchestratorProps<TDto>) {
  const list = useFeatureList<TDto>({ initialSearchTerm });
  const queryClient = useQueryClient();

  const allKeysToInvalidate = [[queryKey], ...additionalKeysToInvalidate];

  const invalidateAll = async () => {
    await Promise.all(
      allKeysToInvalidate.map((key) =>
        queryClient.invalidateQueries({ queryKey: key }),
      ),
    );
  };

  const { data, isLoading } = useQuery({
    queryKey: [queryKey, list.deferredSearch, list.page],
    queryFn: async () =>
      await fetchPage(list.deferredSearch.trim(), list.page, 50),
  });

  const itemId = list.editingItem?.id ?? list.editingItem?.sku;
  const { data: freshItem, isLoading: isLoadingDetail } = useQuery({
    queryKey: [queryKey, "detail", itemId],
    queryFn: () => fetchById!(itemId!),
    enabled: !!itemId && !!fetchById && list.isUpsertOpen,
  });

  const deleteMutation = useMutation({
    mutationFn: async (item: TDto) => {
      if (deleteItem) {
        await deleteItem(item);
      }
    },
    onSuccess: async () => {
      await invalidateAll();
      list.setDeleteDialogVisible(false);
    },
    onError: (e) => {
      const apiErrors = extractApiErrors(e);
      toast.error(apiErrors.globalError || "Erro ao deletar o registro.");
    },
  });

  const handleConfirmDelete = () => {
    if (list.itemToDelete) {
      deleteMutation.mutate(list.itemToDelete);
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
      onView: list.handleView,
      onDelete: list.handleDeleteClick,
      onPageChange: list.setPage,
      rowSelection: list.rowSelection,
      onRowSelectionChange: list.setRowSelection,
      selectAllAcrossPages: list.selectAllAcrossPages,
      onSelectAllAcrossPagesChange: list.setSelectAllAcrossPages,
    },
    upsertProps: {
      open: list.isUpsertOpen,
      editingItem: freshItem || list.editingItem,
      readOnly: list.readOnly,
      loading: isLoadingDetail,
      onClose: () => list.setIsUpsertOpen(false),
      onSuccess: async () => {
        await invalidateAll();
      },
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
