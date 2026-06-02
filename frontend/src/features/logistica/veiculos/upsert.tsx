"use client";

import React from "react";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { TransportadoraInput } from "@/components/entity-inputs/transportadora-input";
import { EstadoInput } from "@/components/entity-inputs/estado-input";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { veiculoSchema, VeiculoResumo, Veiculo, VeiculoFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { veiculosApi } from "@/api/logistica";

interface VeiculosUpsertProps {
  open: boolean;
  editingItem: VeiculoResumo | null;
  onClose: () => void;
  onSuccess: () => void;
}

interface VeiculosUpsertFormProps {
  open: boolean;
  editingItem: Veiculo | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function VeiculosUpsert(props: VeiculosUpsertProps) {
  const { open, editingItem, onClose, onSuccess } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["veiculos", "detail", editingItem?.id],
    queryFn: () => veiculosApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Veículo"
        loading={true}
      />
    );
  }

  return (
    <VeiculosUpsertForm
      open={open}
      editingItem={isEditMode ? fullItem ?? null : null}
      onClose={onClose}
      onSuccess={onSuccess}
    />
  );
}

function VeiculosUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: VeiculosUpsertFormProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: VeiculoFormValues) => {
        return editingItem
          ? await veiculosApi.update(editingItem.id, value)
          : await veiculosApi.create(value);
      },
      queryKey: ["veiculos"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      placa: editingItem?.placa ?? "",
      estadoId: editingItem?.estadoId ?? 0,
      transportadoraId: editingItem?.transportadoraId ?? null,
      rntrc: editingItem?.rntrc ?? "",
      renavam: editingItem?.renavam ?? "",
      tipoVeiculo: editingItem?.tipoVeiculo ?? "",
      marcaModelo: editingItem?.marcaModelo ?? "",
      observacao: editingItem?.observacao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as VeiculoFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        transportadoraId: value.transportadoraId || null,
      };
      mutation.mutate(payload as VeiculoFormValues);
    },
  });

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Veículo" : "Novo Veículo"}
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
                form="upsert-veiculos"
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
        id="upsert-veiculos"
        className="flex flex-col gap-6"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <div className="flex flex-col gap-4">
          <div className="flex flex-wrap items-start gap-4">
            <div className="w-fit">
              <form.Field
                name="placa"
                validators={{ onChange: veiculoSchema.shape.placa }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Placa"
                    getFieldError={getFieldError}
                    inputSize="small"
                  />
                )}
              </form.Field>
            </div>
            <div className="w-fit">
              <form.Field
                name="estadoId"
                validators={{ onChange: veiculoSchema.shape.estadoId }}
              >
                {(field) => {
                  const error = getFieldError(
                    field.name,
                    field.state.meta.errors,
                  );
                  return (
                    <EstadoInput
                      name={field.name}
                      error={error}
                      initialItem={editingItem?.estado}
                      onSelectId={(id) => field.handleChange(id ?? 0)}
                    />
                  );
                }}
              </form.Field>
            </div>
            <div className="flex-1 min-w-62.5">
              <form.Field
                name="marcaModelo"
                validators={{
                  onChange: veiculoSchema.shape.marcaModelo,
                }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Marca / Modelo"
                    getFieldError={getFieldError}
                    inputSize="full"
                  />
                )}
              </form.Field>
            </div>
          </div>

          <div className="flex flex-wrap items-start gap-4">
            <div className="flex-1">
              <form.Field
                name="transportadoraId"
                validators={{ onChange: veiculoSchema.shape.transportadoraId }}
              >
                {(field) => {
                  const error = getFieldError(
                    field.name,
                    field.state.meta.errors,
                  );
                  return (
                    <TransportadoraInput
                      name={field.name}
                      error={error}
                      initialItem={editingItem?.transportadora}
                      onSelectId={(id) => field.handleChange(id)}
                    />
                  );
                }}
              </form.Field>
            </div>
          </div>

          <div className="flex flex-wrap items-start gap-4">
            <div className="w-fit">
              <form.Field
                name="rntrc"
                validators={{ onChange: veiculoSchema.shape.rntrc }}
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
            <div className="w-fit">
              <form.Field
                name="renavam"
                validators={{ onChange: veiculoSchema.shape.renavam }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Renavam"
                    getFieldError={getFieldError}
                    inputSize="medium"
                  />
                )}
              </form.Field>
            </div>
            <div className="w-fit">
              <form.Field
                name="tipoVeiculo"
                validators={{ onChange: veiculoSchema.shape.tipoVeiculo }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Tipo de Veículo"
                    getFieldError={getFieldError}
                    inputSize="medium"
                  />
                )}
              </form.Field>
            </div>
          </div>
        </div>

        <form.Field
          name="observacao"
          validators={{ onChange: veiculoSchema.shape.observacao }}
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
