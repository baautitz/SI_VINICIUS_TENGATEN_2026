"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import React, { useState } from "react";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { BairroInput } from "@/components/entity-inputs/bairro-input";
import { PaisInput } from "@/components/entity-inputs/pais-input";
import { TipoPessoaSelect } from "@/components/tipo-pessoa-select";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { emitenteSchema, Emitente, EmitenteFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { emitentesApi } from "@/api/parceiros";
import { TipoPessoa } from "@/api/types";
import { Pais } from "@/features/localizacao/paises";

interface EmitentesUpsertProps {
  open: boolean;
  editingItem: Emitente | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

interface EmitentesUpsertFormProps {
  open: boolean;
  editingItem: Emitente | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function EmitentesUpsert(props: EmitentesUpsertProps) {
  const { open, editingItem, onClose, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["emitentes", "detail", editingItem?.id],
    queryFn: () => emitentesApi.getById(editingItem!.id),
    enabled: isEditMode && open,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Emitente"
        loading={true}
      />
    );
  }

  return (
    <EmitentesUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function EmitentesUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
}: EmitentesUpsertFormProps) {
  const [selectedPais, setSelectedPais] = useState<Pais | null>(
    editingItem?.nacionalidade ?? null,
  );
  const [tipoPessoa, setTipoPessoa] = useState<TipoPessoa>(
    editingItem?.tipoPessoa ?? TipoPessoa.JURIDICA,
  );
  const [nacionalidadeId, setNacionalidadeId] = useState<number>(
    editingItem?.nacionalidade?.id ?? 0,
  );

  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: EmitenteFormValues) => {
        return editingItem
          ? await emitentesApi.update(editingItem.id, value)
          : await emitentesApi.create(value);
      },
      queryKey: ["emitentes"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      tipoPessoa: editingItem?.tipoPessoa ?? TipoPessoa.JURIDICA,
      nomeRazaoSocial: editingItem?.nomeRazaoSocial ?? "",
      cpfCnpj: editingItem?.cpfCnpj ?? "",
      apelidoNomeFantasia: editingItem?.apelidoNomeFantasia ?? "",
      endereco: editingItem?.endereco ?? "",
      bairroId: editingItem?.bairro?.id ?? null,
      nacionalidadeId: editingItem?.nacionalidade?.id ?? 0,
      telefone: editingItem?.telefone ?? "",
      email: editingItem?.email ?? "",
      rgIe: editingItem?.rgIe ?? "",
      inscricaoMunicipal: editingItem?.inscricaoMunicipal ?? "",
      regimeTributario: editingItem?.regimeTributario ?? "",
      observacao: editingItem?.observacao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as EmitenteFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        bairroId: value.bairroId || null,
        rgIe: isBrasil ? value.rgIe : "",
      };
      mutation.mutate(payload as EmitenteFormValues);
    },
  });

  const isBrasil =
    selectedPais?.codigoIsoPais === "BRA" ||
    (!selectedPais && nacionalidadeId === 1);

  return (
    <UpsertDialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
      title={editingItem ? "Editar Emitente" : "Novo Emitente"}
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
                form="upsert-emitentes"
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
        id="upsert-emitentes"
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
                name="tipoPessoa"
                validators={{ onChange: emitenteSchema.shape.tipoPessoa }}
              >
                {(field) => (
                  <TipoPessoaSelect
                    name={field.name}
                    label="Tipo"
                    value={field.state.value}
                    onChange={(val) => {
                      field.handleChange(val);
                      setTipoPessoa(val);
                    }}
                    error={getFieldError(field.name, field.state.meta.errors)}
                    inputSize="small"
                  />
                )}
              </form.Field>
            </div>
            <div className="min-w-62.5 flex-1">
              <form.Field
                name="nacionalidadeId"
                validators={{ onChange: emitenteSchema.shape.nacionalidadeId }}
              >
                {(field) => (
                  <PaisInput
                    name={field.name}
                    label="Nacionalidade"
                    error={getFieldError(field.name, field.state.meta.errors)}
                    initialItem={editingItem?.nacionalidade}
                    onSelectId={(id) => {
                      field.handleChange(id ?? 0);
                      setNacionalidadeId(id ?? 0);
                    }}
                    onSelectItem={(item) => setSelectedPais(item)}
                  />
                )}
              </form.Field>
            </div>
            <div className="min-w-75 flex-2">
              <form.Field
                name="nomeRazaoSocial"
                validators={{ onChange: emitenteSchema.shape.nomeRazaoSocial }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Nome / Razão Social"
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
                name="apelidoNomeFantasia"
                validators={{
                  onChange: emitenteSchema.shape.apelidoNomeFantasia,
                }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Apelido / Nome Fantasia"
                    getFieldError={getFieldError}
                    inputSize="full"
                  />
                )}
              </form.Field>
            </div>
            <div className="w-fit">
              <form.Field
                name="cpfCnpj"
                validators={{
                  onChange: ({ value }) => {
                    if (!value || value.trim() === "") {
                      return isBrasil ? "CPF/CNPJ é obrigatório." : "Documento é obrigatório.";
                    }
                    return undefined;
                  },
                }}
              >
                {(field) => {
                  let label = "Documento";
                  if (isBrasil) {
                    label = tipoPessoa === TipoPessoa.FISICA ? "CPF" : "CNPJ";
                  }
                  return (
                    <FormFieldUI
                      field={field}
                      label={label}
                      getFieldError={getFieldError}
                      inputSize="medium"
                    />
                  );
                }}
              </form.Field>
            </div>
            {isBrasil && (
              <div className="w-fit">
                <form.Field
                  name="rgIe"
                  validators={{ onChange: emitenteSchema.shape.rgIe }}
                >
                  {(field) => (
                    <FormFieldUI
                      field={field}
                      label={
                        isBrasil && tipoPessoa === TipoPessoa.JURIDICA
                          ? "Inscrição Estadual"
                          : "RG"
                      }
                      getFieldError={getFieldError}
                      inputSize="medium"
                    />
                  )}
                </form.Field>
              </div>
            )}
          </div>

          <div className="flex flex-wrap items-start gap-4">
            <div className="w-fit">
              <form.Field
                name="inscricaoMunicipal"
                validators={{
                  onChange: emitenteSchema.shape.inscricaoMunicipal,
                }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Inscrição Municipal"
                    getFieldError={getFieldError}
                    inputSize="medium"
                  />
                )}
              </form.Field>
            </div>
            <div className="w-fit">
              <form.Field
                name="regimeTributario"
                validators={{ onChange: emitenteSchema.shape.regimeTributario }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Regime Tributário"
                    getFieldError={getFieldError}
                    inputSize="medium"
                  />
                )}
              </form.Field>
            </div>
            <div className="w-fit">
              <form.Field
                name="telefone"
                validators={{ onChange: emitenteSchema.shape.telefone }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Telefone"
                    getFieldError={getFieldError}
                    inputSize="medium"
                  />
                )}
              </form.Field>
            </div>
            <div className="min-w-75 flex-1">
              <form.Field
                name="email"
                validators={{ onChange: emitenteSchema.shape.email }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="E-mail"
                    getFieldError={getFieldError}
                    inputSize="full"
                  />
                )}
              </form.Field>
            </div>
          </div>

          <div className="flex flex-wrap items-start gap-4">
            <div className="flex-2">
              <form.Field
                name="endereco"
                validators={{ onChange: emitenteSchema.shape.endereco }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Endereço (Rua, Número, Complemento)"
                    getFieldError={getFieldError}
                    inputSize="full"
                  />
                )}
              </form.Field>
            </div>
            <div className="flex-1">
              <form.Field
                name="bairroId"
                validators={{ onChange: emitenteSchema.shape.bairroId }}
              >
                {(field) => {
                  const error = getFieldError(
                    field.name,
                    field.state.meta.errors,
                  );
                  return (
                    <BairroInput
                      name={field.name}
                      error={error}
                      initialItem={editingItem?.bairro}
                      onSelectId={(id) => field.handleChange(id)}
                    />
                  );
                }}
              </form.Field>
            </div>
          </div>
        </div>

        <form.Field
          name="observacao"
          validators={{ onChange: emitenteSchema.shape.observacao }}
        >
          {(field) => (
            <FormFieldUI
              field={field}
              label="Observação"
              getFieldError={getFieldError}
              inputSize="full"
            />
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
