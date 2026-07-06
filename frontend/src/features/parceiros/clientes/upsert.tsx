"use client";

import { Kbd, KbdGroup } from "@/components/ui/kbd";
import React, { useState } from "react";
import { Button } from "@/components/ui/button";
import { UpsertDialog } from "@/components/ui/upsert-dialog";
import { DialogClose } from "@/components/ui/dialog";
import { Field, FieldLabel, FieldError } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormFieldUI } from "@/components/ui/form-field-ui";
import { SexoSelect } from "@/components/sexo-select";
import { DatePicker } from "@/components/ui/date-picker";
import { cn } from "@/lib/utils";
import { BairroInput } from "@/components/entity-inputs/bairro-input";
import { PaisInput } from "@/components/entity-inputs/pais-input";
import { TipoPessoaSelect } from "@/components/tipo-pessoa-select";
import { useForm } from "@tanstack/react-form";
import { useUpsertMutation } from "@/hooks/use-upsert-mutation";
import { clienteSchema, Cliente, ClienteFormValues } from "./types";
import { useQuery } from "@tanstack/react-query";
import { clientesApi } from "@/api/parceiros";
import { TipoPessoa } from "@/api/types";
import { Pais } from "@/features/localizacao/paises";

interface ClientesUpsertProps {
  open: boolean;
  editingItem: Cliente | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

interface ClientesUpsertFormProps {
  open: boolean;
  editingItem: Cliente | null;
  onClose: () => void;
  onSuccess: () => void;
  readOnly?: boolean;
}

export function ClientesUpsert(props: ClientesUpsertProps) {
  const { open, editingItem, onClose, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["clientes", "detail", editingItem?.id],
    queryFn: () => clientesApi.getById(editingItem!.id),
    enabled: isEditMode && open,
  });

  if (isEditMode && isLoading) {
    return (
      <UpsertDialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
        title="Editar Cliente"
        loading={true}
      />
    );
  }

  return (
    <ClientesUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function ClientesUpsertForm({
  open,
  editingItem,
  onClose,
  onSuccess,
  readOnly = false,
}: ClientesUpsertFormProps) {
  const [selectedPais, setSelectedPais] = useState<Pais | null>(
    editingItem?.nacionalidade ?? null,
  );
  const [tipoPessoa, setTipoPessoa] = useState<TipoPessoa>(
    editingItem?.tipoPessoa ?? TipoPessoa.FISICA,
  );
  const [nacionalidadeId, setNacionalidadeId] = useState<number>(
    editingItem?.nacionalidade?.id ?? 0,
  );

  const { mutation, globalError, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: ClienteFormValues) => {
        return editingItem
          ? await clientesApi.update(editingItem.id, value)
          : await clientesApi.create(value);
      },
      queryKey: ["clientes"],
      onSuccessCallback: onSuccess,
      onClose: onClose,
    });

  const form = useForm({
    defaultValues: {
      tipoPessoa: editingItem?.tipoPessoa ?? TipoPessoa.FISICA,
      nomeRazaoSocial: editingItem?.nomeRazaoSocial ?? "",
      cpfCnpj: editingItem?.cpfCnpj ?? "",
      apelidoNomeFantasia: editingItem?.apelidoNomeFantasia ?? "",
      logradouro: editingItem?.logradouro ?? "",
      numero: editingItem?.numero ?? "",
      bairroId: editingItem?.bairro?.id ?? null,
      nacionalidadeId: editingItem?.nacionalidade?.id ?? 0,
      telefone: editingItem?.telefone ?? "",
      email: editingItem?.email ?? "",
      rgIe: editingItem?.rgIe ?? "",
      limiteCredito: editingItem?.limiteCredito ?? 0,
      sexo: editingItem?.sexo ?? "",
      dataNascimento: editingItem?.dataNascimento ? editingItem.dataNascimento.split("T")[0] : "",
      observacao: editingItem?.observacao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as ClienteFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        bairroId: value.bairroId || null,
        rgIe: isBrasil ? value.rgIe : "",
        sexo: value.tipoPessoa === TipoPessoa.FISICA ? value.sexo : "",
        dataNascimento: value.dataNascimento || null,
      };
      mutation.mutate(payload as ClienteFormValues);
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
      title={editingItem ? "Editar Cliente" : "Novo Cliente"}
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
                form="upsert-clientes"
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
        id="upsert-clientes"
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
                validators={{ onChange: clienteSchema.shape.tipoPessoa }}
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
                validators={{ onChange: clienteSchema.shape.nacionalidadeId }}
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
                validators={{ onChange: clienteSchema.shape.nomeRazaoSocial }}
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
                  onChange: clienteSchema.shape.apelidoNomeFantasia,
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
                  validators={{ onChange: clienteSchema.shape.rgIe }}
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
                name="telefone"
                validators={{ onChange: clienteSchema.shape.telefone }}
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
                validators={{ onChange: clienteSchema.shape.email }}
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
            <div className="w-fit">
              <form.Field
                name="limiteCredito"
                validators={{ onChange: clienteSchema.shape.limiteCredito }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Limite de Crédito"
                    type="number"
                    getFieldError={getFieldError}
                    inputSize="medium"
                    disabled={readOnly}
                  />
                )}
              </form.Field>
            </div>
          </div>

          <div className="flex flex-wrap items-start gap-4">
            {tipoPessoa === TipoPessoa.FISICA && (
              <div className="w-fit">
                <form.Field
                  name="sexo"
                  validators={{ onChange: clienteSchema.shape.sexo }}
                >
                  {(field) => (
                    <SexoSelect
                      name={field.name}
                      error={getFieldError(field.name, field.state.meta.errors)}
                      value={field.state.value ?? ""}
                      onChange={(val) => field.handleChange(val)}
                      inputSize="medium"
                      disabled={readOnly}
                    />
                  )}
                </form.Field>
              </div>
            )}
            <div className="w-fit">
              <form.Field
                name="dataNascimento"
                validators={{ onChange: clienteSchema.shape.dataNascimento }}
              >
                {(field) => {
                  const error = getFieldError(field.name, field.state.meta.errors);
                  return (
                    <Field data-invalid={!!error} className="w-48">
                      <FieldLabel htmlFor={field.name}>
                        {tipoPessoa === TipoPessoa.FISICA ? "Data de Nascimento" : "Data de Fundação"}
                      </FieldLabel>
                      <DatePicker
                        id={field.name}
                        name={field.name}
                        onBlur={field.handleBlur}
                        value={field.state.value}
                        onChange={(val) => field.handleChange(val || "")}
                        disabled={readOnly}
                        className={cn("h-8", error && "border-destructive")}
                      />
                      {error && <FieldError>{error}</FieldError>}
                    </Field>
                  );
                }}
              </form.Field>
            </div>
          </div>

          <div className="flex flex-wrap items-start gap-4">
            <div className="flex-2 min-w-50">
              <form.Field
                name="logradouro"
                validators={{ onChange: clienteSchema.shape.logradouro }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Logradouro"
                    getFieldError={getFieldError}
                    inputSize="full"
                    disabled={readOnly}
                  />
                )}
              </form.Field>
            </div>
            <div className="w-32 shrink-0">
              <form.Field
                name="numero"
                validators={{ onChange: clienteSchema.shape.numero }}
              >
                {(field) => (
                  <FormFieldUI
                    field={field}
                    label="Número"
                    getFieldError={getFieldError}
                    inputSize="full"
                    disabled={readOnly}
                  />
                )}
              </form.Field>
            </div>
            <div className="flex-1">
              <form.Field
                name="bairroId"
                validators={{ onChange: clienteSchema.shape.bairroId }}
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
          validators={{ onChange: clienteSchema.shape.observacao }}
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
