import { z } from "zod";
import type { MetodoPagamento } from "../metodos/types";

export interface CondicaoPagamentoParcela {
  id?: number;
  numeroParcela: number;
  percentual: number;
  prazoDias: number;
}

export interface CondicaoPagamento {
  id: number;
  descricao: string;
  metodoPagamento: MetodoPagamento;
  entradaMinimaPercentual: number;
  descontoPercentual: number;
  acrescimoPercentual: number;
  multaPercentual: number;
  taxaJurosPercentual: number;
  ativo: boolean;
  condicoesPagamentosParcelas: CondicaoPagamentoParcela[];
}

export const condicaoPagamentoParcelaSchema = z.object({
  numeroParcela: z
    .number()
    .min(1, "O número da parcela deve ser maior que zero."),
  percentual: z.coerce
    .number()
    .min(0.0001, "O percentual deve ser maior que zero.")
    .max(100, "O percentual não pode exceder 100%."),
  prazoDias: z.coerce.number().min(0, "O prazo em dias não pode ser negativo."),
});

export const condicaoPagamentoBaseSchema = z.object({
  descricao: z
    .string()
    .min(1, "Descrição é obrigatória.")
    .max(150, "Descrição deve ter no máximo 150 caracteres."),
  metodoPagamentoCodigo: z
    .string({ required_error: "Método de pagamento é obrigatório." })
    .min(1, "Método de pagamento é obrigatório."),
  entradaMinimaPercentual: z.coerce
    .number()
    .min(0, "Entrada mínima não pode ser negativa.")
    .max(100, "Entrada mínima não pode exceder 100%."),
  descontoPercentual: z.coerce
    .number()
    .min(0, "Desconto não pode ser negativo.")
    .max(100, "Desconto não pode exceder 100%."),
  acrescimoPercentual: z.coerce
    .number()
    .min(0, "Acréscimo não pode ser negativo."),
  multaPercentual: z.coerce.number().min(0, "Multa não pode ser negativa."),
  taxaJurosPercentual: z.coerce
    .number()
    .min(0, "Taxa de juros não pode ser negativa."),
  ativo: z.boolean().default(true),
  parcelas: z.array(condicaoPagamentoParcelaSchema),
});

export const condicaoPagamentoSchema = condicaoPagamentoBaseSchema.superRefine(
  (data, ctx) => {
    if (data.descontoPercentual > 0 && data.acrescimoPercentual > 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["descontoPercentual"],
        message:
          "Não é permitido definir desconto e acréscimo simultaneamente.",
      });
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["acrescimoPercentual"],
        message:
          "Não é permitido definir desconto e acréscimo simultaneamente.",
      });
    }

    if (data.entradaMinimaPercentual === 100) {
      if (data.parcelas.length > 0) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["parcelas"],
          message:
            "Uma condição de pagamento à vista não deve possuir parcelas.",
        });
      }
      return;
    }

    if (data.parcelas.length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["parcelas"],
        message: "Pelo menos uma parcela é obrigatória para condições a prazo.",
      });
      return;
    }

    const totalPercentual = data.parcelas.reduce(
      (sum, p) => sum + p.percentual,
      0,
    );

    if (totalPercentual <= 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["parcelas"],
        message: "O percentual total das parcelas deve ser maior que zero.",
      });
    }

    if (data.entradaMinimaPercentual + totalPercentual !== 100) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["parcelas"],
        message: `A soma da entrada mínima e das parcelas deve ser exatamente igual a 100%. Atual: ${(data.entradaMinimaPercentual + totalPercentual).toFixed(2)}%.`,
      });
    }

    const numeros = data.parcelas.map((p) => p.numeroParcela);
    const repetidos = numeros.filter(
      (n, index) => numeros.indexOf(n) !== index,
    );
    if (repetidos.length > 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["parcelas"],
        message: "Não pode haver parcelas com números repetidos.",
      });
    }

    for (let i = 1; i < data.parcelas.length; i++) {
      const atual = data.parcelas[i];
      const anterior = data.parcelas[i - 1];
      if (atual.prazoDias <= anterior.prazoDias) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ["parcelas", i, "prazoDias"],
          message: `Prazo deve ser maior que ${anterior.prazoDias} dias (parcela #${anterior.numeroParcela}).`,
        });
      }
    }
  },
);

export type CondicaoPagamentoFormValues = z.infer<
  typeof condicaoPagamentoSchema
>;
