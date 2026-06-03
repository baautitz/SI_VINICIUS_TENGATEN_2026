"use client";

import React from "react";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { Checkbox } from "@/components/ui/checkbox";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { marcaSchema, Marca, MarcaFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { marcasApi } from "@/api/catalogo";

interface MarcasUpsertProps {
  open: boolean;
  editingItem: Marca | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function MarcasUpsert(props: MarcasUpsertProps) {
  const { open, editingItem, onClose } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["marcas", "detail", editingItem?.id],
    queryFn: () => marcasApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Marca"
        loading={true}
      />
    );
  }

  return (
    <MarcasUpsertForm
      {...props}
      editingItem={isEditMode ? fullItem ?? null : null}
    />
  );
}

function MarcasUpsertForm({ open, editingItem, onClose, onSuccess }: MarcasUpsertProps) {
  const { mutation, globalError, getFieldError, resetErrors } = useUpsertMutation({
    mutationFn: async (value: MarcaFormValues) => {
      return editingItem
        ? await marcasApi.update(editingItem.id, value)
        : await marcasApi.create(value);
    },
    queryKey: ["marcas"],
    onSuccessCallback: onSuccess,
    onClose: onClose,
  });

  const form = useForm({
    defaultValues: {
      marca: editingItem?.marca ?? "",
      descricao: editingItem?.descricao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as MarcaFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      mutation.mutate(value);
    },
  });

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Marca" : "Nova Marca"}
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
              <Button type="submit" form="upsert-marcas" disabled={!canSubmit || isSubmitting}>
                {isSubmitting ? "Salvando..." : "Salvar"}
              </Button>
            )}
          </form.Subscribe>
        </>
      }
    >
      <form
        id="upsert-marcas"
        className="flex flex-col gap-4"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <FieldGroup className="gap-4">
          <form.Field
            name="marca"
            validators={{ onChange: marcaSchema.shape.marca }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Marca"
                inputSize="medium"
                getFieldError={getFieldError}
                maxLength={100}
              />
            )}
          </form.Field>

          <form.Field
            name="descricao"
            validators={{ onChange: marcaSchema.shape.descricao }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Descrição"
                inputSize="full"
                getFieldError={getFieldError}
                maxLength={255}
              />
            )}
          </form.Field>

          {editingItem && (
            <form.Field name="ativo">
              {(field) => {
                const error = getFieldError(field.name, field.state.meta.errors);
                return (
                  <Field orientation="horizontal" data-invalid={!!error}>
                    <Checkbox
                      id={field.name}
                      name={field.name}
                      checked={field.state.value}
                      onCheckedChange={(checked) => field.handleChange(!!checked)}
                    />
                    <FieldLabel htmlFor={field.name}>Ativo</FieldLabel>
                  </Field>
                );
              }}
            </form.Field>
          )}
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
