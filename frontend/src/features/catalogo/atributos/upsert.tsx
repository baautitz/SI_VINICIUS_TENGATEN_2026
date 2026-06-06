"use client";

import { Kbd } from "@/components/ui/kbd";
import React from "react";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import {
  Field,
  FieldGroup,
  FieldLabel,
  FieldError,
} from "@/components/ui/field";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import {
  skuAtributoChaveSchema,
  SkuAtributoChave,
  SkuAtributoChaveResumo,
  SkuAtributoChaveFormValues,
} from "./types";
import { useQuery } from "@tanstack/react-query";
import { atributosApi } from "@/api/catalogo";
import { Plus, X } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";

interface AtributosUpsertProps {
  open: boolean;
  editingItem: SkuAtributoChaveResumo | null;
  onClose: () => void;
  onSuccess: () => void;
}

interface AtributosUpsertFormProps {
  open: boolean;
  editingItem: SkuAtributoChave | null;
  onClose: () => void;
  onSuccess: () => void;
}

export function AtributosUpsert(props: AtributosUpsertProps) {
  const { open, editingItem, onClose } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["atributos", "detail", editingItem?.id],
    queryFn: () => atributosApi.getById(editingItem!.id),
    enabled: isEditMode,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Atributo"
        loading={true}
      />
    );
  }

  return (
    <AtributosUpsertForm
      open={open}
      editingItem={isEditMode ? (fullItem ?? null) : null}
      onClose={onClose}
      onSuccess={props.onSuccess}
    />
  );
}

function AtributosUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: AtributosUpsertFormProps) {
  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: SkuAtributoChaveFormValues) => {
        return editingItem
          ? await atributosApi.update(editingItem.id, value)
          : await atributosApi.create(value);
      },
      queryKey: ["atributos"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      chave: editingItem?.chave ?? "",
      valores: editingItem?.skuAtributosValores?.map((v) => v.valor) ?? [],
    } as SkuAtributoChaveFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      mutation.mutate(value);
    },
  });

  const [newValue, setNewValue] = React.useState("");

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Atributo" : "Novo Atributo"}
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
                form="upsert-atributos"
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
        id="upsert-atributos"
        className="flex flex-col gap-6"
        onSubmit={(e) => {
          e.preventDefault();
          e.stopPropagation();
          form.handleSubmit();
        }}
      >
        <FieldGroup className="gap-6">
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
            <div className="flex-1 min-w-48">
              <form.Field
                name="chave"
                validators={{ onChange: skuAtributoChaveSchema.shape.chave }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Atributo"
                    inputSize="full"
                    getFieldError={getFieldError}
                    maxLength={100}
                    placeholder="Ex: Cor, Voltagem, Tamanho..."
                  />
                )}
              </form.Field>
            </div>
          </div>

          <form.Field
            name="valores"
            validators={{ onChange: skuAtributoChaveSchema.shape.valores }}
          >
            {(field) => {
              const error =
                getFieldError(field.name, field.state.meta.errors) ||
                field.state.meta.errors?.[0]?.message;
              const currentValues: string[] = field.state.value || [];

              const handleAddValue = (
                e: React.MouseEvent | React.KeyboardEvent,
              ) => {
                e.preventDefault();
                const trimmed = newValue.trim();
                if (trimmed) {
                  if (
                    currentValues.some(
                      (v) => v.toLowerCase() === trimmed.toLowerCase(),
                    )
                  ) {
                    return;
                  }
                  field.setValue([...currentValues, trimmed]);
                  setNewValue("");
                }
              };

              const handleRemoveValue = (indexToRemove: number) => {
                field.setValue(
                  currentValues.filter((_, idx) => idx !== indexToRemove),
                );
              };

              return (
                <Field data-invalid={!!error} className="w-full max-w-md">
                  <FieldLabel>Valores Possíveis</FieldLabel>

                  <div className="flex gap-2">
                    <Input
                      inputSize="full"
                      placeholder="Adicionar valor... (ex: Bivolt, Azul, G)"
                      value={newValue}
                      onChange={(e) => setNewValue(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") {
                          e.preventDefault();
                          e.stopPropagation();
                          handleAddValue(e);
                        }
                      }}
                      maxLength={150}
                    />
                    <Button
                      type="button"
                      size="icon"
                      variant="secondary"
                      onClick={handleAddValue}
                    >
                      <Plus className="size-4" />
                    </Button>
                  </div>

                  <Card
                    size="sm"
                    className="w-full mt-2 border-dashed bg-muted/10"
                  >
                    <CardContent className="flex flex-wrap gap-2 py-3 min-h-20">
                      {currentValues.length > 0 ? (
                        currentValues.map((val, idx) => (
                          <Badge
                            key={idx}
                            variant="secondary"
                            className="pl-3 pr-1 py-1 h-7 text-sm font-medium gap-1 flex items-center rounded-md"
                          >
                            {val}
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon-xs"
                              className="h-5 w-5 rounded-sm text-muted-foreground hover:text-foreground hover:bg-muted/80 p-0"
                              onClick={() => handleRemoveValue(idx)}
                            >
                              <X className="size-3" />
                            </Button>
                          </Badge>
                        ))
                      ) : (
                        <span className="text-muted-foreground text-sm m-auto">
                          Nenhum valor adicionado ainda. Digite e adicione
                          valores para este atributo.
                        </span>
                      )}
                    </CardContent>
                  </Card>

                  {error && <FieldError>{error}</FieldError>}
                </Field>
              );
            }}
          </form.Field>
        </FieldGroup>

        {globalError && (
          <Alert variant="destructive" className="mt-4">
            <AlertDescription>{globalError}</AlertDescription>
          </Alert>
        )}
      </form>
    </UpsertDialog>
  );
}
