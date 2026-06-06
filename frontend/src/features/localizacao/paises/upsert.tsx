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
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { paisSchema, PaisDto } from "./types";
import { useQuery } from "@tanstack/react-query";
import { paisesApi } from "@/api/localizacao";

interface PaisesUpsertProps {
  open: boolean;
  editingItem: PaisDto | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function PaisesUpsert(props: PaisesUpsertProps) {
  const { open, editingItem, onClose } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["paises", "detail", editingItem?.id],
    queryFn: () => paisesApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar País"
        loading={true}
      />
    );
  }

  return (
    <PaisesUpsertForm
      {...props}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function PaisesUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: PaisesUpsertProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: {
        pais: string;
        siglaIso: string;
        ddi: string;
        moeda: string;
        simboloMoeda: string;
      }) => {
        return editingItem
          ? await paisesApi.update(editingItem.id, value)
          : await paisesApi.create(value);
      },
      queryKey: ["paises"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      pais: editingItem?.pais ?? "",
      siglaIso: editingItem?.siglaIso ?? "",
      ddi: editingItem?.ddi ?? "",
      moeda: editingItem?.moeda ?? "",
      simboloMoeda: editingItem?.simboloMoeda ?? "",
    },
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
      title={editingItem ? "Editar País" : "Novo País"}
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
                form="upsert-paises"
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
        id="upsert-paises"
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
                name="pais"
                validators={{ onChange: paisSchema.shape.pais }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="País"
                    inputSize="full"
                    getFieldError={getFieldError}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="siglaIso"
            validators={{ onChange: paisSchema.shape.siglaIso }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Sigla ISO"
                inputSize="small"
                getFieldError={getFieldError}
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
              />
            )}
          </form.Field>

          <form.Field
            name="moeda"
            validators={{ onChange: paisSchema.shape.moeda }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Moeda"
                inputSize="medium"
                getFieldError={getFieldError}
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
    </UpsertDialog>
  );
}
