"use client";

import { Kbd, KbdGroup } from "@/ui/primitives";
import React from "react";
import { Button } from "@/ui/primitives";
import { Field, FieldGroup, FieldLabel } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { Checkbox } from "@/ui/primitives";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import {
  metodoPagamentoSchema,
  MetodoPagamento,
  MetodoPagamentoFormValues,
} from "./types";
import { metodosApi } from "@/api/financeiro";
import { useQuery } from "@tanstack/react-query";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface MetodosUpsertProps {
  editingItem: MetodoPagamento | null;
  readOnly?: boolean;
}

export function MetodosUpsert(props: MetodosUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["metodosPagamento", "detail", editingItem?.codigo],
    queryFn: () => metodosApi.getById(editingItem!.codigo),
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
    <MetodosUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function MetodosUpsertForm({
  editingItem,
  readOnly = false,
}: MetodosUpsertProps) {
  const activeWindow = useWindow<true>();
  const { mutation, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: MetodoPagamentoFormValues) => {
        return editingItem
          ? await metodosApi.update(editingItem.codigo, value)
          : await metodosApi.create(value);
      },
      queryKey: ["metodosPagamento"],
      onSuccessCallback: () => activeWindow.resolve(true),
    });

  const submitForm = async ({
    value,
  }: {
    value: MetodoPagamentoFormValues;
  }) => {
    resetErrors();
    try {
      await mutation.mutateAsync(value);
    } catch {
      // O hook central já apresenta o erro operacional em um toast.
    }
  };

  const form = useForm({
    defaultValues: {
      codigo: editingItem?.codigo ?? "",
      descricao: editingItem?.descricao ?? "",
      ativo: editingItem?.ativo ?? true,
      permiteTroco: editingItem?.permiteTroco ?? false,
    } as MetodoPagamentoFormValues,
    onSubmit: submitForm,
  });

  const registerDirty = React.useCallback(() => {
    activeWindow.setDirtyCheck(() => form.state.isDirty);
    return () => activeWindow.setDirtyCheck(null);
  }, [activeWindow, form]);

  const commands = React.useMemo(
    () => [
      {
        id: "metodosPagamento.save",
        hotkey: "Alt+Enter" as const,
        label: "Salvar método de pagamento",
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

  const cancelForm = async () => {
    activeWindow.dismiss("cancel");
  };

  const submitFormEvent = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    event.stopPropagation();
    await form.handleSubmit();
  };

  return (
    <div className="flex flex-col gap-4">
      <div data-window-actions className="flex justify-end gap-2 border-b pb-4">
        <Button
          type="button"
          variant="outline"
          onClick={cancelForm}
        >
          {readOnly ? "Fechar" : "Cancelar"} <Kbd>Esc</Kbd>
        </Button>
        {!readOnly && (
          <form.Subscribe
            selector={(state) => [state.canSubmit, state.isSubmitting]}
          >
            {([canSubmit, isSubmitting]) => (
              <Button
                type="submit"
                form="upsert-metodos"
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
        )}
      </div>
      <form
        id="upsert-metodos"
        ref={registerDirty}
        className="flex flex-col gap-4"
        onSubmit={submitFormEvent}
      >
        <FieldGroup className="gap-4">
          <form.Field
            name="codigo"
            validators={{ onChange: metodoPagamentoSchema.shape.codigo }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Código"
                placeholder={
                  editingItem
                    ? undefined
                    : "Deixe em branco para auto-gerar"
                }
                inputSize="full"
                getFieldError={getFieldError}
                maxLength={10}
                disabled={!!editingItem || readOnly}
              />
            )}
          </form.Field>

          <form.Field
            name="descricao"
            validators={{ onChange: metodoPagamentoSchema.shape.descricao }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Descrição"
                inputSize="full"
                getFieldError={getFieldError}
                maxLength={100}
                disabled={readOnly}
              />
            )}
          </form.Field>

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
                    disabled={readOnly}
                  />
                  <FieldLabel htmlFor={field.name}>Ativo</FieldLabel>
                </Field>
              );
            }}
          </form.Field>

          <form.Field name="permiteTroco">
            {(field) => (
              <Field orientation="horizontal">
                <Checkbox
                  id={field.name}
                  name={field.name}
                  checked={field.state.value}
                  onCheckedChange={(checked) => field.handleChange(!!checked)}
                  disabled={readOnly}
                />
                <FieldLabel htmlFor={field.name}>Permite troco</FieldLabel>
              </Field>
            )}
          </form.Field>
        </FieldGroup>

      </form>
    </div>
  );
}
