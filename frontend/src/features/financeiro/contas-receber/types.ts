import { z } from "zod";
import type { Cliente } from "@/features/parceiros/clientes/types";
import type { CondicaoPagamento } from "@/features/pagamentos/condicoes/types";
import type { StatusTituloFinanceiro } from "../contas-pagar/types";

export interface ContasReceberParcela {
  id?: number;
  contaReceberId?: number;
  numeroParcela: number;
  dataVencimento: string;
  valorParcela: number;
  valorRecebido: number;
  status: StatusTituloFinanceiro;
}

export interface ContasReceber {
  id: number;
  descricao: string;
  dataEmissao?: string | null;
  dataVencimento?: string | null;
  valorOriginal: number;
  valorSaldo: number;
  status: StatusTituloFinanceiro;
  observacao?: string | null;
  criadoEm: string;
  cliente: Cliente;
  nfeId?: number | null;
  vendaId?: number | null;
  condicaoPagamento?: CondicaoPagamento | null;
  contasReceberParcelas: ContasReceberParcela[];
}

export const contasReceberParcelaSchema = z.object({
  numeroParcela: z.number().min(1),
  dataVencimento: z.string().min(1, "Data de vencimento é obrigatória."),
  valorParcela: z.coerce.number().positive("Valor da parcela deve ser maior que zero."),
  valorRecebido: z.coerce.number().min(0).default(0),
  status: z.enum(["ABERTO", "PARCIAL", "PAGO", "CANCELADO"]).default("ABERTO"),
});

export const contasReceberBaseSchema = z.object({
  descricao: z
    .string()
    .min(1, "Descrição é obrigatória.")
    .max(150, "Descrição deve ter no máximo 150 caracteres."),
  clienteId: z
    .number({ required_error: "Cliente é obrigatório." })
    .min(1, "Cliente é obrigatório."),
  nfeId: z.number().nullable().optional(),
  vendaId: z.number().nullable().optional(),
  dataEmissao: z.string().nullable().optional(),
  dataVencimento: z.string().nullable().optional(),
  valorOriginal: z.coerce
    .number({ invalid_type_error: "Valor original deve ser um número." })
    .positive("Valor original deve ser maior que zero."),
  condicaoPagamentoId: z.number().nullable().optional(),
  observacao: z
    .string()
    .max(500, "Observação deve ter no máximo 500 caracteres.")
    .nullable()
    .optional(),
  parcelas: z.array(contasReceberParcelaSchema),
});

export const contasReceberSchema = contasReceberBaseSchema.superRefine((data, ctx) => {
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

export type ContasReceberFormValues = z.infer<typeof contasReceberSchema>;
