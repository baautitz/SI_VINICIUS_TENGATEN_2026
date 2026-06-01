import { z } from "zod"
import { Bairros } from "@/api/client"

export interface BairroDto extends Omit<Bairros, "init" | "toJSON"> {
  id: number
  bairro: string
  cidadeId: number
  cidadeNome: string
  uf: string
}

export const bairroSchema = z.object({
  bairro: z.string().min(1, "Bairro é obrigatório."),
  cidadeId: z
    .number({ required_error: "Cidade é obrigatória." })
    .min(1, "Cidade é obrigatória."),
})

export type BairroFormValues = z.infer<typeof bairroSchema>
