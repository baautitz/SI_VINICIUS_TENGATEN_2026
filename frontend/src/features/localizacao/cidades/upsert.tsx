"use client";

import React from "react";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { Button } from "@/ui/primitives";
import { FieldGroup, FieldLabel } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { EstadoInput } from "@/components/entity-inputs/estado-input";
import { useForm, useStore } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { cidadeSchema, Cidade, CidadeFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { cidadesApi } from "@/api/localizacao";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface CidadesUpsertProps {
  editingItem: Cidade | null;
  readOnly?: boolean;
}

export function CidadesUpsert(props: CidadesUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["cidades", "detail", editingItem?.id],
    queryFn: () => cidadesApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <div className="flex min-h-48 items-center justify-center"><Spinner className="size-6" /></div>
    );
  }

  return (
    <CidadesUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function CidadesUpsertForm({
  editingItem,
  readOnly = false,
}: CidadesUpsertProps) {
  const activeWindow = useWindow<true>();
  const { mutation, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: CidadeFormValues) => {
        return editingItem
          ? await cidadesApi.update(editingItem.id, value)
          : await cidadesApi.create(value);
      },
      queryKey: ["cidades"],
      onSuccessCallback: () => activeWindow.resolve(true),
    });

  const form = useForm({
    defaultValues: {
      cidade: editingItem?.cidade ?? "",
      ddd: editingItem?.ddd ?? "",
      estadoId: editingItem?.estado?.id ?? null,
    } as CidadeFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        ddd: value.ddd || "",
        estadoId: value.estadoId || null,
      };
      try {
        await mutation.mutateAsync(payload as CidadeFormValues);
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
      id: "cidades.save",
      hotkey: "Alt+Enter" as const,
      label: "Salvar cidade",
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
          {readOnly ? "Fechar" : "Cancelar"} <Kbd>Esc</Kbd>
        </Button>
          {!readOnly && (<form.Subscribe
            selector={(state) => [state.canSubmit, state.isSubmitting]}
          >
            {([canSubmit, isSubmitting]) => (
              <Button
                type="submit"
                form="upsert-cidades"
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
          </form.Subscribe>)}
      </div>
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
                name="cidade"
                validators={{ onChange: cidadeSchema.shape.cidade }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Cidade"
                    inputSize="full"
                    getFieldError={getFieldError}
                    disabled={readOnly}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="ddd"
            validators={{ onChange: cidadeSchema.shape.ddd }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="DDD"
                inputSize="small"
                type="text"
                inputMode="numeric"
                disabled={readOnly}
                onChangeOverride={(val) => val.replace(/\D/g, "")}
                getFieldError={getFieldError}
              />
            )}
          </form.Field>

          <form.Field
            name="estadoId"
            validators={{
              onChange: ({ value }) => {
                const res = cidadeSchema.shape.estadoId.safeParse(value);
                return res.success ? undefined : res.error.errors[0]?.message;
              },
            }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors);
              return (
                <EstadoInput
                  name={field.name}
                  error={error}
                  initialItem={editingItem?.estado}
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
