"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import React from "react";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { CidadeInput } from "@/components/entity-inputs/cidade-input";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { bairroSchema, Bairro, BairroFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { bairrosApi } from "@/api/localizacao";

interface BairrosUpsertProps {
  open: boolean;
  editingItem: Bairro | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

interface BairrosUpsertFormProps {
  open: boolean;
  editingItem: Bairro | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function BairrosUpsert(props: BairrosUpsertProps) {
  const { open, editingItem, onClose, onSuccess, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["bairros", "detail", editingItem?.id],
    queryFn: () => bairrosApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Bairro"
        loading={true}
      />
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
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly = false,
}: BairrosUpsertFormProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: BairroFormValues) => {
        return editingItem
          ? await bairrosApi.update(editingItem.id, value)
          : await bairrosApi.create(value);
      },
      queryKey: ["bairros"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
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
      mutation.mutate(payload as BairroFormValues);
    },
  });

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Bairro" : "Novo Bairro"}
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
                form="upsert-bairros"
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
        id="upsert-bairros"
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
                    className="h-8 text-xs"
                    inputSize="small"
                  />
                </div>
              </div>
            )}
            <div className="flex-1 min-w-48">
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
                  onSelectId={(id) =>
                    field.handleChange(id ? parseInt(String(id), 10) : null)
                  }
                />
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
