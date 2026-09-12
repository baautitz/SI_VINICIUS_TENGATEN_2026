"use client";

import React from "react";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { Button } from "@/ui/primitives";
import { FieldGroup, FieldLabel } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { PaisInput } from "@/components/entity-inputs/pais-input";
import { useForm, useStore } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { estadoSchema, Estado, EstadoFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { estadosApi } from "@/api/localizacao";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface EstadosUpsertProps {
  editingItem: Estado | null;
  readOnly?: boolean;
}

export function EstadosUpsert(props: EstadosUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["estados", "detail", editingItem?.id],
    queryFn: () => estadosApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <div className="flex min-h-48 items-center justify-center"><Spinner className="size-6" /></div>
    );
  }

  return (
    <EstadosUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function EstadosUpsertForm({
  editingItem,
  readOnly = false,
}: EstadosUpsertProps) {
  const activeWindow = useWindow<true>();
  const { mutation, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: EstadoFormValues) => {
        return editingItem
          ? await estadosApi.update(editingItem.id, value)
          : await estadosApi.create(value);
      },
      queryKey: ["estados"],
      onSuccessCallback: () => activeWindow.resolve(true),
    });

  const form = useForm({
    defaultValues: {
      estado: editingItem?.estado ?? "",
      uf: editingItem?.uf ?? "",
      paisId: editingItem?.pais?.id ?? null,
    } as EstadoFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        paisId: value.paisId || null,
      };
      try {
        await mutation.mutateAsync(payload as EstadoFormValues);
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
  useWindowCommands([
    {
      id: "estados.save",
      hotkey: "Alt+Enter" as const,
      label: "Salvar estado",
      enabled: !readOnly && !mutation.isPending,
      run: async (event) => {
        event.preventDefault();
        await form.handleSubmit();
      },
    },
  ]);

  return (
    <div className="flex flex-col gap-4">
      <div data-window-actions className="flex justify-end gap-2 border-b pb-4">
        <Button type="button" variant="outline" onClick={() => activeWindow.dismiss("cancel")}>
          Cancelar <Kbd>Esc</Kbd>
        </Button>
          <form.Subscribe
            selector={(state) => [state.canSubmit, state.isSubmitting]}
          >
            {([canSubmit, isSubmitting]) => (
              <Button
                type="submit"
                form="upsert-estados"
                disabled={readOnly || !canSubmit || isSubmitting}
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
      </div>
      <form
        id="upsert-estados"
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
                name="estado"
                validators={{ onChange: estadoSchema.shape.estado }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Estado"
                    inputSize="full"
                    getFieldError={getFieldError}
                    disabled={readOnly}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="uf"
            validators={{ onChange: estadoSchema.shape.uf }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="UF"
                inputSize="small"
                getFieldError={getFieldError}
                disabled={readOnly}
                maxLength={2}
                onChangeOverride={(val) => val.toUpperCase()}
              />
            )}
          </form.Field>

          <form.Field
            name="paisId"
            validators={{
              onChange: ({ value }) => {
                const res = estadoSchema.shape.paisId.safeParse(value);
                return res.success ? undefined : res.error.errors[0]?.message;
              },
            }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors);
              return (
                <PaisInput
                  name={field.name}
                  error={error}
                  initialItem={editingItem?.pais}
                  disabled={readOnly}
                  onSelectId={(id) =>
                    field.handleChange(id ? parseInt(String(id), 10) : null)
                  }
                />
              );
            }}
          </form.Field>
        </FieldGroup>

      </form>
    </div>
  );
}
