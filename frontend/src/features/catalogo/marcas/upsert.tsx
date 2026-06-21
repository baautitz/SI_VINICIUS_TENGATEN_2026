"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import React from "react";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
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
  readOnly?: boolean;
}

export function MarcasUpsert(props: MarcasUpsertProps) {
  const { open, editingItem, onClose, readOnly = false } = props;
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
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function MarcasUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: MarcasUpsertProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: MarcaFormValues) => {
        return editingItem
          ? await marcasApi.update(editingItem.id, value)
          : await marcasApi.create(value);
      },
      queryKey: [["marcas"], ["produtos"]],
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
              Cancelar <Kbd>Esc</Kbd>
            </Button>
          </DialogClose>
          <form.Subscribe
            selector={(state) => [state.canSubmit, state.isSubmitting]}
          >
            {([canSubmit, isSubmitting]) => (
              <Button
                type="submit"
                form="upsert-marcas"
                disabled={!canSubmit || isSubmitting}
              >
                {isSubmitting ? (
                  "Salvando..."
                ) : (
                  <span className="flex items-center gap-2">
                    Salvar{" "}
                    <KbdGroup>
                      <Kbd>Alt</Kbd>
                      <Kbd>Enter</Kbd>
                    </KbdGroup>
                  </span>
                )}
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
          <div className="flex w-full flex-wrap items-start gap-4">
            {editingItem && (
              <div className="w-fit">
                <div className="flex flex-col gap-2">
                  <FieldLabel>Código</FieldLabel>
                  <Input
                    value={editingItem.id}
                    disabled
                    className="h-8 text-xs"
                    inputSize="small"
                  />
                </div>
              </div>
            )}
            <div className="min-w-48 flex-1">
              <form.Field
                name="marca"
                validators={{ onChange: marcaSchema.shape.marca }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Marca"
                    inputSize="full"
                    getFieldError={getFieldError}
                    maxLength={100}
                  />
                )}
              </form.Field>
            </div>
          </div>

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
                const error = getFieldError(
                  field.name,
                  field.state.meta.errors,
                );
                return (
                  <Field orientation="horizontal" data-invalid={!!error}>
                    <Checkbox
                      id={field.name}
                      name={field.name}
                      checked={field.state.value}
                      onCheckedChange={(checked) =>
                        field.handleChange(!!checked)
                      }
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
