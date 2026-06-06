"use client";

import { Kbd } from "@/components/ui/kbd";
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
import {
  unidadeMedidaSchema,
  UnidadeMedida,
  UnidadeMedidaFormValues,
} from "./types";
import { useQuery } from "@tanstack/react-query";
import { unidadesMedidaApi } from "@/api/catalogo";

interface UnidadesMedidaUpsertProps {
  open: boolean;
  editingItem: UnidadeMedida | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function UnidadesMedidaUpsert(props: UnidadesMedidaUpsertProps) {
  const { open, editingItem, onClose } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["unidades-medida", "detail", editingItem?.id],
    queryFn: () => unidadesMedidaApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Unidade de Medida"
        loading={true}
      />
    );
  }

  return (
    <UnidadesMedidaUpsertForm
      {...props}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function UnidadesMedidaUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: UnidadesMedidaUpsertProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: UnidadeMedidaFormValues) => {
        return editingItem
          ? await unidadesMedidaApi.update(editingItem.id, value)
          : await unidadesMedidaApi.create(value);
      },
      queryKey: ["unidades-medida"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      sigla: editingItem?.sigla ?? "",
      descricao: editingItem?.descricao ?? "",
      categoria: editingItem?.categoria ?? "",
      ativo: editingItem?.ativo ?? true,
    } as UnidadeMedidaFormValues,
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
      title={
        editingItem ? "Editar Unidade de Medida" : "Nova Unidade de Medida"
      }
      footer={
        <>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancelar <Kbd className="ml-2">Esc</Kbd>
            </Button>
          </DialogClose>
          <form.Subscribe
            selector={(state) => [state.canSubmit, state.isSubmitting]}
          >
            {([canSubmit, isSubmitting]) => (
              <Button
                type="submit"
                form="upsert-unidades-medida"
                disabled={!canSubmit || isSubmitting}
              >
                {isSubmitting ? "Salvando..." : <span className="flex items-center gap-2">Salvar <Kbd className="bg-primary-foreground/20 text-primary-foreground">Ctrl+Enter</Kbd></span>}
              </Button>
            )}
          </form.Subscribe>
        </>
      }
    >
      <form
        id="upsert-unidades-medida"
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
              <div className="w-24 shrink-0">
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
            <div className="flex-1 min-w-32">
              <form.Field
                name="sigla"
                validators={{ onChange: unidadeMedidaSchema.shape.sigla }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Sigla"
                    inputSize="full"
                    getFieldError={getFieldError}
                    maxLength={10}
                    onChangeOverride={(val) => val.toUpperCase()}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="descricao"
            validators={{ onChange: unidadeMedidaSchema.shape.descricao }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Descrição"
                inputSize="medium"
                getFieldError={getFieldError}
                maxLength={100}
              />
            )}
          </form.Field>

          <form.Field
            name="categoria"
            validators={{ onChange: unidadeMedidaSchema.shape.categoria }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Categoria"
                inputSize="medium"
                getFieldError={getFieldError}
                maxLength={50}
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
