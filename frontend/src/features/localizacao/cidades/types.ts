import { z } from "zod"
import { Cidades } from "@/api/client"

export interface CidadeDto extends Omit<Cidades, "init" | "toJSON"> {
  id: number
  cidade: string
  ddd: number
  estadoId: number
  estadoNome: string
  uf: string
}

export const cidadeSchema = z.object({
  cidade: z.string().min(1, "Cidade é obrigatória."),
  ddd: z.coerce
    .number({ invalid_type_error: "DDD deve ser um número." })
    .int("DDD deve ser um número inteiro.")
    .positive("DDD deve ser maior que zero."),
  estadoId: z.number({ required_error: "Estado é obrigatório." }).min(1, "Estado é obrigatório."),
})

export type CidadeFormValues = z.infer<typeof cidadeSchema>
