"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { FieldGroup, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { EstadoInput } from "@/components/entity-inputs/estado-input";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { cidadeSchema, Cidade, CidadeFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { cidadesApi } from "@/api/localizacao";

interface CidadesUpsertProps {
  open: boolean;
  editingItem: Cidade | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

interface CidadesUpsertFormProps {
  open: boolean;
  editingItem: Cidade | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function CidadesUpsert(props: CidadesUpsertProps) {
  const { open, editingItem, onClose, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["cidades", "detail", editingItem?.id],
    queryFn: () => cidadesApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
      isEdit={!!editingItem}
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Cidade"
        loading={true}
      />
    );
  }

  return (
    <CidadesUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function CidadesUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: CidadesUpsertFormProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: CidadeFormValues) => {
        return editingItem
          ? await cidadesApi.update(editingItem.id, value)
          : await cidadesApi.create(value);
      },
      queryKey: ["cidades"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      cidade: editingItem?.cidade ?? "",
      ddd: editingItem?.ddd ?? "",
      estadoId: editingItem?.estado?.id ?? null,
    } as CidadeFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        ddd: value.ddd || "",
        estadoId: value.estadoId || null,
      };
      mutation.mutate(payload as CidadeFormValues);
    },
  });

  return (
    <UpsertDialog
      isEdit={!!editingItem}
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Cidade" : "Nova Cidade"}
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
                form="upsert-cidades"
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
        id="upsert-cidades"
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
                name="cidade"
                validators={{ onChange: cidadeSchema.shape.cidade }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Cidade"
                    inputSize="full"
                    getFieldError={getFieldError}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="ddd"
            validators={{ onChange: cidadeSchema.shape.ddd }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="DDD"
                inputSize="small"
                type="text"
                inputMode="numeric"
                onChangeOverride={(val) => val.replace(/\D/g, "")}
                getFieldError={getFieldError}
              />
            )}
          </form.Field>

          <form.Field
            name="estadoId"
            validators={{
              onChange: ({ value }) => {
                const res = cidadeSchema.shape.estadoId.safeParse(value);
                return res.success ? undefined : res.error.errors[0]?.message;
              },
            }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors);
              return (
                <EstadoInput
                  name={field.name}
                  error={error}
                  initialItem={editingItem?.estado}
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
