"use client";

import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { FieldGroup } from "@/components/ui/field";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { EstadoInput } from "@/components/entity-inputs/estado-input";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { cidadeSchema, CidadeResumo, Cidade, CidadeFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { cidadesApi } from "@/api/localizacao";

interface CidadesUpsertProps {
  open: boolean;
  editingItem: CidadeResumo | null;
  onClose: () => void;
  onSuccess: () => void;
}

interface CidadesUpsertFormProps {
  open: boolean;
  editingItem: Cidade | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function CidadesUpsert(props: CidadesUpsertProps) {
  const { open, editingItem, onClose, onSuccess } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["cidades", "detail", editingItem?.id],
    queryFn: () => cidadesApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Cidade"
        loading={true}
      />
    );
  }

  return (
    <CidadesUpsertForm
      open={open}
      editingItem={isEditMode ? fullItem ?? null : null}
      onClose={onClose}
      onSuccess={onSuccess}
    />
  );
}

function CidadesUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: CidadesUpsertFormProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: CidadeFormValues) => {
        return editingItem
          ? await cidadesApi.update(editingItem.id, value)
          : await cidadesApi.create(value);
      },
      queryKey: ["cidades"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      cidade: editingItem?.cidade ?? "",
      ddd: editingItem?.ddd ?? null,
      estadoId: editingItem?.estado?.id ?? null,
    } as CidadeFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        ddd: value.ddd || null,
        estadoId: value.estadoId || null,
      };
      mutation.mutate(payload as CidadeFormValues);
    },
  });

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Cidade" : "Nova Cidade"}
      footer={
        <>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancelar
            </Button>
          </DialogClose>
          <form.Subscribe
            selector={(state) => [state.canSubmit, state.isSubmitting]}
          >
            {([canSubmit, isSubmitting]) => (
              <Button
                type="submit"
                form="upsert-cidades"
                disabled={!canSubmit || isSubmitting}
              >
                {isSubmitting ? "Salvando..." : "Salvar"}
              </Button>
            )}
          </form.Subscribe>
        </>
      }
    >
      <form
        id="upsert-cidades"
        className="flex flex-col gap-4"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <FieldGroup className="gap-4">
          <form.Field
            name="cidade"
            validators={{ onChange: cidadeSchema.shape.cidade }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Cidade"
                getFieldError={getFieldError}
              />
            )}
          </form.Field>

          <form.Field
            name="ddd"
            validators={{ onChange: cidadeSchema.shape.ddd }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="DDD"
                inputSize="small"
                type="number"
                getFieldError={getFieldError}
              />
            )}
          </form.Field>

          <form.Field
            name="estadoId"
            validators={{ onChange: cidadeSchema.shape.estadoId }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors);
              return (
                <EstadoInput
                  name={field.name}
                  error={error}
                  initialItem={editingItem?.estado}
                  onSelectId={(id) => field.handleChange(id)}
                />
              );
            }}
          </form.Field>
        </FieldGroup>

        {globalError && (
          <Alert variant="destructive">
            <AlertDescription>{globalError}</AlertDescription>
          </Alert>
        )}
      </form>
    </UpsertDialog>
  );
}
