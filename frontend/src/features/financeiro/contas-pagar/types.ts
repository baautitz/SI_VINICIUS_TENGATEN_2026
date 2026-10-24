import { z } from "zod";
import type { OrigemTitulo } from "../origem";
import type { Fornecedor } from "@/features/parceiros/fornecedores/types";
import type { CondicaoPagamento } from "@/features/financeiro/condicoes/types";

export type StatusTituloFinanceiro = "ABERTO" | "PARCIAL" | "PAGO" | "CANCELADO";

export const statusTituloLabels: Record<StatusTituloFinanceiro, string> = {
  ABERTO: "Aberto",
  PARCIAL: "Parcial",
  PAGO: "Pago",
  CANCELADO: "Cancelado",
};

export interface ContasPagar {
  id: number;
  origemTipo: OrigemTitulo;
  origemId?: number | null;
  numeroDocumento: string;
  numeroParcela: number;
  totalParcelas: number;
  descricao: string;
  fornecedor?: Fornecedor | null;
  clienteId?: number | null;
  clienteNome?: string | null;
  transportadoraId?: number | null;
  transportadoraNome?: string | null;
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

export const contasPagarParcelaSchema = z.object({
  dataVencimento: z.string().min(1, "Data de vencimento é obrigatória."),
  valor: z.coerce.number().positive("Valor da parcela deve ser maior que zero."),
});

export const contasPagarBaseSchema = z.object({
  descricao,
  fornecedorId: z
    .number({ required_error: "Fornecedor é obrigatório." })
    .min(1, "Fornecedor é obrigatório."),
  numeroDocumento: z.string().max(30, "Documento deve ter no máximo 30 caracteres.").nullable().optional(),
  dataEmissao: z.string().nullable().optional(),
  condicaoPagamentoId: z.number().nullable().optional(),
  observacao,
  parcelas: z.array(contasPagarParcelaSchema).min(1, "Informe pelo menos uma parcela."),
});

export const contasPagarSchema = contasPagarBaseSchema;
export type ContasPagarFormValues = z.infer<typeof contasPagarSchema>;

export const contasPagarEdicaoSchema = z.object({
  descricao,
  dataVencimento: z.string().min(1, "Data de vencimento é obrigatória."),
  valor: z.coerce.number().positive("Valor deve ser maior que zero."),
  observacao,
});
export type ContasPagarEdicaoValues = z.infer<typeof contasPagarEdicaoSchema>;
