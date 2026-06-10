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
import { categoriaSchema, Categoria, CategoriaFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { categoriasApi } from "@/api/catalogo";

interface CategoriasUpsertProps {
  open: boolean;
  editingItem: Categoria | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function CategoriasUpsert(props: CategoriasUpsertProps) {
  const { open, editingItem, onClose, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["categorias", "detail", editingItem?.id],
    queryFn: () => categoriasApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Categoria"
        loading={true}
      />
    );
  }

  return (
    <CategoriasUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function CategoriasUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly = false,
}: CategoriasUpsertProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
  useUpsertMutation({
    mutationFn: async (value: CategoriaFormValues) => {
      return editingItem
        ? await categoriasApi.update(editingItem.id, value)
        : await categoriasApi.create(value);
    },
    queryKey: [["categorias"], ["produtos"]],
    onSuccessCallback: onSuccess,
    onClose: onClose,
  });

  const form = useForm({
    defaultValues: {
      categoria: editingItem?.categoria ?? "",
      descricao: editingItem?.descricao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as CategoriaFormValues,
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
      title={editingItem ? "Editar Categoria" : "Nova Categoria"}
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
                form="upsert-categorias"
                disabled={!canSubmit || isSubmitting}
              >
                {isSubmitting ? (
                  "Salvando..."
                ) : (
                  <span className="flex items-center gap-2">
                    Salvar <KbdGroup><Kbd>Alt</Kbd><Kbd>Enter</Kbd></KbdGroup>
                  </span>
                )}
              </Button>
            )}
          </form.Subscribe>
        </>
      }
    >
      <form
        id="upsert-categorias"
        className="flex flex-col gap-4"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <FieldGroup className="gap-4">
          <div className="flex flex-wrap gap-4 items-start w-full">
            {editingItem && (
              <div className="w-fit">
                <div className="flex flex-col gap-1.5">
                  <FieldLabel>Código</FieldLabel>
                  <Input
                    value={editingItem.id}
                    disabled
                    className="h-8 text-xs font-mono"
                    inputSize="small"
                  />
                </div>
              </div>
            )}
            <div className="flex-1 min-w-48">
              <form.Field
                name="categoria"
                validators={{ onChange: categoriaSchema.shape.categoria }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Categoria"
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
            validators={{ onChange: categoriaSchema.shape.descricao }}
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
