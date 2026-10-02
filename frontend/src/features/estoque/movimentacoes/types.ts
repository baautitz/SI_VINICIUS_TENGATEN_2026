import { z } from "zod";
import type { Sku } from "@/features/catalogo/skus/types";

export interface MovimentacaoEstoqueItem {
  id: number;
  sku: Sku;
  produtoNome: string;
  unidadeMedidaSigla: string;
  quantidade: number;
  custoUnitario: number;
  quantidadeAnterior?: number | null;
  custoMedioAnterior?: number | null;
}

export type TipoMovimentacao = "ENTRADA" | "SAIDA";
export type OrigemMovimentacao = "MANUAL" | "VENDA" | "COMPRA" | "BALANCO" | "ESTORNO";

// Linha imutável do razão de estoque: o estorno é uma nova movimentação (origem ESTORNO).
export interface MovimentacaoEstoque {
  id: number;
  dataMovimentacao: string;
  tipoMovimentacao: TipoMovimentacao;
  origemTipo: OrigemMovimentacao;
  origemId?: number | null;
  motivo?: string | null;
  observacao?: string | null;
  estornada: boolean;
  usuario?: { id: number; nome: string } | null;
  movimentacoesEstoquesItens: MovimentacaoEstoqueItem[];
  totalCusto?: number;
}

export const tipoMovimentacaoLabels: Record<string, string> = {
  ENTRADA: "Entrada",
  SAIDA: "Saída",
};

export const origemMovimentacaoLabels: Record<string, string> = {
  MANUAL: "Manual",
  VENDA: "Venda",
  COMPRA: "Compra",
  BALANCO: "Balanço",
  ESTORNO: "Estorno",
};

// Só lançamentos manuais e de balanço podem ser estornados direto; venda se estorna cancelando a venda.
export function podeEstornar(m: MovimentacaoEstoque): boolean {
  return !m.estornada && (m.origemTipo === "MANUAL" || m.origemTipo === "BALANCO");
}

export const movimentacaoEstoqueItemSchema = z.object({
  sku: z.string().min(1, "SKU é obrigatório."),
  quantidade: z
    .number({ invalid_type_error: "Deve ser um número." })
    .positive("Quantidade deve ser maior que zero."),
  custoUnitario: z
    .number({ invalid_type_error: "Deve ser um número." })
    .min(0, "Custo unitário não pode ser negativo.")
    .optional()
    .default(0),
});

export const movimentacaoEstoqueSchema = z.object({
  tipoMovimentacao: z.enum(["ENTRADA", "SAIDA"], {
    required_error: "Selecione o tipo de movimentação.",
  }),
  usuarioId: z.number().nullable().optional(),
  motivo: z
    .string({ required_error: "Motivo é obrigatório." })
    .trim()
    .min(5, "Motivo deve ter pelo menos 5 caracteres.")
    .max(500, "Motivo deve ter no máximo 500 caracteres."),
  observacao: z
    .string()
    .max(500, "Observação deve ter no máximo 500 caracteres.")
    .nullable()
    .optional(),
  itens: z
    .array(movimentacaoEstoqueItemSchema)
    .min(1, "A movimentação deve conter pelo menos um produto."),
});

export type MovimentacaoEstoqueItemFormValues = z.infer<
  typeof movimentacaoEstoqueItemSchema
>;
export type MovimentacaoEstoqueFormValues = z.infer<
  typeof movimentacaoEstoqueSchema
>;
