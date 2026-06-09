import { z } from "zod";

export interface SkuAtributoChave {
  id: number;
  chave: string;
  skuAtributosValores: SkuAtributoValor[];
}

export interface SkuAtributoValor {
  id: number;
  chaveId: number;
  valor: string;
}

export const skuAtributoChaveSchema = z.object({
  chave: z.string().min(1, "Chave é obrigatória.").max(100, "Máximo 100 caracteres."),
  valores: z.array(z.string()).min(1, "Pelo menos um valor é obrigatório."),
});

export type SkuAtributoChaveFormValues = z.infer<typeof skuAtributoChaveSchema>;
