"use client"

import React from "react"
import { Button } from "@/components/ui/button"
import { UpsertDialog } from "@/components/ui/upsert-dialog"
import { DialogClose } from "@/components/ui/dialog"
import { FieldGroup } from "@/components/ui/field"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { FormFieldUI } from "@/components/ui/form-field-ui"
import { PaisInput } from "@/components/entity-inputs/pais-input"
import { EstadosClient, CreateEstadoDto, UpdateEstadoDto } from "@/api/client"
import { API_URL } from "@/api/url"
import { useForm } from "@tanstack/react-form"
import { useUpsertMutation } from "@/hooks/use-upsert-mutation"
import { estadoSchema, EstadoDto } from "./types"

interface EstadosUpsertProps {
  open: boolean
  editingItem: EstadoDto | null
  onClose: () => void
  onSuccess: () => void
}

export function EstadosUpsert({ open, editingItem, onClose, onSuccess }: EstadosUpsertProps) {
  const { mutation, globalError, getFieldError, resetErrors } = useUpsertMutation({
    mutationFn: async (value: { estado: string; uf: string; paisId: number }) => {
      const client = new EstadosClient(API_URL)
      return editingItem
        ? await client.updateEstado(editingItem.id, new UpdateEstadoDto(value))
        : await client.createEstado(new CreateEstadoDto(value))
    },
    queryKey: ['estados'],
    onSuccessCallback: onSuccess,
    onClose: onClose
  })

  const form = useForm({
    defaultValues: {
      estado: editingItem?.estado ?? "",
      uf: editingItem?.uf ?? "",
      paisId: editingItem?.paisId ?? (undefined as unknown as number),
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
      title={editingItem ? "Editar Estado" : "Novo Estado"}
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
              <Button type="submit" form="upsert-estados" disabled={!canSubmit || isSubmitting}>
                {isSubmitting ? "Salvando..." : "Salvar"}
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
          e.preventDefault()
          e.stopPropagation()
          form.handleSubmit()
        }}
      >
        <FieldGroup className="gap-4">
          <form.Field
            name="estado"
            validators={{ onChange: estadoSchema.shape.estado }}
          >
            {(field) => (
              <FormFieldUI
                field={field}
                label="Estado"
                getFieldError={getFieldError}
              />
            )}
          </form.Field>

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
            validators={{ onChange: estadoSchema.shape.paisId }}
          >
            {(field) => {
              const error = getFieldError(field.name, field.state.meta.errors)
              return (
                <PaisInput
                  name={field.name}
                  error={error}
                  initialDisplayValue={editingItem ? `${editingItem.paisNome}` : ""}
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
