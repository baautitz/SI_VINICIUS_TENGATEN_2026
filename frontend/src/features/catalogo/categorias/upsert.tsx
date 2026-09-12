"use client";

import { Kbd, KbdGroup } from "@/ui/primitives";
import React from "react";
import { Button } from "@/ui/primitives";
import { Field, FieldGroup, FieldLabel } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { Checkbox } from "@/ui/primitives";
import { useForm, useStore } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { categoriaSchema, Categoria, CategoriaFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { categoriasApi } from "@/api/catalogo";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface CategoriasUpsertProps {
  editingItem: Categoria | null;
  readOnly?: boolean;
}

export function CategoriasUpsert(props: CategoriasUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["categorias", "detail", editingItem?.id],
    queryFn: () => categoriasApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <div className="flex min-h-48 items-center justify-center">
        <Spinner className="size-6" />
      </div>
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
  editingItem,
  readOnly = false,
}: CategoriasUpsertProps) {
  const activeWindow = useWindow<true>();
  const { mutation, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: CategoriaFormValues) => {
        return editingItem
          ? await categoriasApi.update(editingItem.id, value)
          : await categoriasApi.create(value);
      },
      queryKey: [["categorias"], ["produtos"]],
      onSuccessCallback: () => activeWindow.resolve(true),
    });

  const form = useForm({
    defaultValues: {
      categoria: editingItem?.categoria ?? "",
      descricao: editingItem?.descricao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as CategoriaFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      try {
        await mutation.mutateAsync(value);
      } catch {
        // O hook central já apresenta o erro operacional em um toast.
      }
    },
  });

  const isDirty = useStore(form.store, (state) => state.isDirty);

  React.useEffect(() => {
    activeWindow.setDirty(isDirty);
    return () => activeWindow.setDirty(false);
  }, [activeWindow, isDirty]);

  const commands = React.useMemo(
    () => [
      {
        id: "categorias.save",
        hotkey: "Alt+Enter" as const,
        label: "Salvar categoria",
        enabled: !readOnly && !mutation.isPending,
        run: async (event: KeyboardEvent) => {
          event.preventDefault();
          await form.handleSubmit();
        },
      },
    ],
    [form, mutation.isPending, readOnly],
  );

  useWindowCommands(commands);

  return (
    <div className="flex flex-col gap-4">
      <div data-window-actions className="flex justify-end gap-2 border-b pb-4">
        <Button
          type="button"
          variant="outline"
          onClick={() => activeWindow.dismiss("cancel")}
        >
          Cancelar <Kbd>Esc</Kbd>
        </Button>
        <form.Subscribe
          selector={(state) => [state.canSubmit, state.isSubmitting]}
        >
          {([canSubmit, isSubmitting]) => (
            <Button
              type="submit"
              form="upsert-categorias"
              disabled={readOnly || !canSubmit || isSubmitting}
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
      </div>
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
                name="categoria"
                validators={{ onChange: categoriaSchema.shape.categoria }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Categoria"
                    inputSize="full"
                    getFieldError={getFieldError}
                    disabled={readOnly}
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
                disabled={readOnly}
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
                      disabled={readOnly}
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

      </form>
    </div>
  );
}
