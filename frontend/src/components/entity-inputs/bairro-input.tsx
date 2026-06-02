"use client";

import { EntityInput } from "@/components/ui/entity-input";
import {
  BairrosFeature,
  Bairro,
  BairroResumo,
  formatBairroLabel,
} from "@/features/localizacao/bairros";
import { bairrosApi } from "@/api/localizacao";

interface BairroInputProps {
  name: string;
  label?: string;
  error?: string;
  initialItem?: Bairro | BairroResumo | null;
  onSelectId: (id: number | null) => void;
}

export function BairroInput({
  name,
  label = "Bairro",
  error,
  initialItem,
  onSelectId,
}: BairroInputProps) {
  return (
    <EntityInput<Bairro, BairroResumo>
      name={name}
      label={label}
      error={error}
      initialItem={initialItem}
      onSelectId={onSelectId}
      modalTitle="Selecionar Bairro"
      getDisplayLabel={formatBairroLabel}
      getSearchTerm={(item) => item.bairro}
      getId={(item) => item.id}
      fetchById={async (id) => {
        try {
          return await bairrosApi.getById(id);
        } catch {
          return null;
        }
      }}
      fetchList={async (term) => {
        try {
          return await bairrosApi.list(term.trim() || undefined, 1, 10);
        } catch {
          return null;
        }
      }}
      renderFeature={(props) => <BairrosFeature {...props} />}
    />
  );
}
