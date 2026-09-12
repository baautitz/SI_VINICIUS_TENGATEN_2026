"use client";

import { Kbd, KbdGroup } from "@/ui/primitives";
import React from "react";
import { Button } from "@/ui/primitives";
import { FieldLabel } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { TransportadoraInput } from "@/components/entity-inputs/transportadora-input";
import { EstadoInput } from "@/components/entity-inputs/estado-input";
import { useForm, useStore } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { veiculoSchema, Veiculo, VeiculoFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { veiculosApi } from "@/api/parceiros";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface VeiculosUpsertProps {
  editingItem: Veiculo | null;
  readOnly?: boolean;
}

export function VeiculosUpsert(props: VeiculosUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["veiculos", "detail", editingItem?.id],
    queryFn: () => veiculosApi.getById(editingItem!.id),
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
    <VeiculosUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function VeiculosUpsertForm({
  editingItem,
  readOnly = false,
}: VeiculosUpsertProps) {
  const activeWindow = useWindow<true>();
  const { mutation, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: VeiculoFormValues) => {
        return editingItem
          ? await veiculosApi.update(editingItem.id, value)
          : await veiculosApi.create(value);
      },
      queryKey: ["veiculos"],
      onSuccessCallback: () => activeWindow.resolve(true),
    });

  const form = useForm({
    defaultValues: {
      placa: editingItem?.placa ?? "",
      estadoId: editingItem?.estado?.id ?? 0,
      transportadoraId: editingItem?.transportadora?.id ?? null,
      rntrc: editingItem?.rntrc ?? "",
      renavam: editingItem?.renavam ?? "",
      tipoVeiculo: editingItem?.tipoVeiculo ?? "",
      marcaModelo: editingItem?.marcaModelo ?? "",
      observacao: editingItem?.observacao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as VeiculoFormValues,
    onSubmit: async ({ value }) => {
      if (readOnly) return;
      resetErrors();
      const payload = {
        ...value,
        transportadoraId: value.transportadoraId || null,
      };
      try {
        await mutation.mutateAsync(payload as VeiculoFormValues);
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

  const commands = React.useMemo(
    () => [
      {
        id: "veiculos.save",
        hotkey: "Alt+Enter" as const,
        label: "Salvar veículo",
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

  return (
    <div className="flex flex-col gap-4">
      <div data-window-actions className="flex justify-end gap-2 border-b pb-4">
        <Button
          type="button"
          variant="outline"
          onClick={() => activeWindow.dismiss("cancel")}
        >
          Cancelar <Kbd>Esc</Kbd>
        </Button>
        <form.Subscribe
          selector={(state) => [state.canSubmit, state.isSubmitting]}
        >
          {([canSubmit, isSubmitting]) => (
            <Button
              type="submit"
              form="upsert-veiculos"
              disabled={readOnly || !canSubmit || isSubmitting}
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
      </div>
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
                    disabled={readOnly}
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
                      disabled={readOnly}
                      initialItem={editingItem?.estado}
                      onSelectId={(id) => field.handleChange(id ?? 0)}
                    />
                  );
                }}
              </form.Field>
            </div>
            <div className="min-w-62.5 flex-1">
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
                    disabled={readOnly}
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
                      disabled={readOnly}
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
                    disabled={readOnly}
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
                    disabled={readOnly}
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
                    disabled={readOnly}
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
              disabled={readOnly}
            />
          )}
        </form.Field>

      </form>
    </div>
  );
}
