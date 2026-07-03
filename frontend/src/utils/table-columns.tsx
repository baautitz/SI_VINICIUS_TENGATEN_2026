import React from "react";
import { ColumnDef } from "@tanstack/react-table";
import { Checkbox } from "@/components/ui/checkbox";
import { Button } from "@/components/ui/button";
import { Check, Pencil, Trash2 } from "lucide-react";

export function getSelectColumn<T>(): ColumnDef<T> {
  return {
    id: "select",
    header: ({ table }) => (
      <Checkbox
        checked={
          table.getIsAllPageRowsSelected() ||
          (table.getIsSomePageRowsSelected() && "indeterminate")
        }
        onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
        aria-label="Selecionar tudo"
      />
    ),
    cell: ({ row }) => (
      <Checkbox
        checked={row.getIsSelected()}
        onCheckedChange={(value) => row.toggleSelected(!!value)}
        aria-label="Selecionar linha"
      />
    ),
    enableHiding: false,
    size: 50,
  };
}

interface ActionColumnOptions<T> {
  onEdit?: (item: T) => void;
  onDelete?: (item: T) => void;
  selectionMode?: boolean;
  onSelect?: (item: T) => void;
}

export function getActionsColumn<T>({
  onEdit,
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
              onClick={() => onEdit(item)}
            >
              <Pencil className="size-4" />
            </Button>
          )}
          {onDelete && (
            <Button
              size="icon-sm"
              variant="destructive"
              onClick={() => onDelete(item)}
            >
              <Trash2 className="size-4" />
            </Button>
          )}
          {selectionMode && onSelect && (
            <Button variant="secondary" onClick={() => onSelect(item)}>
              <Check className="mr-2 size-4" />
              Selecionar
            </Button>
          )}
        </div>
      );
    },
  };
}
