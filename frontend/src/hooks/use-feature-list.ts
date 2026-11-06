import { useCallback, useState } from "react";
import { RowSelectionState } from "@tanstack/react-table";
import { useDebounce } from "@/hooks/use-debounce";

const ATRASO_BUSCA_MS = 400;

interface UseFeatureListOptions<T> {
  initialSearchTerm?: string;
  recipes?: Partial<FeatureListRecipes<T>>;
}

export interface FeatureListRecipes<T> {
  list: () => Promise<void>;
  create: () => Promise<void>;
  edit: (item: T) => Promise<void>;
  view: (item: T) => Promise<void>;
  remove: (item: T) => Promise<void>;
  select: (item: T) => Promise<void>;
}

export function useFeatureList<T>({
  initialSearchTerm = "",
  recipes: suppliedRecipes,
}: UseFeatureListOptions<T> = {}) {
  const [searchTerm, setSearchTerm] = useState(initialSearchTerm ?? "");
  // Termo que vai para a consulta: so muda depois que a digitacao para.
  const [deferredSearch, setDeferredSearch] = useState(initialSearchTerm ?? "");
  const debounceBusca = useDebounce(setDeferredSearch, ATRASO_BUSCA_MS);
  const [page, setPage] = useState(1);

  const [rowSelection, setRowSelectionRaw] = useState<RowSelectionState>({});
  const [selectAllAcrossPages, setSelectAllAcrossPages] = useState(false);

  const setRowSelection = (
    updaterOrValue:
      | RowSelectionState
      | ((old: RowSelectionState) => RowSelectionState),
  ) => {
    // O updater do setState precisa ser puro (roda duas vezes em StrictMode):
    // calcula o valor aqui e chama os dois setters no handler.
    setRowSelectionRaw(
      typeof updaterOrValue === "function"
        ? updaterOrValue(rowSelection)
        : updaterOrValue,
    );
    setSelectAllAcrossPages(false);
  };
  const handleSearchChange = useCallback((val: string) => {
    setSearchTerm(val);
    setPage(1);
    // Limpar responde na hora; digitar espera a pausa.
    if (val === "") debounceBusca.imediato("");
    else debounceBusca.chamar(val);
  }, [debounceBusca]);

  // Product code owns interaction orchestration. Recipes are always async so
  // callers can await windows, APIs, invalidation and feedback in one place.
  const list = useCallback(async () => { await suppliedRecipes?.list?.(); }, [suppliedRecipes]);
  const create = useCallback(async () => { await suppliedRecipes?.create?.(); }, [suppliedRecipes]);
  const edit = useCallback(async (item: T) => { await suppliedRecipes?.edit?.(item); }, [suppliedRecipes]);
  const view = useCallback(async (item: T) => { await suppliedRecipes?.view?.(item); }, [suppliedRecipes]);
  const remove = useCallback(async (item: T) => { await suppliedRecipes?.remove?.(item); }, [suppliedRecipes]);
  const select = useCallback(async (item: T) => { await suppliedRecipes?.select?.(item); }, [suppliedRecipes]);

  return {
    searchTerm,
    deferredSearch,
    page,
    setPage,
    rowSelection,
    setRowSelection,
    selectAllAcrossPages,
    setSelectAllAcrossPages,
    handleSearchChange,
    recipes: { list, create, edit, view, remove, select },
  };
}
