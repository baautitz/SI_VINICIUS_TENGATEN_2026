import React from "react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/ui/primitives";
import { Check, Eye, Pencil, Trash2 } from "lucide-react";
import { fireAndForget } from "@/lib/utils";

interface ActionColumnOptions<T> {
  onEdit?: (item: T) => void;
  onView?: (item: T) => void;
  onDelete?: (item: T) => void;
  selectionMode?: boolean;
  onSelect?: (item: T) => void;
}

export function getActionsColumn<T>({
  onEdit,
  onView,
  onDelete,
  selectionMode = false,
  onSelect,
}: ActionColumnOptions<T>): ColumnDef<T> {
  return {
    id: "actions",
    header: () => <div className="px-4 text-right">Ações</div>,
    cell: ({ row }) => {
      const item = row.original;
      return (
        <div className="flex justify-end gap-2 px-4">
          {onEdit && (
            <Button
              size="icon-sm"
              variant="outline"
              onClick={() => fireAndForget(() => onEdit(item))}
              aria-label="Editar"
            >
              <Pencil className="size-4" />
            </Button>
          )}
          {/* Editar ou visualizar, nunca os dois: a edição já abre o registro. */}
          {onView && !onEdit && (
            <Button
              size="icon-sm"
              variant="outline"
              onClick={() => fireAndForget(() => onView(item))}
              aria-label="Visualizar"
            >
              <Eye className="size-4" />
            </Button>
          )}
          {onDelete && (
            <Button
              size="icon-sm"
              variant="destructive"
              onClick={() => fireAndForget(() => onDelete(item))}
            >
              <Trash2 className="size-4" />
            </Button>
          )}
          {selectionMode && onSelect && (
            <Button variant="secondary" onClick={() => fireAndForget(() => onSelect(item))}>
              <Check className="mr-2 size-4" />
              Selecionar
            </Button>
          )}
        </div>
      );
    },
  };
}
