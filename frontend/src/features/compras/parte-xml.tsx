"use client";

import { Pencil } from "lucide-react";
import { TipoPessoa } from "@/api/types";
import { Badge, Button } from "@/ui/primitives";
import type { ZodTypeAny } from "zod";
import type { NfeXmlParte } from "./types";

/** Primeira mensagem de erro do cadastro novo (XML + edição) contra o schema do formulário, ou null. */
export function erroDoCadastro(schema: ZodTypeAny, parte: NfeXmlParte, editado?: object) {
  const r = schema.safeParse({ ...valoresDaParteXml(parte), ...editado });
  return r.success ? null : r.error.issues[0]?.message ?? "Cadastro inválido.";
}

/** Valores iniciais do cadastro novo a partir da parte do XML (emitente/transportadora). */
export function valoresDaParteXml(p: NfeXmlParte) {
  const doc = (p.documento ?? "").replace(/\D/g, "");
  return {
    tipoPessoa: doc.length === 11 ? TipoPessoa.FISICA : TipoPessoa.JURIDICA,
    nomeRazaosocial: p.nome,
    apelidoNomefantasia: p.fantasia ?? "",
    cpfCnpj: p.documento ?? "",
    nacionalidadeId: 1,
  };
}

/** Cadastro encontrado (existente) ou o que será criado (dados do XML), com botão Editar. */
export function ParteXmlCard(props: {
  titulo: string;
  nome: string;
  documento?: string | null;
  existente: boolean;
  editado: boolean;
  onEditar: () => void;
  erro?: string | null;
}) {
  const { titulo, nome, documento, existente, editado, onEditar, erro } = props;
  return (
    <div className={`flex flex-col gap-1 rounded-lg border p-4 text-sm ${erro ? "border-destructive" : ""}`}>
      <span className="text-muted-foreground text-xs font-semibold uppercase">
        {titulo}
      </span>
      <span className="text-base font-semibold">{nome}</span>
      <span>CNPJ/CPF: {documento}</span>
      {erro && <span className="text-destructive text-xs">{erro} Clique em Editar para corrigir.</span>}
      <div className="flex items-center gap-2">
        <Badge variant={existente ? "outline" : "secondary"}>
          {existente ? "Já cadastrado" : "Será cadastrado ao confirmar"}
        </Badge>
        {editado && <Badge>Editado</Badge>}
        <Button type="button" size="sm" variant="outline" className="ml-auto" onClick={onEditar}>
          <Pencil /> Editar
        </Button>
      </div>
    </div>
  );
}
