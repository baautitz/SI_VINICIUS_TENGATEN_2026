import { z } from "zod";
import type { Fornecedor } from "@/features/parceiros/fornecedores/types";
import type { CondicaoPagamento } from "@/features/financeiro/condicoes/types";

export type StatusTituloFinanceiro = "ABERTO" | "PARCIAL" | "PAGO" | "CANCELADO";

export const statusTituloLabels: Record<StatusTituloFinanceiro, string> = {
  ABERTO: "Aberto",
  PARCIAL: "Parcial",
  PAGO: "Pago",
  CANCELADO: "Cancelado",
};

export interface ContasPagarParcela {
  id?: number;
  contaPagarId?: number;
  numeroParcela: number;
  dataVencimento: string;
  valorParcela: number;
  valorPago: number;
  status: StatusTituloFinanceiro;
}

export interface ContasPagar {
  id: number;
  descricao: string;
  dataEmissao?: string | null;
  dataVencimento?: string | null;
  valorOriginal: number;
  valorSaldo: number;
  status: StatusTituloFinanceiro;
  observacao?: string | null;
  criadoEm: string;
  fornecedor: Fornecedor;
  nfeId?: number | null;
  condicaoPagamento?: CondicaoPagamento | null;
  contasPagarParcelas: ContasPagarParcela[];
}

export const contasPagarParcelaSchema = z.object({
  numeroParcela: z.number().min(1),
  dataVencimento: z.string().min(1, "Data de vencimento é obrigatória."),
  valorParcela: z.coerce.number().positive("Valor da parcela deve ser maior que zero."),
  valorPago: z.coerce.number().min(0).default(0),
  status: z.enum(["ABERTO", "PARCIAL", "PAGO", "CANCELADO"]).default("ABERTO"),
});

export const contasPagarBaseSchema = z.object({
  descricao: z
    .string()
    .min(1, "Descrição é obrigatória.")
    .max(150, "Descrição deve ter no máximo 150 caracteres."),
  fornecedorId: z
    .number({ required_error: "Fornecedor é obrigatório." })
    .min(1, "Fornecedor é obrigatório."),
  nfeId: z.number().nullable().optional(),
  dataEmissao: z.string().nullable().optional(),
  valorOriginal: z.coerce
    .number({ invalid_type_error: "Valor original deve ser um número." })
    .positive("Valor original deve ser maior que zero."),
  condicaoPagamentoId: z.number().nullable().optional(),
  observacao: z
    .string()
    .max(500, "Observação deve ter no máximo 500 caracteres.")
    .nullable()
    .optional(),
  parcelas: z.array(contasPagarParcelaSchema),
});

export const contasPagarSchema = contasPagarBaseSchema.superRefine((data, ctx) => {
  if (!data.parcelas || data.parcelas.length === 0) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["parcelas"],
      message: "A conta deve possuir pelo menos uma parcela.",
    });
    return;
  }

  const totalParcelas = data.parcelas.reduce((sum, p) => sum + p.valorParcela, 0);
  if (Math.abs(totalParcelas - data.valorOriginal) > 0.01) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["parcelas"],
      message: `A soma das parcelas (R$ ${totalParcelas.toFixed(2)}) deve ser exatamente igual ao valor original da conta (R$ ${data.valorOriginal.toFixed(2)}).`,
    });
  }

  const numeros = data.parcelas.map((p) => p.numeroParcela);
  const repetidos = numeros.filter((n, idx) => numeros.indexOf(n) !== idx);
  if (repetidos.length > 0) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      path: ["parcelas"],
      message: "Não pode haver parcelas com números repetidos.",
    });
  }
});

export type ContasPagarFormValues = z.infer<typeof contasPagarSchema>;

