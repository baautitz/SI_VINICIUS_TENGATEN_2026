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
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { useHotkeys } from "@tanstack/react-hotkeys";
import { InputGroup, InputGroupInput, InputGroupAddon } from "./input-group";

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
  const [focusedRowIndex, setFocusedRowIndex] = React.useState<number | null>(
    null,
  );
  const rowRefs = React.useRef<(HTMLTableRowElement | null)[]>([]);
  const internalSearchInputRef = React.useRef<HTMLInputElement>(null);
  const activeSearchInputRef = searchInputRef || internalSearchInputRef;

  useHotkeys(
    [
      {
        hotkey: "Alt+Q",
        callback: (e: KeyboardEvent) => {
          const isInsideDialog =
            !!activeSearchInputRef?.current?.closest('[role="dialog"]');
          const anyDialogOpen =
            typeof document !== "undefined" &&
            !!document.querySelectorAll('[role="dialog"]').length;

          if (anyDialogOpen && !isInsideDialog) {
            return;
          }

          e.preventDefault();
          activeSearchInputRef?.current?.focus();
          activeSearchInputRef?.current?.select();
        },
        options: {
          ignoreInputs: false,
        },
      },
      {
        hotkey: "Enter",
        callback: (e: KeyboardEvent) => {
          if (focusedRowIndex === null || rows.length === 0) return;

          const activeElement = document.activeElement as HTMLElement;
          if (
            activeElement?.tagName === "INPUT" ||
            activeElement?.tagName === "TEXTAREA" ||
            activeElement?.tagName === "SELECT" ||
            activeElement?.tagName === "A" ||
            activeElement?.tagName === "BUTTON"
          ) {
            return;
          }

          const rowData = rows[focusedRowIndex].original;
          const dialogs =
            typeof document !== "undefined"
              ? document.querySelectorAll('[role="dialog"]')
              : [];
          const topDialog =
            dialogs.length > 0 ? dialogs[dialogs.length - 1] : null;

          if (
            topDialog &&
            rowRefs.current[focusedRowIndex]?.closest('[role="dialog"]') !==
              topDialog
          ) {
            return;
          }

          e.preventDefault();
          if (onRowSelect) {
            onRowSelect(rowData);
          } else if (onEditRow) {
            onEditRow(rowData);
          }
        },
        options: {
          enabled: focusedRowIndex !== null,
          ignoreInputs: true,
        },
      },
      {
        hotkey: "Alt+E",
        callback: (e: KeyboardEvent) => {
          if (focusedRowIndex === null || rows.length === 0 || !onEditRow)
            return;
          const rowData = rows[focusedRowIndex].original;
          e.preventDefault();
          onEditRow(rowData);
        },
        options: {
          enabled: focusedRowIndex !== null && !!onEditRow,
          ignoreInputs: false,
        },
      },
      {
        hotkey: "Delete",
        callback: (e: KeyboardEvent) => {
          if (focusedRowIndex === null || rows.length === 0 || !onDeleteRow)
            return;

          if (
            document.activeElement?.tagName === "INPUT" ||
            document.activeElement?.tagName === "TEXTAREA" ||
            document.activeElement?.tagName === "SELECT"
          ) {
            return;
          }

          const rowData = rows[focusedRowIndex].original;
          e.preventDefault();
          onDeleteRow(rowData);
        },
        options: {
          enabled: focusedRowIndex !== null && !!onDeleteRow,
          ignoreInputs: true,
        },
      },
      {
        hotkey: "Backspace",
        callback: (e: KeyboardEvent) => {
          if (focusedRowIndex === null || rows.length === 0 || !onDeleteRow)
            return;

          if (
            document.activeElement?.tagName === "INPUT" ||
            document.activeElement?.tagName === "TEXTAREA" ||
            document.activeElement?.tagName === "SELECT"
          ) {
            return;
          }

          const rowData = rows[focusedRowIndex].original;
          e.preventDefault();
          onDeleteRow(rowData);
        },
        options: {
          enabled: focusedRowIndex !== null && !!onDeleteRow,
          ignoreInputs: true,
        },
      },
    ],
    { conflictBehavior: "allow" },
  );

  React.useEffect(() => {
    const timeout = setTimeout(() => {
      activeSearchInputRef?.current?.focus();
    }, 100);
    return () => clearTimeout(timeout);
  }, [activeSearchInputRef]);

  React.useEffect(() => {
    const input = activeSearchInputRef?.current;
    if (!input || !hasKeyboardNav) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "ArrowDown") {
        e.preventDefault();
        if (rowRefs.current.length > 0) {
          rowRefs.current[0]?.focus();
          setFocusedRowIndex(0);
        }
      }
    };

    input.addEventListener("keydown", handleKeyDown);
    return () => input.removeEventListener("keydown", handleKeyDown);
  }, [activeSearchInputRef, hasKeyboardNav, data]);

  React.useEffect(() => {
    setFocusedRowIndex(null);
  }, [pageIndex]);

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

  const handleRowKeyDown = (
    e: React.KeyboardEvent<HTMLTableRowElement>,
    index: number,
    rowData: TData,
  ) => {
    if (e.key === "Enter") {
      e.preventDefault();
      if (onRowSelect) {
        onRowSelect(rowData);
      } else if (onEditRow) {
        onEditRow(rowData);
      }
    } else if ((e.altKey && e.key === "e") || (e.altKey && e.key === "E")) {
      e.preventDefault();
      if (onEditRow) {
        onEditRow(rowData);
      }
    } else if (e.key === "Delete" || e.key === "Backspace") {
      e.preventDefault();
      if (onDeleteRow) {
        onDeleteRow(rowData);
      }
    } else if (e.key === "ArrowDown") {
      e.preventDefault();
      const next = index + 1;
      if (next < rows.length) {
        rowRefs.current[next]?.focus();
        setFocusedRowIndex(next);
      }
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      if (index === 0) {
        activeSearchInputRef?.current?.focus();
        setFocusedRowIndex(null);
      } else {
        const prev = index - 1;
        rowRefs.current[prev]?.focus();
        setFocusedRowIndex(prev);
      }
    }
  };

  React.useEffect(() => {
    if (typeof document === "undefined") return;

    let dialogCount = document.querySelectorAll('[role="dialog"]').length;

    const observer = new MutationObserver(() => {
      const currentCount = document.querySelectorAll('[role="dialog"]').length;

      if (currentCount < dialogCount) {
        const input = activeSearchInputRef?.current;
        if (!input) return;

        const isTableInsideDialog = !!input.closest('[role="dialog"]');

        const restoreFocus = () => {
          if (focusedRowIndex !== null && rowRefs.current[focusedRowIndex]) {
            rowRefs.current[focusedRowIndex]?.focus();
          } else {
            input.focus();
            input.select();
            setFocusedRowIndex(null);
          }
        };

        if (currentCount === 0 && !isTableInsideDialog) {
          setTimeout(restoreFocus, 100);
        } else if (currentCount > 0 && isTableInsideDialog) {
          const dialogs = document.querySelectorAll('[role="dialog"]');
          const topDialog = dialogs[dialogs.length - 1];
          if (input.closest('[role="dialog"]') === topDialog) {
            setTimeout(restoreFocus, 100);
          }
        }
      }
      dialogCount = currentCount;
    });

    observer.observe(document.body, { childList: true, subtree: true });
    return () => observer.disconnect();
  }, [activeSearchInputRef, focusedRowIndex]);

  return (
    <div
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
                  placeholder={searchPlaceholder}
                  value={globalFilter ?? ""}
                  onChange={(event) => onGlobalFilterChange(event.target.value)}
                  className="h-9"
                  onKeyDown={(e) => {
                    if (e.key === "ArrowDown" && hasKeyboardNav) {
                      e.preventDefault();

                      if (rows.length > 0) {
                        rowRefs.current[0]?.focus();
                        setFocusedRowIndex(0);
                      }
                    }
                  }}
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
                      className="text-foreground h-11 py-2 font-bold whitespace-nowrap"
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
                  key={row.id}
                  ref={(el) => {
                    rowRefs.current[index] = el;
                  }}
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
                  onKeyDown={(e) =>
                    hasKeyboardNav && handleRowKeyDown(e, index, row.original)
                  }
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
