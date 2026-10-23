import { z } from "zod";
import type { OrigemTitulo } from "../origem";
import type { Cliente } from "@/features/parceiros/clientes/types";
import type { CondicaoPagamento } from "@/features/financeiro/condicoes/types";
import type { StatusTituloFinanceiro } from "../contas-pagar/types";

export interface ContasReceber {
  id: number;
  origemTipo: OrigemTitulo;
  origemId?: number | null;
  numeroDocumento: string;
  numeroParcela: number;
  totalParcelas: number;
  descricao: string;
  cliente?: Cliente | null;
  dataEmissao?: string | null;
  dataVencimento: string;
  valor: number;
  valorPago: number;
  valorSaldo: number;
  status: StatusTituloFinanceiro;
  condicaoPagamento?: CondicaoPagamento | null;
  observacao?: string | null;
  criadoEm: string;
}

const observacao = z
  .string()
  .max(500, "Observação deve ter no máximo 500 caracteres.")
  .nullable()
  .optional();
const descricao = z
  .string()
  .min(1, "Descrição é obrigatória.")
  .max(150, "Descrição deve ter no máximo 150 caracteres.");

export const contasReceberParcelaSchema = z.object({
  dataVencimento: z.string().min(1, "Data de vencimento é obrigatória."),
  valor: z.coerce.number().positive("Valor da parcela deve ser maior que zero."),
});

export const contasReceberSchema = z.object({
  descricao,
  clienteId: z
    .number({ required_error: "Cliente é obrigatório." })
    .min(1, "Cliente é obrigatório."),
  numeroDocumento: z.string().max(30, "Documento deve ter no máximo 30 caracteres.").nullable().optional(),
  dataEmissao: z.string().nullable().optional(),
  condicaoPagamentoId: z.number().nullable().optional(),
  observacao,
  parcelas: z.array(contasReceberParcelaSchema).min(1, "Informe pelo menos uma parcela."),
});
export type ContasReceberFormValues = z.infer<typeof contasReceberSchema>;

export const contasReceberEdicaoSchema = z.object({
  descricao,
  dataVencimento: z.string().min(1, "Data de vencimento é obrigatória."),
  valor: z.coerce.number().positive("Valor deve ser maior que zero."),
  observacao,
});
export type ContasReceberEdicaoValues = z.infer<typeof contasReceberEdicaoSchema>;
