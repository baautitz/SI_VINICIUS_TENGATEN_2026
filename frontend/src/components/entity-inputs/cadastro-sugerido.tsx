"use client";

import { Pencil } from "lucide-react";
import { TipoPessoa } from "@/api/types";
import { Badge, Button } from "@/ui/primitives";
import type { ZodTypeAny } from "zod";

/** Dados de uma pessoa sugerida por outra fonte (XML, importação, API...). */
export interface CadastroSugerido {
  documento?: string | null;
  nome: string;
  fantasia?: string | null;
}

/** Todas as mensagens de erro do cadastro novo (sugerido + edição) contra o schema do formulário, uma por linha, ou null. */
export function erroDoCadastro(schema: ZodTypeAny, parte: CadastroSugerido, editado?: object) {
  // O cadastro editado manda null em opcionais (ex.: dataNascimento), que o schema do formulário não aceita.
  const limpo = Object.fromEntries(Object.entries(editado ?? {}).filter(([, v]) => v !== null));
  const r = schema.safeParse({ ...valoresDoCadastroSugerido(parte), ...limpo });
  if (r.success) return null;
  return [...new Set(r.error.issues.map((i) => i.message))].join("\n");
}

/** Valores iniciais do cadastro novo a partir dos dados sugeridos. */
export function valoresDoCadastroSugerido(p: CadastroSugerido) {
  const doc = (p.documento ?? "").replace(/\D/g, "");
  return {
    tipoPessoa: doc.length === 11 ? TipoPessoa.FISICA : TipoPessoa.JURIDICA,
    nomeRazaosocial: p.nome,
    apelidoNomefantasia: p.fantasia ?? "",
    cpfCnpj: p.documento ?? "",
    nacionalidadeId: 0, // sem país escolhido o cadastro é inválido, igual ao formulário aberto
  };
}

/** Cadastro encontrado (existente) ou o que será criado (dados do XML), com botão Editar. */
export function CadastroSugeridoCard(props: {
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
      {erro && <span className="text-destructive text-xs whitespace-pre-line">{erro}{"\n"}Clique em Editar para corrigir.</span>}
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
