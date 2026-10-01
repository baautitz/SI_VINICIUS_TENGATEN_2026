"use client";

import { Kbd, KbdGroup } from "@/ui/primitives";
import React from "react";
import { Button } from "@/ui/primitives";
import { FieldGroup, FieldLabel } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { CidadeInput } from "@/components/entity-inputs/cidade-input";
import { useForm, useStore } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { bairroSchema, Bairro, BairroFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { bairrosApi } from "@/api/localizacao";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface BairrosUpsertProps {
  editingItem: Bairro | null;
  readOnly?: boolean;
}

export function BairrosUpsert(props: BairrosUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["bairros", "detail", editingItem?.id],
    queryFn: () => bairrosApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <div className="flex min-h-48 items-center justify-center"><Spinner className="size-6" /></div>
    );
  }

  return (
    <BairrosUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function BairrosUpsertForm({
  editingItem,
  readOnly = false,
}: BairrosUpsertProps) {
  const activeWindow = useWindow<true>();
  const { mutation, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: BairroFormValues) => {
        return editingItem
          ? await bairrosApi.update(editingItem.id, value)
          : await bairrosApi.create(value);
      },
      queryKey: ["bairros"],
      onSuccessCallback: () => activeWindow.resolve(true),
    });

  const form = useForm({
    defaultValues: {
      bairro: editingItem?.bairro ?? "",
      cidadeId: editingItem?.cidade?.id ?? null,
    } as BairroFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        cidadeId: value.cidadeId || null,
      };
      try {
        await mutation.mutateAsync(payload as BairroFormValues);
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
      id: "bairros.save",
      hotkey: "Alt+Enter" as const,
      label: "Salvar bairro",
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
                form="upsert-bairros"
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
        id="upsert-bairros"
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
                name="bairro"
                validators={{ onChange: bairroSchema.shape.bairro }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Bairro"
                    inputSize="full"
                    getFieldError={getFieldError}
                    disabled={readOnly}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="cidadeId"
            validators={{
              onChange: ({ value }) => {
                const res = bairroSchema.shape.cidadeId.safeParse(value);
                return res.success ? undefined : res.error.errors[0]?.message;
              },
            }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors);
              return (
                <CidadeInput
                  name={field.name}
                  error={error}
                  initialItem={editingItem?.cidade}
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
