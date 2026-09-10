"use client";

import { Kbd, KbdGroup } from "@/ui/primitives";
import React from "react";
import { Button } from "@/ui/primitives";
import { FieldGroup, FieldLabel } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { Alert, AlertDescription } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { useForm, useStore } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { paisSchema, Pais } from "./types";
import { useQuery } from "@tanstack/react-query";
import { paisesApi } from "@/api/localizacao";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface PaisesUpsertProps {
  editingItem: Pais | null;
  readOnly?: boolean;
}

export function PaisesUpsert(props: PaisesUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["paises", "detail", editingItem?.id],
    queryFn: () => paisesApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <div className="flex min-h-48 items-center justify-center"><Spinner className="size-6" /></div>
    );
  }

  return (
    <PaisesUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function PaisesUpsertForm({
  editingItem,
  readOnly = false,
}: PaisesUpsertProps) {
  const activeWindow = useWindow<true>();
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: {
        pais: string;
        codigoIsoPais: string;
        ddi: string;
        codigoIsoMoeda: string;
        simboloMoeda: string;
      }) => {
        return editingItem
          ? await paisesApi.update(editingItem.id, value)
          : await paisesApi.create(value);
      },
      queryKey: ["paises"],
      onSuccessCallback: () => activeWindow.resolve(true),
    });

  const form = useForm({
    defaultValues: {
      pais: editingItem?.pais ?? "",
      codigoIsoPais: editingItem?.codigoIsoPais ?? "",
      ddi: editingItem?.ddi ?? "",
      codigoIsoMoeda: editingItem?.codigoIsoMoeda ?? "",
      simboloMoeda: editingItem?.simboloMoeda ?? "",
    },
    onSubmit: async ({ value }) => {
      resetErrors();
      await mutation.mutateAsync(value);
    },
  });

  const isDirty = useStore(form.store, (state) => state.isDirty);
  React.useEffect(() => {
    activeWindow.setDirty(isDirty);
    return () => activeWindow.setDirty(false);
  }, [activeWindow, isDirty]);
  useWindowCommands([
    {
      id: "paises.save",
      hotkey: "Alt+Enter" as const,
      label: "Salvar país",
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
                form="upsert-paises"
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
        id="upsert-paises"
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
                name="pais"
                validators={{ onChange: paisSchema.shape.pais }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="País"
                    inputSize="full"
                    getFieldError={getFieldError}
                    disabled={readOnly}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="codigoIsoPais"
            validators={{ onChange: paisSchema.shape.codigoIsoPais }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Código ISO do país"
                inputSize="small"
                getFieldError={getFieldError}
                disabled={readOnly}
                maxLength={3}
                onChangeOverride={(val) => val.toUpperCase()}
              />
            )}
          </form.Field>

          <form.Field
            name="ddi"
            validators={{ onChange: paisSchema.shape.ddi }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="DDI"
                inputSize="small"
                getFieldError={getFieldError}
                disabled={readOnly}
              />
            )}
          </form.Field>

          <form.Field
            name="codigoIsoMoeda"
            validators={{ onChange: paisSchema.shape.codigoIsoMoeda }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Código ISO da moeda"
                inputSize="medium"
                getFieldError={getFieldError}
                disabled={readOnly}
              />
            )}
          </form.Field>

          <form.Field
            name="simboloMoeda"
            validators={{ onChange: paisSchema.shape.simboloMoeda }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Símbolo da Moeda"
                inputSize="small"
                getFieldError={getFieldError}
                disabled={readOnly}
              />
            )}
          </form.Field>
        </FieldGroup>

        {globalError && (
          <Alert variant="destructive">
            <AlertDescription>{globalError}</AlertDescription>
          </Alert>
        )}
      </form>
    </div>
  );
}
