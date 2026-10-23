import React from "react";
import { ColumnDef } from "@tanstack/react-table";
import { Button } from "@/ui/primitives";
import { Ban, Check, Eye, Pencil, Trash2, Undo2 } from "lucide-react";
import { fireAndForget } from "@/lib/utils";

interface ActionColumnOptions<T> {
  onEdit?: (item: T) => void;
  onView?: (item: T) => void;
  onDelete?: (item: T) => void;
  selectionMode?: boolean;
  onSelect?: (item: T) => void;
  /** Ações adicionais renderizadas antes de Editar/Excluir. */
  extra?: (item: T) => React.ReactNode;
}

const ROW_ACTIONS = {
  baixar: { icon: Check, variant: "ghost" },
  estornar: { icon: Undo2, variant: "outline" },
  cancelar: { icon: Ban, variant: "destructive" },
} as const;

interface RowActionProps {
  kind: keyof typeof ROW_ACTIONS;
  /** Rótulo acessível e tooltip, ex.: "Estornar pagamento". */
  label: string;
  onClick: () => void | Promise<void>;
}

/** Botão de ação de negócio da linha: ícone e variant fixos por tipo (DESIGN.md, Action grammar). */
export function RowAction({ kind, label, onClick }: RowActionProps) {
  const { icon: Icon, variant } = ROW_ACTIONS[kind];
  return (
    <Button size="icon-sm" variant={variant} aria-label={label} title={label} onClick={() => fireAndForget(onClick)}>
      <Icon className="size-4" />
    </Button>
  );
}

export function getActionsColumn<T>({
  onEdit,
  onView,
  onDelete,
  selectionMode = false,
  onSelect,
  extra,
}: ActionColumnOptions<T>): ColumnDef<T> {
  return {
    id: "actions",
    header: () => <div className="px-4 text-right">Ações</div>,
    cell: ({ row }) => {
      const item = row.original;
      return (
        <div className="flex justify-end gap-2 px-4">
          {extra?.(item)}
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
              aria-label="Excluir"
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
