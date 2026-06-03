import { z } from "zod";

export interface SkuAtributoChaveValores {
  id: number;
  chaveId: number;
  valor: string;
}

export interface SkuAtributoChave {
  id: number;
  chave: string;
  skuAtributosValores: SkuAtributoChaveValores[];
}

export interface SkuAtributoChaveResumo {
  id: number;
  chave: string;
  valores: string[];
}

export function formatSkuAtributoChaveLabel(attr?: SkuAtributoChaveResumo | SkuAtributoChave | null): string {
  if (!attr) return "";
  return attr.chave;
}

export const skuAtributoChaveSchema = z.object({
  chave: z
    .string()
    .min(1, "Chave de atributo é obrigatória.")
    .max(100, "Chave deve ter no máximo 100 caracteres."),
  valores: z
    .array(z.string().min(1, "Valor não pode ser vazio.").max(150, "Cada valor deve ter no máximo 150 caracteres."))
    .min(1, "Pelo menos um valor é obrigatório."),
});

export type SkuAtributoChaveFormValues = z.infer<typeof skuAtributoChaveSchema>;
