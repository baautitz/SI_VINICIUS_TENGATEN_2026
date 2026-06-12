"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { PaisInput } from "@/components/entity-inputs/pais-input";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { estadoSchema, Estado, EstadoFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { estadosApi } from "@/api/localizacao";

interface EstadosUpsertProps {
  open: boolean;
  editingItem: Estado | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

interface EstadosUpsertFormProps {
  open: boolean;
  editingItem: Estado | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function EstadosUpsert(props: EstadosUpsertProps) {
  const { open, editingItem, onClose, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["estados", "detail", editingItem?.id],
    queryFn: () => estadosApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Estado"
        loading={true}
      />
    );
  }

  return (
    <EstadosUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function EstadosUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: EstadosUpsertFormProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: EstadoFormValues) => {
        return editingItem
          ? await estadosApi.update(editingItem.id, value)
          : await estadosApi.create(value);
      },
      queryKey: ["estados"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      estado: editingItem?.estado ?? "",
      uf: editingItem?.uf ?? "",
      paisId: editingItem?.pais?.id ?? null,
    } as EstadoFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        paisId: value.paisId || null,
      };
      mutation.mutate(payload as EstadoFormValues);
    },
  });

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Estado" : "Novo Estado"}
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
                form="upsert-estados"
                disabled={!canSubmit || isSubmitting}
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
        </>
      }
    >
      <form
        id="upsert-estados"
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
            <div className="min-w-48 flex-1">
              <form.Field
                name="estado"
                validators={{ onChange: estadoSchema.shape.estado }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Estado"
                    inputSize="full"
                    getFieldError={getFieldError}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="uf"
            validators={{ onChange: estadoSchema.shape.uf }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="UF"
                inputSize="small"
                getFieldError={getFieldError}
                maxLength={2}
                onChangeOverride={(val) => val.toUpperCase()}
              />
            )}
          </form.Field>

          <form.Field
            name="paisId"
            validators={{
              onChange: ({ value }) => {
                const res = estadoSchema.shape.paisId.safeParse(value);
                return res.success ? undefined : res.error.errors[0]?.message;
              },
            }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors);
              return (
                <PaisInput
                  name={field.name}
                  error={error}
                  initialItem={editingItem?.pais}
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
