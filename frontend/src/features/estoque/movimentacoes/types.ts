import { z } from "zod";
import type { Sku } from "@/features/catalogo/skus/types";
import { geraNfeDeBaixa, motivosSaidaManual, type MotivoMovimentacao } from "@/features/estoque/motivos";

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
export type OrigemMovimentacao = "MANUAL" | "VENDA" | "COMPRA" | "BALANCO" | "ESTORNO" | "DEVOLUCAO_VENDA" | "DEVOLUCAO_COMPRA";

// Linha imutável do razão de estoque: o estorno é uma nova movimentação (origem ESTORNO).
export interface MovimentacaoEstoque {
  id: number;
  dataMovimentacao: string;
  tipoMovimentacao: TipoMovimentacao;
  origemTipo: OrigemMovimentacao;
  origemId?: number | null;
  motivo?: string | null;
  motivoTipo?: MotivoMovimentacao | null;
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
  DEVOLUCAO_VENDA: "Devolução de venda",
  DEVOLUCAO_COMPRA: "Devolução de compra",
};

// Só lançamento manual se estorna direto; venda, compra e balanço se estornam cancelando o documento de origem.
export function podeEstornar(m: MovimentacaoEstoque): boolean {
  return !m.estornada && m.origemTipo === "MANUAL";
}

// Aviso para uma movimentação que não se cancela direto: ela segue o documento de origem.
export function avisoNaoEstornavel(m: MovimentacaoEstoque): string {
  if (m.origemTipo === "ESTORNO")
    return "Esta movimentação é um estorno e não pode ser cancelada. Para corrigir, faça um novo lançamento manual.";
  if (m.origemTipo === "MANUAL") return "Esta movimentação já foi estornada.";

  const documento = `${origemMovimentacaoLabels[m.origemTipo]}${m.origemId ? ` #${m.origemId}` : ""}`;
  return `Movimentação gerada por ${documento}. Para cancelá-la, cancele o documento de origem.`;
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

// Lançamento manual é sempre saída: a entrada vem da compra e dos retornos (ADR 0006).
export const movimentacaoEstoqueSchema = z.object({
  motivoTipo: z.enum(motivosSaidaManual as [MotivoMovimentacao, ...MotivoMovimentacao[]], {
    required_error: "Selecione o motivo da movimentação.",
    invalid_type_error: "Selecione o motivo da movimentação.",
  }),
  emitenteId: z.number().nullable().optional(),
  usuarioId: z.number().nullable().optional(),
  motivo: z
    .string({ required_error: "Motivo é obrigatório." })
    .trim()
    .min(5, "Motivo deve ter pelo menos 5 caracteres.")
    .max(500, "Motivo deve ter no máximo 500 caracteres."),
  itens: z
    .array(movimentacaoEstoqueItemSchema)
    .min(1, "A movimentação deve conter pelo menos um produto."),
}).superRefine((valores, ctx) => {
  if (geraNfeDeBaixa(valores.motivoTipo) && !valores.emitenteId)
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["emitenteId"], message: "Informe o emitente da NF-e de baixa." });
});

export type MovimentacaoEstoqueItemFormValues = z.infer<
  typeof movimentacaoEstoqueItemSchema
>;
export type MovimentacaoEstoqueFormValues = z.infer<
  typeof movimentacaoEstoqueSchema
>;
