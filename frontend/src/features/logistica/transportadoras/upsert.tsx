"use client";

import React from "react";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { BairroInput } from "@/components/entity-inputs/bairro-input";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { transportadoraSchema, TransportadoraResumo, Transportadora, TransportadoraFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { transportadorasApi } from "@/api/logistica";

interface TransportadorasUpsertProps {
  open: boolean;
  editingItem: TransportadoraResumo | null;
  onClose: () => void;
  onSuccess: () => void;
}

interface TransportadorasUpsertFormProps {
  open: boolean;
  editingItem: Transportadora | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function TransportadorasUpsert(props: TransportadorasUpsertProps) {
  const { open, editingItem, onClose, onSuccess } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["transportadoras", "detail", editingItem?.id],
    queryFn: () => transportadorasApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Transportadora"
        loading={true}
      />
    );
  }

  return (
    <TransportadorasUpsertForm
      open={open}
      editingItem={isEditMode ? fullItem ?? null : null}
      onClose={onClose}
      onSuccess={onSuccess}
    />
  );
}

function TransportadorasUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: TransportadorasUpsertFormProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: TransportadoraFormValues) => {
        return editingItem
          ? await transportadorasApi.update(editingItem.id, value)
          : await transportadorasApi.create(value);
      },
      queryKey: ["transportadoras"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      nomeRazaosocial: editingItem?.nomeRazaosocial ?? "",
      cpfCnpj: editingItem?.cpfCnpj ?? "",
      apelidoNomefantasia: editingItem?.apelidoNomefantasia ?? "",
      endereco: editingItem?.endereco ?? "",
      bairroId: editingItem?.bairro?.id ?? null,
      telefone: editingItem?.telefone ?? "",
      email: editingItem?.email ?? "",
      rgIe: editingItem?.rgIe ?? "",
      rntrc: editingItem?.rntrc ?? "",
      observacao: editingItem?.observacao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as TransportadoraFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        bairroId: value.bairroId || null,
      };
      mutation.mutate(payload as TransportadoraFormValues);
    },
  });

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Transportadora" : "Nova Transportadora"}
      footer={
        <>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancelar
            </Button>
          </DialogClose>
          <form.Subscribe
            selector={(state) => [state.canSubmit, state.isSubmitting]}
          >
            {([canSubmit, isSubmitting]) => (
              <Button
                type="submit"
                form="upsert-transportadoras"
                disabled={!canSubmit || isSubmitting}
              >
                {isSubmitting ? "Salvando..." : "Salvar"}
              </Button>
            )}
          </form.Subscribe>
        </>
      }
    >
      <form
        id="upsert-transportadoras"
        className="flex flex-col gap-6"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <div className="flex flex-col gap-4">
          <div className="flex flex-wrap items-start gap-4">
            <div className="flex-1 min-w-62.5">
              <form.Field
                name="nomeRazaosocial"
                validators={{ onChange: transportadoraSchema.shape.nomeRazaosocial }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Nome / Razão Social"
                    getFieldError={getFieldError}
                    inputSize="full"
                  />
                )}
              </form.Field>
            </div>
            <div className="flex-1 min-w-62.5">
              <form.Field
                name="apelidoNomefantasia"
                validators={{
                  onChange: transportadoraSchema.shape.apelidoNomefantasia,
                }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Apelido / Nome Fantasia"
                    getFieldError={getFieldError}
                    inputSize="full"
                  />
                )}
              </form.Field>
            </div>
          </div>

          <div className="flex flex-wrap items-start gap-4">
            <div className="w-fit">
              <form.Field
                name="cpfCnpj"
                validators={{ onChange: transportadoraSchema.shape.cpfCnpj }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="CPF / CNPJ"
                    getFieldError={getFieldError}
                    inputSize="medium"
                  />
                )}
              </form.Field>
            </div>
            <div className="w-fit">
              <form.Field
                name="rgIe"
                validators={{ onChange: transportadoraSchema.shape.rgIe }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="RG / Inscrição Estadual"
                    getFieldError={getFieldError}
                    inputSize="medium"
                  />
                )}
              </form.Field>
            </div>
            <div className="w-fit">
              <form.Field
                name="telefone"
                validators={{ onChange: transportadoraSchema.shape.telefone }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Telefone"
                    getFieldError={getFieldError}
                    inputSize="medium"
                  />
                )}
              </form.Field>
            </div>
            <div className="w-fit">
              <form.Field
                name="rntrc"
                validators={{ onChange: transportadoraSchema.shape.rntrc }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="RNTRC"
                    getFieldError={getFieldError}
                    inputSize="medium"
                  />
                )}
              </form.Field>
            </div>
          </div>

          <div className="flex flex-wrap items-start gap-4">
            <div className="flex-1 min-w-62.5">
              <form.Field
                name="email"
                validators={{ onChange: transportadoraSchema.shape.email }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="E-mail"
                    getFieldError={getFieldError}
                    inputSize="full"
                  />
                )}
              </form.Field>
            </div>
          </div>

          <div className="flex flex-wrap items-start gap-4">
            <div className="flex-2">
              <form.Field
                name="endereco"
                validators={{ onChange: transportadoraSchema.shape.endereco }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Endereço (Rua, Número, Complemento)"
                    getFieldError={getFieldError}
                    inputSize="full"
                  />
                )}
              </form.Field>
            </div>
            <div className="flex-1">
              <form.Field
                name="bairroId"
                validators={{ onChange: transportadoraSchema.shape.bairroId }}
              >
                {(field) => {
                  const error = getFieldError(
                    field.name,
                    field.state.meta.errors,
                  );
                  return (
                    <BairroInput
                      name={field.name}
                      error={error}
                      initialItem={editingItem?.bairro}
                      onSelectId={(id) => field.handleChange(id)}
                    />
                  );
                }}
              </form.Field>
            </div>
          </div>
        </div>

        <form.Field
          name="observacao"
          validators={{ onChange: transportadoraSchema.shape.observacao }}
        >
          {(field) => (
            <FormFieldUI
              field={field}
              label="Observação"
              getFieldError={getFieldError}
              inputSize="full"
            />
          )}
        </form.Field>

        {globalError && (
          <Alert variant="destructive">
            <AlertDescription>{globalError}</AlertDescription>
          </Alert>
        )}
      </form>
    </UpsertDialog>
  );
}
