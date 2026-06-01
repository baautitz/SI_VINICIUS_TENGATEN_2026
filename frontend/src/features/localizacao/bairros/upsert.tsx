"use client"

import React from "react"
import { Button } from "@/components/ui/button"
import { UpsertDialog } from "@/components/ui/upsert-dialog"
import { DialogClose } from "@/components/ui/dialog"
import { FieldGroup } from "@/components/ui/field"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { FormFieldUI } from "@/components/ui/form-field-ui"
import { CidadeInput } from "@/components/entity-inputs/cidade-input"
import { BairrosClient, CreateBairroDto, UpdateBairroDto } from "@/api/client"
import { API_URL } from "@/api/url"
import { useForm } from "@tanstack/react-form"
import { useUpsertMutation } from "@/hooks/use-upsert-mutation"
import { bairroSchema, BairroDto } from "./types"

interface BairrosUpsertProps {
  open: boolean
  editingItem: BairroDto | null
  onClose: () => void
  onSuccess: () => void
}

export function BairrosUpsert({ open, editingItem, onClose, onSuccess }: BairrosUpsertProps) {
  const { mutation, globalError, getFieldError, resetErrors } = useUpsertMutation({
    mutationFn: async (value: { bairro: string; cidadeId: number }) => {
      const client = new BairrosClient(API_URL)
      return editingItem
        ? await client.updateBairro(editingItem.id, new UpdateBairroDto(value))
        : await client.createBairro(new CreateBairroDto(value))
    },
    queryKey: ['bairros'],
    onSuccessCallback: onSuccess,
    onClose: onClose
  })

  const form = useForm({
    defaultValues: {
      bairro: editingItem?.bairro ?? "",
      cidadeId: editingItem?.cidadeId ?? (undefined as unknown as number),
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
      title={editingItem ? "Editar Bairro" : "Novo Bairro"}
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
              <Button type="submit" form="upsert-bairros" disabled={!canSubmit || isSubmitting}>
                {isSubmitting ? "Salvando..." : "Salvar"}
              </Button>
            )}
          </form.Subscribe>
        </>
      }
    >
      <form
        id="upsert-bairros"
        className="flex flex-col gap-4"
        onSubmit={(e) => {
          e.preventDefault()
          e.stopPropagation()
          form.handleSubmit()
        }}
      >
        <FieldGroup className="gap-4">
          <form.Field
            name="bairro"
            validators={{ onChange: bairroSchema.shape.bairro }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Bairro"
                getFieldError={getFieldError}
              />
            )}
          </form.Field>

          <form.Field
            name="cidadeId"
            validators={{ onChange: bairroSchema.shape.cidadeId }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors)
              return (
                <CidadeInput
                  name={field.name}
                  error={error}
                  initialDisplayValue={editingItem ? `${editingItem.cidadeNome} (${editingItem.uf})` : ""}
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

