"use client";

import { getSelectColumn, getActionsColumn } from "@/utils/table-columns";
import { useHotkeys } from "react-hotkeys-hook";
import { FeatureHeader } from "@/components/ui/feature-header";
import { Truck } from "lucide-react"
import { ColumnDef } from "@tanstack/react-table";

import { DataTable } from "@/components/ui/data-table";

import { FeatureLayout } from "@/components/ui/feature-layout";
import { Badge } from "@/components/ui/badge";
import { FornecedorDto } from "./types";
import { TipoPessoa } from "@/api/types";

import { FeatureListProps } from "@/hooks/use-feature-orchestrator";

export function FornecedoresList({
  items: fornecedores,
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
}: FeatureListProps<FornecedorDto>) {
  useHotkeys("alt+n", (e) => {
    e.preventDefault();
    if (!selectionMode && document.querySelector('[role="dialog"]')) return;
    onAdd();
  }, { enableOnFormTags: true }, [selectionMode, onAdd]);

  const columns: ColumnDef<FornecedorDto>[] = [
    getSelectColumn<FornecedorDto>(),
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
      accessorKey: "nomeRazaosocial",
      header: "Nome / Razão Social",
      cell: ({ row }) => {
        const item = row.original;
        return (
          <div className="flex flex-col">
            <span className="font-medium">{item.nomeRazaosocial}</span>
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
    getActionsColumn<FornecedorDto>({ onEdit, onDelete, selectionMode, onSelect }),
  ];

  return (
    <FeatureLayout>
      <FeatureHeader
        title="Fornecedores"
        icon={<Truck />}
        onAdd={onAdd}
        addButtonLabel="Novo Fornecedor"
      />

      <DataTable
        columns={columns}
        data={fornecedores}
        loading={loading}
        pageCount={totalPages}
        pageIndex={page}
        onPageChange={onPageChange}
        totalItems={totalItems}
        globalFilter={searchTerm}
        onGlobalFilterChange={onSearchChange}
        searchPlaceholder="Pesquisar por nome ou CNPJ/CPF..."
        rowSelection={rowSelection}
        onRowSelectionChange={onRowSelectionChange}
        selectAllAcrossPages={selectAllAcrossPages}
        onSelectAllAcrossPagesChange={onSelectAllAcrossPagesChange}
        getRowId={(row) => row.id.toString()}
        onRowSelect={selectionMode ? onSelect : undefined}
      />
    </FeatureLayout>
  );
}
