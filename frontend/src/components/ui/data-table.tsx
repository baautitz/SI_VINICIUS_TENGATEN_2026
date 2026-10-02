"use client";

import * as React from "react";
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  useReactTable,
  RowSelectionState,
  OnChangeFn,
} from "@tanstack/react-table";
import {
  Search,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
} from "lucide-react";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/ui/primitives";
import { Button } from "@/ui/primitives";
import { Spinner } from "@/ui/primitives";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { useWindowCommands } from "@/ui/imperative";
import { useOptionalActiveWindow } from "@/ui/imperative";
import { useNavigationScope } from "@/ui/keyboard-navigation";
import { InputGroup, InputGroupInput, InputGroupAddon } from "./input-group";
import { fireAndForget } from "@/lib/utils";

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  loading?: boolean;

  pageCount: number;
  pageIndex: number;
  onPageChange: (pageIndex: number) => void;
  totalItems?: number;

  globalFilter?: string;
  onGlobalFilterChange?: (filter: string) => void;
  searchPlaceholder?: string;

  rowSelection?: RowSelectionState;
  onRowSelectionChange?: OnChangeFn<RowSelectionState>;

  selectAllAcrossPages?: boolean;
  onSelectAllAcrossPagesChange?: (value: boolean) => void;

  actions?: React.ReactNode;

  getRowId?: (originalRow: TData, index: number, parent?: unknown) => string;

  onRowSelect?: (row: TData) => void;

  onEditRow?: (row: TData) => void;
  onDeleteRow?: (row: TData) => void;

  searchInputRef?: React.RefObject<HTMLInputElement | null>;
  hideSearchKbd?: boolean;
}

export function DataTable<TData, TValue>({
  columns,
  data,
  loading,
  pageCount,
  pageIndex,
  onPageChange,
  totalItems,
  globalFilter,
  onGlobalFilterChange,
  searchPlaceholder = "Filtrar...",
  rowSelection = {},
  onRowSelectionChange,
  selectAllAcrossPages = false,
  onSelectAllAcrossPagesChange,
  actions,
  getRowId,
  onRowSelect,
  onEditRow,
  onDeleteRow,
  searchInputRef,
  hideSearchKbd,
}: DataTableProps<TData, TValue>) {
  "use no memo";

  const isSelectionMode = !!onRowSelect;
  const hasKeyboardNav = true;
  const navigationScopeId = React.useId();
  const [focusedRowIndex, setFocusedRowIndex] = React.useState<number | null>(
    null,
  );
  const navigationRootRef = React.useRef<HTMLDivElement>(null);
  const internalSearchInputRef = React.useRef<HTMLInputElement>(null);
  const activeSearchInputRef = searchInputRef || internalSearchInputRef;
  const activeWindow = useOptionalActiveWindow<unknown>();

  // WindowManagerHost owns the normal modal scope. This low-priority local
  // scope is a portal-safe fallback for lists whose dialog root is not yet
  // connected when the first keyboard interaction happens.
  useNavigationScope(navigationRootRef, {
    id: `data-table-${navigationScopeId}`,
    active: activeWindow?.isActive ?? true,
    priority: -1,
  });

  // Trocar de página invalida a linha focada: ajusta durante o render, sem
  // um render extra com o índice da página anterior.
  const [prevPageIndex, setPrevPageIndex] = React.useState(pageIndex);
  if (pageIndex !== prevPageIndex) {
    setPrevPageIndex(pageIndex);
    setFocusedRowIndex(null);
  }

  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data,
    columns,
    state: {
      rowSelection,
    },
    manualPagination: true,
    manualFiltering: true,
    manualSorting: true,
    enableRowSelection: true,
    onRowSelectionChange: onRowSelectionChange,
    getCoreRowModel: getCoreRowModel(),
    getRowId: getRowId,
  });

  const rows = table.getRowModel().rows;

  const commands = React.useMemo(
    () => [
      {
        id: "table.search",
        hotkey: "Alt+Q" as const,
        label: "Focar busca",
        run: (event: KeyboardEvent) => {
          event.preventDefault();
          activeSearchInputRef?.current?.focus();
          activeSearchInputRef?.current?.select();
        },
      },
      {
        id: "table.confirm-row",
        hotkey: "Enter" as const,
        label: "Selecionar ou editar linha",
        enabled: focusedRowIndex !== null && rows.length > 0,
        // Só consome Enter quando uma linha está focada. Se o foco estiver em
        // um link/botão externo (por exemplo, a busca do menu lateral), a ação
        // nativa desse controle precisa continuar funcionando.
        preventDefault: false,
        stopPropagation: false,
        run: (event: KeyboardEvent) => {
          const activeElement = document.activeElement as HTMLElement;
          if (["INPUT", "TEXTAREA", "SELECT", "A", "BUTTON"].includes(activeElement?.tagName)) return;
          if (focusedRowIndex === null || !rows[focusedRowIndex]) return;
          event.preventDefault();
          const rowData = rows[focusedRowIndex].original;
          if (onRowSelect) fireAndForget(() => onRowSelect(rowData));
          else if (onEditRow) fireAndForget(() => onEditRow(rowData));
        },
      },
      {
        id: "table.edit-row",
        hotkey: "Alt+E" as const,
        label: "Editar linha",
        enabled: focusedRowIndex !== null && rows.length > 0 && !!onEditRow,
        run: (event: KeyboardEvent) => {
          if (focusedRowIndex === null || !rows[focusedRowIndex] || !onEditRow) return;
          event.preventDefault();
          fireAndForget(() => onEditRow(rows[focusedRowIndex].original));
        },
      },
      ...(["Delete", "Backspace"] as const).map((hotkey) => ({
        id: `table.delete-row.${hotkey.toLowerCase()}`,
        hotkey,
        label: "Excluir linha",
        enabled: focusedRowIndex !== null && rows.length > 0 && !!onDeleteRow,
        // O atalho também fica registrado enquanto o filtro está focado. A
        // decisão de consumir a tecla precisa acontecer no handler, depois de
        // verificar o elemento ativo, para não quebrar Backspace/Delete em
        // inputs de busca.
        preventDefault: false,
        stopPropagation: false,
        run: (event: KeyboardEvent) => {
          if (focusedRowIndex === null || !rows[focusedRowIndex] || !onDeleteRow) return;
          const eventTarget = event.target as HTMLElement | null;
          const focusedTag = eventTarget?.tagName ?? document.activeElement?.tagName;
          if (["INPUT", "TEXTAREA", "SELECT", "BUTTON"].includes(focusedTag ?? "")) return;
          event.preventDefault();
          fireAndForget(() => onDeleteRow(rows[focusedRowIndex].original));
        },
      })),
    ],
    [activeSearchInputRef, focusedRowIndex, onDeleteRow, onEditRow, onRowSelect, rows],
  );

  useWindowCommands(commands);

  return (
    <div
      ref={navigationRootRef}
      data-navigation-list="true"
      className="flex min-h-0 flex-1 flex-col space-y-4"
      onBlur={(e) => {
        if (!e.currentTarget.contains(e.relatedTarget as Node)) {
          setFocusedRowIndex(null);
        }
      }}
    >
      <div className="flex shrink-0 items-center justify-between gap-2">
        <div className="flex flex-1 items-center gap-2">
          {onGlobalFilterChange && (
            <div className="w-full flex-1">
              <InputGroup>
                <InputGroupInput
                  ref={activeSearchInputRef}
                  autoFocus
                  placeholder={searchPlaceholder}
                  value={globalFilter ?? ""}
                  onChange={(event) => onGlobalFilterChange(event.target.value)}
                  className="h-8"
                  data-navigation-list-search="true"
                />

                <InputGroupAddon>
                  <Search className="text-muted-foreground size-4" />
                </InputGroupAddon>

                {!hideSearchKbd && (
                  <InputGroupAddon align="inline-end">
                    <KbdGroup>
                      <Kbd>Alt</Kbd>
                      <Kbd>Q</Kbd>
                    </KbdGroup>
                  </InputGroupAddon>
                )}
              </InputGroup>
            </div>
          )}
          {actions}
        </div>
      </div>

      <div className="bg-card relative flex max-h-full flex-1 flex-col overflow-hidden rounded-xl border">
        <Table>
          <TableHeader className="bg-muted sticky top-0 z-10">
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow
                key={headerGroup.id}
                className="border-b hover:bg-transparent"
              >
                {headerGroup.headers.map((header) => {
                  return (
                    <TableHead
                      key={header.id}
                      className="text-foreground h-10 py-2 font-bold whitespace-nowrap"
                      style={{
                        width:
                          header.column.getSize() !== 150
                            ? header.column.getSize()
                            : undefined,
                      }}
                    >
                      {header.isPlaceholder
                        ? null
                        : flexRender(
                            header.column.columnDef.header,
                            header.getContext(),
                          )}
                    </TableHead>
                  );
                })}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody className="overflow-y-scroll">
            {table.getIsAllPageRowsSelected() &&
              totalItems !== undefined &&
              totalItems > table.getRowModel().rows.length &&
              onSelectAllAcrossPagesChange && (
                <TableRow className="bg-muted/30 hover:bg-muted/30 border-b">
                  <TableCell
                    colSpan={columns.length}
                    className="py-3 text-center text-sm"
                  >
                    {selectAllAcrossPages ? (
                      <>
                        <span className="text-muted-foreground mr-2">
                          Todas as <strong>{totalItems}</strong> entidades estão
                          selecionadas.
                        </span>
                        <Button
                          variant="link"
                          className="h-auto p-0 font-semibold"
                          onClick={() => onSelectAllAcrossPagesChange(false)}
                        >
                          Limpar seleção
                        </Button>
                      </>
                    ) : (
                      <>
                        <span className="text-muted-foreground mr-2">
                          Todas as{" "}
                          <strong>{table.getRowModel().rows.length}</strong>{" "}
                          entidades desta página estão selecionadas.
                        </span>
                        <Button
                          variant="link"
                          className="h-auto p-0 font-semibold"
                          onClick={() => onSelectAllAcrossPagesChange(true)}
                        >
                          Selecionar todas as {totalItems} entidades
                        </Button>
                      </>
                    )}
                  </TableCell>
                </TableRow>
              )}
            {loading ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="full text-center"
                >
                  <div className="text-muted-foreground flex flex-col items-center justify-center gap-3 py-8">
                    <Spinner className="size-6" />
                    <span className="animate-pulse font-medium">
                      Carregando dados...
                    </span>
                  </div>
                </TableCell>
              </TableRow>
            ) : rows?.length ? (
              rows.map((row, index) => (
                <TableRow
                  data-navigation-list-item="true"
                  key={row.id}
                  data-state={row.getIsSelected() && "selected"}
                  data-focused={
                    hasKeyboardNav && focusedRowIndex === index
                      ? "true"
                      : undefined
                  }
                  tabIndex={hasKeyboardNav ? 0 : -1}
                  className={[
                    "group border-b transition-colors last:border-0",
                    hasKeyboardNav ? "cursor-pointer focus:outline-none" : "",
                    hasKeyboardNav && focusedRowIndex === index
                      ? "bg-primary/5 outline-primary relative z-10 outline-2 -outline-offset-2"
                      : "",
                  ].join(" ")}
                  onFocus={() => hasKeyboardNav && setFocusedRowIndex(index)}
                  onBlur={(event) => {
                    if (!event.currentTarget.contains(event.relatedTarget)) {
                      setFocusedRowIndex((current) =>
                        current === index ? null : current,
                      )
                    }
                  }}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell
                      key={cell.id}
                      className="py-3"
                      style={{
                        width:
                          cell.column.getSize() !== 150
                            ? cell.column.getSize()
                            : undefined,
                      }}
                    >
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext(),
                      )}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="text-muted-foreground h-full text-center"
                >
                  Nenhum resultado encontrado.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="mt-auto flex items-center justify-between">
        <div className="flex flex-1 flex-col gap-0.5">
          {isSelectionMode ? (
            <span className="text-muted-foreground text-xs font-medium">
              ↑↓ para navegar · Enter para selecionar
            </span>
          ) : (
            <span className="text-muted-foreground text-xs font-medium">
              ↑↓ para navegar · Alt+E para editar
            </span>
          )}
          {!isSelectionMode && Object.keys(rowSelection).length > 0 && (
            <span className="text-primary text-xs font-semibold">
              {selectAllAcrossPages
                ? totalItems
                : Object.keys(rowSelection).length}{" "}
              de {totalItems ?? table.getFilteredRowModel().rows.length}{" "}
              selecionado(s).
            </span>
          )}
        </div>
        <div className="flex items-center space-x-6 lg:space-x-8">
          <div className="flex items-center space-x-2">
            <p className="text-foreground/80 text-sm font-semibold">
              Página {pageIndex} de {pageCount || 1}
            </p>
          </div>
          <div className="flex items-center space-x-2">
            <Button
              variant="outline"
              className="hidden h-9 w-9 rounded-lg p-0 lg:flex"
              onClick={() => onPageChange(1)}
              disabled={pageIndex <= 1 || loading}
            >
              <span className="sr-only">Ir para primeira página</span>
              <ChevronsLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              className="h-9 w-9 rounded-lg p-0"
              onClick={() => onPageChange(pageIndex - 1)}
              disabled={pageIndex <= 1 || loading}
            >
              <span className="sr-only">Página anterior</span>
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              className="h-9 w-9 rounded-lg p-0"
              onClick={() => onPageChange(pageIndex + 1)}
              disabled={pageIndex >= pageCount || loading}
            >
              <span className="sr-only">Próxima página</span>
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              className="hidden h-9 w-9 rounded-lg p-0 lg:flex"
              onClick={() => onPageChange(pageCount)}
              disabled={pageIndex >= pageCount || loading}
            >
              <span className="sr-only">Ir para última página</span>
              <ChevronsRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
