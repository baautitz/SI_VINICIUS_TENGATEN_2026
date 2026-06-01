"use client"

import React from "react"
import { Button } from "@/components/ui/button"
import { UpsertDialog } from "@/components/ui/upsert-dialog"
import { DialogClose } from "@/components/ui/dialog"
import { FieldGroup } from "@/components/ui/field"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { FormFieldUI } from "@/components/ui/form-field-ui"
import { EstadoInput } from "@/components/entity-inputs/estado-input"
import { CidadesClient, CreateCidadeDto, UpdateCidadeDto } from "@/api/client"
import { API_URL } from "@/api/url"
import { useForm } from "@tanstack/react-form"
import { useUpsertMutation } from "@/hooks/use-upsert-mutation"
import { cidadeSchema, CidadeDto } from "./types"

interface CidadesUpsertProps {
  open: boolean
  editingItem: CidadeDto | null
  onClose: () => void
  onSuccess: () => void
}

export function CidadesUpsert({ open, editingItem, onClose, onSuccess }: CidadesUpsertProps) {
  const { mutation, globalError, getFieldError, resetErrors } = useUpsertMutation({
    mutationFn: async (value: { cidade: string; ddd: number; estadoId: number }) => {
      const client = new CidadesClient(API_URL)
      return editingItem
        ? await client.updateCidade(editingItem.id, new UpdateCidadeDto(value))
        : await client.createCidade(new CreateCidadeDto(value))
    },
    queryKey: ['cidades'],
    onSuccessCallback: onSuccess,
    onClose: onClose
  })

  const form = useForm({
    defaultValues: {
      cidade: editingItem?.cidade ?? "",
      ddd: (editingItem?.ddd ?? "") as unknown as number,
      estadoId: editingItem?.estadoId ?? (undefined as unknown as number),
    },
    onSubmit: async ({ value }) => {
      resetErrors()
      mutation.mutate(value)
    },
  })

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => { if (!o) onClose() }}
      title={editingItem ? "Editar Cidade" : "Nova Cidade"}
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
              <Button type="submit" form="upsert-cidades" disabled={!canSubmit || isSubmitting}>
                {isSubmitting ? "Salvando..." : "Salvar"}
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
          e.preventDefault()
          e.stopPropagation()
          form.handleSubmit()
        }}
      >
        <FieldGroup className="gap-4">
          <form.Field
            name="cidade"
            validators={{ onChange: cidadeSchema.shape.cidade }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Cidade"
                getFieldError={getFieldError}
              />
            )}
          </form.Field>

          <form.Field
            name="ddd"
            validators={{ onChange: cidadeSchema.shape.ddd }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="DDD"
                inputSize="small"
                type="number"
                getFieldError={getFieldError}
              />
            )}
          </form.Field>

          <form.Field
            name="estadoId"
            validators={{ onChange: cidadeSchema.shape.estadoId }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors)
              return (
                <EstadoInput
                  name={field.name}
                  error={error}
                  initialDisplayValue={editingItem ? `${editingItem.estadoNome} (${editingItem.uf})` : ""}
                  onSelectId={(id) => field.handleChange(id as number)}
                />
              )
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
  )
}
