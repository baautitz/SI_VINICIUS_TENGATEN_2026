"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { FieldLabel } from "@/components/ui/field";
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
  readOnly?: boolean;
}

interface UnidadesMedidaUpsertFormProps {
  open: boolean;
  editingItem: UnidadeMedida | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function UnidadesMedidaUpsert(props: UnidadesMedidaUpsertProps) {
  const { open, editingItem, onClose, onSuccess, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["unidadesMedida", "detail", editingItem?.id],
    queryFn: () => unidadesMedidaApi.getById(editingItem!.id),
    enabled: isEditMode && open,
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
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function UnidadesMedidaUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly = false,
}: UnidadesMedidaUpsertFormProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: UnidadeMedidaFormValues) => {
        return editingItem
          ? await unidadesMedidaApi.update(editingItem.id, value)
          : await unidadesMedidaApi.create(value);
      },
      queryKey: ["unidadesMedida"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      sigla: editingItem?.sigla ?? "",
      descricao: editingItem?.descricao ?? "",
      categoria: editingItem?.categoria ?? "",
      permiteDecimais: editingItem?.permiteDecimais ?? false,
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
              Cancelar <Kbd>Esc</Kbd>
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
        id="upsert-unidades-medida"
        className="flex flex-col gap-6"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <div className="flex flex-wrap items-start gap-4">
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
          <div className="w-32">
            <form.Field
              name="sigla"
              validators={{ onChange: unidadeMedidaSchema.shape.sigla }}
            >
              {(field) => (
                <FormFieldUI
                  field={field}
                  label="Sigla"
                  getFieldError={getFieldError}
                  inputSize="full"
                />
              )}
            </form.Field>
          </div>
          <div className="min-w-62.5 flex-1">
            <form.Field
              name="descricao"
              validators={{ onChange: unidadeMedidaSchema.shape.descricao }}
            >
              {(field) => (
                <FormFieldUI
                  field={field}
                  label="Descrição"
                  getFieldError={getFieldError}
                  inputSize="full"
                />
              )}
            </form.Field>
          </div>
        </div>

        <div className="flex flex-wrap items-start gap-4">
          <div className="min-w-62.5 flex-1">
            <form.Field
              name="categoria"
              validators={{ onChange: unidadeMedidaSchema.shape.categoria }}
            >
              {(field) => (
                <FormFieldUI
                  field={field}
                  label="Categoria (ex: Peso, Volume)"
                  getFieldError={getFieldError}
                  inputSize="full"
                />
              )}
            </form.Field>
          </div>
        </div>

        <form.Field name="permiteDecimais">
          {(field) => (
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id={field.name}
                checked={field.state.value}
                onChange={(e) => field.handleChange(e.target.checked)}
                className="h-4 w-4"
              />
              <FieldLabel htmlFor={field.name}>
                Permite casas decimais
              </FieldLabel>
            </div>
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
