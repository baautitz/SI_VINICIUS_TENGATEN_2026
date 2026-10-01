"use client";

import { Kbd, KbdGroup } from "@/ui/primitives";
import React from "react";
import { Button } from "@/ui/primitives";
import {
  Field,
  FieldGroup,
  FieldLabel,
  FieldError,
} from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { useForm, useStore } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import {
  skuAtributoChaveSchema,
  SkuAtributoChave,
  SkuAtributoChaveFormValues,
} from "./types";
import { useQuery } from "@tanstack/react-query";
import { atributosApi } from "@/api/catalogo";
import { Plus, X } from "lucide-react";
import { Input } from "@/ui/primitives";
import { Badge } from "@/ui/primitives";
import { Card, CardContent } from "@/ui/primitives";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface AtributosUpsertProps {
  editingItem: SkuAtributoChave | null;
  readOnly?: boolean;
}

export function AtributosUpsert(props: AtributosUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["atributos", "detail", editingItem?.id],
    queryFn: () => atributosApi.getById(editingItem!.id),
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
    <AtributosUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function AtributosUpsertForm({
  editingItem,
  readOnly = false,
}: AtributosUpsertProps) {
  const activeWindow = useWindow<true>();
  const { mutation, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: SkuAtributoChaveFormValues) => {
        return editingItem
          ? await atributosApi.update(editingItem.id, value)
          : await atributosApi.create(value);
      },
      queryKey: [["atributos"], ["produtos"]],
      onSuccessCallback: () => activeWindow.resolve(true),
    });

  const form = useForm({
    defaultValues: {
      chave: editingItem?.chave ?? "",
      valores: editingItem?.skuAtributosValores?.map((v) => v.valor) ?? [],
    } as SkuAtributoChaveFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      try {
        await mutation.mutateAsync(value);
      } catch {
        // O hook central já apresenta o erro operacional em um toast.
      }
    },
  });

  const [newValue, setNewValue] = React.useState("");

  const isDirty = useStore(form.store, (state) => state.isDirty);

  React.useEffect(() => {
    activeWindow.setDirty(isDirty);
    return () => activeWindow.setDirty(false);
  }, [activeWindow, isDirty]);

  const commands = React.useMemo(
    () => [
      {
        id: "atributos.save",
        hotkey: "Alt+Enter" as const,
        label: "Salvar atributo",
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
              form="upsert-atributos"
              disabled={readOnly || !canSubmit || isSubmitting}
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
      </div>
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
                name="chave"
                validators={{ onChange: skuAtributoChaveSchema.shape.chave }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Atributo"
                    inputSize="full"
                    getFieldError={getFieldError}
                    disabled={readOnly}
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
                e.stopPropagation();
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
                      disabled={readOnly}
                      onChange={(e) => setNewValue(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter" && !e.altKey && !e.ctrlKey) {
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
                      disabled={readOnly}
                    >
                      <Plus className="size-4" />
                    </Button>
                  </div>

                  <Card
                    size="sm"
                    className="bg-muted/10 mt-2 w-full border-dashed"
                  >
                    <CardContent className="flex min-h-20 flex-wrap gap-2 py-3">
                      {currentValues.length > 0 ? (
                        currentValues.map((val, idx) => (
                          <Badge
                            key={idx}
                            variant="secondary"
                            className="flex h-7 items-center gap-1 rounded-md py-1 pr-1 pl-3 text-sm font-medium"
                          >
                            {val}
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon-xs"
                              className="text-muted-foreground hover:text-foreground hover:bg-muted/80 h-5 w-5 rounded-sm p-0"
                              onClick={() => handleRemoveValue(idx)}
                              disabled={readOnly}
                            >
                              <X className="size-3" />
                            </Button>
                          </Badge>
                        ))
                      ) : (
                        <span className="text-muted-foreground m-auto text-sm">
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

      </form>
    </div>
  );
}
