"use client";

import { Kbd, KbdGroup } from "@/ui/primitives";
import { WindowActions } from "@/imperative-ui";
import React, { useState } from "react";
import { Button } from "@/ui/primitives";
import { Field, FieldLabel, FieldError } from "@/ui/primitives";
import { Input } from "@/ui/primitives";
import { FormFieldUI } from "@/ui/composites";
import { SexoSelect } from "@/components/sexo-select";
import { DatePicker } from "@/ui/composites";
import { cn } from "@/lib/utils";
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
import { Spinner } from "@/ui/primitives";
import { useWindow, useWindowCommands } from "@/ui/imperative";

export interface EmitentesUpsertProps {
  editingItem: Emitente | null;
  readOnly?: boolean;
}

interface EmitentesUpsertFormProps {
  editingItem: Emitente | null;
  readOnly?: boolean;
}

export function EmitentesUpsert(props: EmitentesUpsertProps) {
  const { editingItem, readOnly = false } = props;
  const isEditMode = !!editingItem;

  const { data: fullItem, isLoading } = useQuery({
    queryKey: ["emitentes", "detail", editingItem?.id],
    queryFn: () => emitentesApi.getById(editingItem!.id),
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
    <EmitentesUpsertForm
      {...props}
      readOnly={readOnly}
      editingItem={isEditMode ? (fullItem ?? null) : null}
    />
  );
}

function EmitentesUpsertForm({
  editingItem,
  readOnly = false,
}: EmitentesUpsertFormProps) {
  const activeWindow = useWindow<true>();
  const [selectedPais, setSelectedPais] = useState<Pais | null>(
    editingItem?.nacionalidade ?? null,
  );
  const [tipoPessoa, setTipoPessoa] = useState<TipoPessoa>(
    editingItem?.tipoPessoa ?? TipoPessoa.JURIDICA,
  );
  const [nacionalidadeId, setNacionalidadeId] = useState<number>(
    editingItem?.nacionalidade?.id ?? 0,
  );

  const { mutation, getFieldError, resetErrors } =
    useUpsertMutation({
      mutationFn: async (value: EmitenteFormValues) => {
        return editingItem
          ? await emitentesApi.update(editingItem.id, value)
          : await emitentesApi.create(value);
      },
      queryKey: ["emitentes"],
      onSuccessCallback: () => activeWindow.resolve(true),
    });

  const form = useForm({
    defaultValues: {
      tipoPessoa: editingItem?.tipoPessoa ?? TipoPessoa.JURIDICA,
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
      inscricaoMunicipal: editingItem?.inscricaoMunicipal ?? "",
      regimeTributario: editingItem?.regimeTributario ?? "",
      sexo: editingItem?.sexo ?? "",
      dataNascimento: editingItem?.dataNascimento ? editingItem.dataNascimento.split("T")[0] : "",
      observacao: editingItem?.observacao ?? "",
      ativo: editingItem?.ativo ?? true,
    } as EmitenteFormValues,
    onSubmit: async ({ value }) => {
      resetErrors();
      const payload = {
        ...value,
        bairroId: value.bairroId || null,
        rgIe: isBrasil ? value.rgIe : "",
        sexo: value.tipoPessoa === TipoPessoa.FISICA ? value.sexo : "",
        dataNascimento: value.dataNascimento || null,
      };
      if (readOnly) return;
      try {
        await mutation.mutateAsync(payload as EmitenteFormValues);
      } catch {
        // O hook central já apresenta o erro operacional em um toast.
      }
    },
  });


  const save = React.useCallback(async () => {
    await form.handleSubmit();
  }, [form]);

  const cancel = React.useCallback(async () => {
    activeWindow.dismiss("cancel");
  }, [activeWindow]);

  const registerDirty = React.useCallback(() => {
    activeWindow.setDirtyCheck(() => form.state.isDirty);
    return () => activeWindow.setDirtyCheck(null);
  }, [activeWindow, form]);

  const commands = React.useMemo(
    () => [
      {
        id: "emitentes.save",
        hotkey: "Alt+Enter" as const,
        label: "Salvar emitente",
        enabled: !readOnly && !mutation.isPending,
        run: async (event: KeyboardEvent) => {
          event.preventDefault();
          await save();
        },
      },
    ],
    [mutation.isPending, readOnly, save],
  );

  useWindowCommands(commands);

  const isBrasil =
    selectedPais?.codigoIsoPais === "BRA" ||
    (!selectedPais && nacionalidadeId === 1);

  return (
    <div className="flex flex-col gap-4">
      <WindowActions>
        <Button
          type="button"
          variant="outline"
          onClick={cancel}
        >
          {readOnly ? "Fechar" : "Cancelar"} <Kbd>Esc</Kbd>
        </Button>
        {!readOnly && (
<form.Subscribe selector={(state) => [state.canSubmit, state.isSubmitting]}>
          {([canSubmit, isSubmitting]) => (
            <Button
              type="submit"
              form="upsert-emitentes"
              disabled={readOnly || !canSubmit || isSubmitting}
            >
              {isSubmitting ? "Salvando..." : <span className="flex items-center gap-2">Salvar <KbdGroup><Kbd>Alt</Kbd><Kbd>Enter</Kbd></KbdGroup></span>}
            </Button>
          )}
        </form.Subscribe>
)}
      </WindowActions>
      <form
        ref={registerDirty}
        id="upsert-emitentes"
        className="flex flex-col gap-6"
        onSubmit={async (e) => {
          e.preventDefault();
          e.stopPropagation();
          await save();
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
                    disabled={readOnly}
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
                    disabled={readOnly}
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
                    disabled={readOnly}
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
                    disabled={readOnly}
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
                      disabled={readOnly}
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
                      disabled={readOnly}
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
                    disabled={readOnly}
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
                    disabled={readOnly}
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
                    disabled={readOnly}
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
                  validators={{ onChange: emitenteSchema.shape.sexo }}
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
                validators={{ onChange: emitenteSchema.shape.dataNascimento }}
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
                validators={{ onChange: emitenteSchema.shape.logradouro }}
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
                validators={{ onChange: emitenteSchema.shape.numero }}
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
                      disabled={readOnly}
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
              disabled={readOnly}
            />
          )}
        </form.Field>

      </form>
    </div>
  );
}
