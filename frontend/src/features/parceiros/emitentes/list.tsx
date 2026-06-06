"use client";

import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useHotkeys } from "react-hotkeys-hook";
import { FeatureHeader } from "@/components/ui/feature-header";
import { UserCircle } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/components/ui/data-table";

import { FeatureLayout } from "@/components/ui/feature-layout";
import { Badge } from "@/components/ui/badge";
import { EmitenteDto } from "./types";
import { TipoPessoa } from "@/api/types";

import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function EmitentesList({
  items: emitentes,
  loading,
  searchTerm,
  page,
  totalPages,
  totalItems,
  selectionMode = false,
  onSearchChange,
  onAdd,
  onEdit,
  onDelete,
  onSelect,
  onPageChange,
  rowSelection,
  onRowSelectionChange,
  selectAllAcrossPages,
  onSelectAllAcrossPagesChange,
}: FeatureListProps<EmitenteDto>) {
  useHotkeys("alt+n", (e) => {
    e.preventDefault();
    if (!selectionMode && document.querySelector('[role="dialog"]')) return;
    onAdd();
  }, { enableOnFormTags: true }, [selectionMode, onAdd]);

  const columns: ColumnDef<EmitenteDto>[] = [
    getSelectColumn<EmitenteDto>(),
    {
      accessorKey: "id",
      header: "ID",
      size: 80,
      cell: ({ row }) => (
        <span className="font-semibold">{row.getValue("id")}</span>
      ),
    },
    {
      accessorKey: "tipoPessoa",
      header: "Tipo",
      size: 100,
      cell: ({ row }) => {
        const tipo = row.getValue("tipoPessoa") as TipoPessoa;
        return (
          <Badge variant={tipo === TipoPessoa.FISICA ? "secondary" : "outline"}>
            {tipo === TipoPessoa.FISICA ? "Física" : "Jurídica"}
          </Badge>
        );
      },
    },
    {
      accessorKey: "nomeRazaoSocial",
      header: "Nome / Razão Social",
      cell: ({ row }) => {
        const item = row.original;
        return (
          <div className="flex flex-col">
            <span className="font-medium">{item.nomeRazaoSocial}</span>
            {item.apelidoNomeFantasia && (
              <span className="text-xs text-muted-foreground">
                {item.apelidoNomeFantasia}
              </span>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: "cpfCnpj",
      header: "CPF / CNPJ",
      cell: ({ row }) => (
        <span className="text-muted-foreground">{row.getValue("cpfCnpj")}</span>
      ),
    },
    getActionsColumn<EmitenteDto>({ onEdit, onDelete, selectionMode, onSelect }),
  ];

  return (
    <FeatureLayout>
      <FeatureHeader
        title="Emitentes"
        icon={<UserCircle />}
        onAdd={onAdd}
        addButtonLabel="Novo Emitente"
      />

      <DataTable
        columns={columns}
        data={emitentes}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por nome ou CNPJ/CPF..."
        rowSelection={rowSelection}
        onRowSelectionChange={rowSelection ? onRowSelectionChange : undefined}
        selectAllAcrossPages={selectAllAcrossPages}
        onSelectAllAcrossPagesChange={onSelectAllAcrossPagesChange}
        getRowId={(row) => row.id.toString()}
        onRowSelect={selectionMode ? onSelect : undefined}
      />
    </FeatureLayout>
  );
}
