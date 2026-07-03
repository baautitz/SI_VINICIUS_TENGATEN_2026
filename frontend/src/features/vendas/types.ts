import { z } from "zod";
import type { Cliente } from "@/features/parceiros/clientes/types";
import type { Emitente } from "@/features/parceiros/emitentes/types";
import type { Sku } from "@/features/catalogo/skus/types";

export interface VendaItem {
  id?: number;
  vendaId?: number;
  sku: string;
  quantidade: number;
  valorUnitario: number;
  precoFinal: number;
  percentualDesconto: number;
  valorDesconto: number;
  valorTotal: number;
  // UI helper fields
  produtoNome: string;
  unidadeMedidaSigla: string;
  permiteDecimais: boolean;
  estoqueAtual: number;
}

export interface VendasResumo {
  id: number;
  dataVenda: string;
  valorTotal: number;
  clienteNome: string;
  emitenteNome: string;
  quantidadeItens: number;
}

export interface Venda {
  id: number;
  dataVenda: string;
  valorTotal: number;
  observacao?: string | null;
  emitente: Emitente;
  cliente: Cliente;
  vencimentos?: string;
  itens: Array<{
    id: number;
    sku: Sku;
    quantidade: number;
    valorUnitario: number;
    valorDesconto: number;
    valorTotal: number;
  }>;
}

export const vendaItemSchema = z.object({
  sku: z.string().min(1, "SKU é obrigatório."),
  quantidade: z.coerce.number().positive("Quantidade deve ser maior que zero."),
  valorUnitario: z.coerce.number().min(0, "Preço unitário não pode ser negativo."),
  percentualDesconto: z.coerce
    .number()
    .min(0, "Desconto não pode ser negativo.")
    .max(100, "Desconto não pode ser maior que 100%."),
  valorDesconto: z.coerce.number().min(0),
  valorTotal: z.coerce.number().min(0),
  // UI helper fields
  produtoNome: z.string(),
  unidadeMedidaSigla: z.string(),
  permiteDecimais: z.boolean(),
  estoqueAtual: z.coerce.number(),
});

export const vendaParcelaSchema = z.object({
  numeroParcela: z.number().min(1),
  dataVencimento: z.string().min(1, "Data de vencimento é obrigatória."),
  valorParcela: z.coerce.number().positive("Valor da parcela deve ser maior que zero."),
});

export const vendaBaseSchema = z.object({
  dataVenda: z.string().min(1, "Data da venda é obrigatória."),
  clienteId: z
    .number({ required_error: "Cliente é obrigatório." })
    .min(1, "Cliente é obrigatório."),
  emitenteId: z
    .number({ required_error: "Emitente é obrigatório." })
    .min(1, "Emitente é obrigatório."),
  observacao: z
    .string()
    .max(500, "Observação deve ter no máximo 500 caracteres.")
    .nullable()
    .optional(),
  itens: z.array(vendaItemSchema).min(1, "A venda deve conter ao menos um item."),
  condicaoPagamentoId: z
    .number({ required_error: "Método/Condição de Pagamento é obrigatório." })
    .min(1, "Método/Condição de Pagamento é obrigatório."),
  parcelas: z
    .array(vendaParcelaSchema)
    .min(1, "A condição de pagamento selecionada deve gerar parcelas correspondentes."),
});

export const vendaSchema = vendaBaseSchema.superRefine((data, ctx) => {
  // Check stock availability
  data.itens.forEach((item, index) => {
    if (item.quantidade > item.estoqueAtual) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["itens", index, "quantidade"],
        message: `Estoque insuficiente. Disponível: ${item.estoqueAtual.toFixed(item.permiteDecimais ? 4 : 0)}.`,
      });
    }
  });

  const totalVenda = data.itens.reduce((sum, item) => sum + (item.quantidade * item.valorUnitario - item.valorDesconto), 0);

  if (data.condicaoPagamentoId && data.parcelas && data.parcelas.length > 0) {
    const totalParcelas = data.parcelas.reduce((sum, p) => sum + p.valorParcela, 0);
    if (Math.abs(totalParcelas - totalVenda) > 0.01) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["parcelas"],
        message: `A soma das parcelas (R$ ${totalParcelas.toFixed(2)}) deve ser igual ao valor total da venda (R$ ${totalVenda.toFixed(2)}).`,
      });
    }
  }
});

export type VendaFormValues = z.infer<typeof vendaSchema>;
