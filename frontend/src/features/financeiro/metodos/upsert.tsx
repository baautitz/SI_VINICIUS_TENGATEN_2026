"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
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
import { metodoPagamentoSchema, MetodoPagamento, MetodoPagamentoFormValues } from "./types";
import { metodosApi } from "@/api/financeiro";

interface MetodosUpsertProps {
  open: boolean;
  editingItem: MetodoPagamento | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function MetodosUpsert({
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly = false,
}: MetodosUpsertProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: MetodoPagamentoFormValues) => {
        return editingItem
          ? await metodosApi.update(editingItem.codigo, value)
          : await metodosApi.create(value);
      },
      queryKey: ["metodosPagamento"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      codigo: editingItem?.codigo ?? "",
      descricao: editingItem?.descricao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as MetodoPagamentoFormValues,
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
      title={editingItem ? "Editar Método de Pagamento" : "Novo Método de Pagamento"}
      footer={
        <>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancelar <Kbd>Esc</Kbd>
            </Button>
          </DialogClose>
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
        </>
      }
    >
      <form
        id="upsert-metodos"
        className="flex flex-col gap-4"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
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
                placeholder={editingItem ? undefined : "Deixe em branco para auto-gerar"}
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

