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
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";

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

  /** When provided, the table enters "selection mode": rows are keyboard-navigable
   *  (↑↓ arrows, Enter to select) and auto-focus on the first row when data loads. */
  onRowSelect?: (row: TData) => void;

  /** Ref forwarded to the search input so parents can focus it on open */
  searchInputRef?: React.RefObject<HTMLInputElement | null>;
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
  searchInputRef,
}: DataTableProps<TData, TValue>) {
  "use no memo";

  const isSelectionMode = !!onRowSelect;
  const [focusedRowIndex, setFocusedRowIndex] = React.useState<number | null>(
    null,
  );
  const rowRefs = React.useRef<(HTMLTableRowElement | null)[]>([]);
  const internalSearchInputRef = React.useRef<HTMLInputElement>(null);
  const activeSearchInputRef = searchInputRef || internalSearchInputRef;

  // Attach ArrowDown listener to the external search input if provided
  React.useEffect(() => {
    const input = activeSearchInputRef?.current;
    if (!input || !isSelectionMode) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "ArrowDown") {
        e.preventDefault();
        if (rowRefs.current.length > 0) {
          rowRefs.current[0]?.focus();
          setFocusedRowIndex(0);
        }
      } else if (e.key === "Enter") {
        // Optionally, if they press Enter in the search input and there's exactly 1 row,
        // we could select it. But the user just asked to move up/down.
      }
    };

    input.addEventListener("keydown", handleKeyDown);
    return () => input.removeEventListener("keydown", handleKeyDown);
  }, [activeSearchInputRef, isSelectionMode, data]);

  // Reset focus index when data changes (search/page)
  React.useEffect(() => {
    setFocusedRowIndex(null);
  }, [data, pageIndex]);

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
      onRowSelect?.(rowData);
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
        // Go back to search input
        activeSearchInputRef?.current?.focus();
        setFocusedRowIndex(null);
      } else {
        const prev = index - 1;
        rowRefs.current[prev]?.focus();
        setFocusedRowIndex(prev);
      }
    }
  };

  return (
    <div className="space-y-4 flex-1 min-h-0 flex flex-col">
      <div className="flex items-center justify-between gap-2 shrink-0">
        <div className="flex flex-1 items-center gap-2">
          {onGlobalFilterChange && (
            <div className="relative max-w-sm flex-1">
              <Search className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
              <Input
                ref={activeSearchInputRef}
                placeholder={searchPlaceholder}
                value={globalFilter ?? ""}
                onChange={(event) => onGlobalFilterChange(event.target.value)}
                className="pl-9 h-9"
                onKeyDown={(e) => {
                  if (e.key === "ArrowDown" && isSelectionMode) {
                    e.preventDefault();
                    if (rows.length > 0) {
                      rowRefs.current[0]?.focus();
                      setFocusedRowIndex(0);
                    }
                  }
                }}
              />
            </div>
          )}
          {actions}
        </div>
      </div>

      <div className="rounded-xl border bg-card relative overflow-hidden flex-1 min-h-0 flex flex-col">
        <Table>
          <TableHeader className="sticky top-0 bg-muted z-10">
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow
                key={headerGroup.id}
                className="hover:bg-transparent border-b"
              >
                {headerGroup.headers.map((header) => {
                  return (
                    <TableHead
                      key={header.id}
                      className="whitespace-nowrap h-11 py-2 font-bold text-foreground"
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
          <TableBody>
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
                  <div className="flex flex-col items-center justify-center gap-3 py-8 text-muted-foreground">
                    <Spinner className="size-6" />
                    <span className="font-medium animate-pulse">
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
                    isSelectionMode && focusedRowIndex === index
                      ? "true"
                      : undefined
                  }
                  tabIndex={isSelectionMode ? 0 : -1}
                  className={[
                    "group border-b last:border-0 transition-colors",
                    isSelectionMode ? "cursor-pointer focus:outline-none" : "",
                    isSelectionMode && focusedRowIndex === index
                      ? "bg-primary/5 relative z-10 outline-2 outline-primary -outline-offset-2"
                      : "",
                  ].join(" ")}
                  onFocus={() => isSelectionMode && setFocusedRowIndex(index)}
                  onKeyDown={(e) =>
                    isSelectionMode && handleRowKeyDown(e, index, row.original)
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
                  className="h-full text-center text-muted-foreground"
                >
                  Nenhum resultado encontrado.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between mt-auto">
        <div className="flex-1 text-sm text-muted-foreground font-medium">
          {isSelectionMode ? (
            <span className="text-xs text-muted-foreground">
              ↑↓ para navegar · Enter para selecionar
            </span>
          ) : (
            <>
              {selectAllAcrossPages
                ? totalItems
                : Object.keys(rowSelection).length}{" "}
              de {totalItems ?? table.getFilteredRowModel().rows.length}{" "}
              selecionado(s).
            </>
          )}
        </div>
        <div className="flex items-center space-x-6 lg:space-x-8">
          <div className="flex items-center space-x-2">
            <p className="text-sm font-semibold text-foreground/80">
              Página {pageIndex} de {pageCount || 1}
            </p>
          </div>
          <div className="flex items-center space-x-2">
            <Button
              variant="outline"
              className="hidden h-9 w-9 p-0 lg:flex rounded-lg"
              onClick={() => onPageChange(1)}
              disabled={pageIndex <= 1 || loading}
            >
              <span className="sr-only">Ir para primeira página</span>
              <ChevronsLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              className="h-9 w-9 p-0 rounded-lg"
              onClick={() => onPageChange(pageIndex - 1)}
              disabled={pageIndex <= 1 || loading}
            >
              <span className="sr-only">Página anterior</span>
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              className="h-9 w-9 p-0 rounded-lg"
              onClick={() => onPageChange(pageIndex + 1)}
              disabled={pageIndex >= pageCount || loading}
            >
              <span className="sr-only">Próxima página</span>
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              className="hidden h-9 w-9 p-0 lg:flex rounded-lg"
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
