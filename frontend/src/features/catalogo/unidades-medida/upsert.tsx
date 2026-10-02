"use client";
import { ProdutosUpsert } from "@/features/catalogo/produtos/upsert";
import { produtosApi } from "@/api/catalogo";
import { relacionadosApi } from "@/api/relacionados";
import { useRelated } from "@/hooks/use-related";
import { RelatedActions } from "@/components/related-actions";
import { Package } from "lucide-react";

import React from "react";
import { WindowActions } from "@/imperative-ui";
import { Kbd, KbdGroup } from "@/ui/primitives";
import { Button } from "@/ui/primitives";
import { FieldLabel } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { Checkbox } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import {
  unidadeMedidaSchema,
  UnidadeMedida,
  UnidadeMedidaFormValues,
} from "./types";
import { useQuery } from "@tanstack/react-query";
import { unidadesMedidaApi } from "@/api/catalogo";
import { useWindow, useWindowCommands } from "@/ui/imperative";
import { Spinner } from "@/ui/primitives";

export interface UnidadesMedidaUpsertProps {
  editingItem: UnidadeMedida | null;
  readOnly?: boolean;
}

interface UnidadesMedidaUpsertFormProps {
  editingItem: UnidadeMedida | null;
  readOnly?: boolean;
}

export function UnidadesMedidaUpsert(props: UnidadesMedidaUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["unidadesMedida", "detail", editingItem?.id],
    queryFn: () => unidadesMedidaApi.getById(editingItem!.id),
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
    <UnidadesMedidaUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function UnidadesMedidaUpsertForm({
  editingItem,
  readOnly = false,
}: UnidadesMedidaUpsertFormProps) {
  const activeWindow = useWindow<true>();
  const { mutation, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: UnidadeMedidaFormValues) => {
        return editingItem
          ? await unidadesMedidaApi.update(editingItem.id, value)
          : await unidadesMedidaApi.create(value);
      },
      queryKey: ["unidadesMedida"],
      onSuccessCallback: () => activeWindow.resolve(true),
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
      if (readOnly) return;
      resetErrors();
      try {
        await mutation.mutateAsync(value);
      } catch {
        // O hook central já apresenta o erro operacional em um toast.
      }
    },
  });

  const registerDirty = React.useCallback(() => {
    activeWindow.setDirtyCheck(() => form.state.isDirty);
    return () => activeWindow.setDirtyCheck(null);
  }, [activeWindow, form]);
  const commands = React.useMemo(
    () => [
      {
        id: "unidades-medida.save",
        hotkey: "Alt+Enter" as const,
        label: "Salvar unidade de medida",
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

  const related = useRelated();
  return (
    <div className="flex flex-col gap-4">
      <WindowActions>
        {readOnly && editingItem && <RelatedActions actions={[{ id: "unidade.produtos", hotkey: "Alt+R", label: "Produtos", icon: <Package className="size-4" />, run: () => related.openList(() => relacionadosApi.produtosPor("unidadeMedidaId", editingItem.id), produtosApi.getById, ProdutosUpsert, "Produtos (unidade)", "Nenhum produto nesta unidade.", false) }]} />}
        <Button
          type="button"
          variant="outline"
          onClick={() => activeWindow.dismiss("cancel")}
        >
          {readOnly ? "Fechar" : "Cancelar"} <Kbd>Esc</Kbd>
        </Button>
        {!readOnly && (<form.Subscribe
          selector={(state) => [state.canSubmit, state.isSubmitting]}
        >
          {([canSubmit, isSubmitting]) => (
            <Button
              type="submit"
              form="upsert-unidades-medida"
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
        </form.Subscribe>)}
      </WindowActions>
      <form
        ref={registerDirty}
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
                  disabled={readOnly}
                  maxLength={10}
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
                  disabled={readOnly}
                  maxLength={100}
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
                  disabled={readOnly}
                  maxLength={50}
                />
              )}
            </form.Field>
          </div>
        </div>

        <form.Field name="permiteDecimais">
          {(field) => (
            <div className="flex items-center gap-2">
              <Checkbox
                id={field.name}
                name={field.name}
                checked={field.state.value}
                disabled={readOnly}
                onCheckedChange={(checked) => field.handleChange(!!checked)}
              />
              <FieldLabel htmlFor={field.name}>
                Permite casas decimais
              </FieldLabel>
            </div>
          )}
        </form.Field>

      </form>
    </div>
  );
}
