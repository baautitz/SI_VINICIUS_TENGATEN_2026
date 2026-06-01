import { z } from "zod"
import { Estados } from "@/api/client"

export interface EstadoDto extends Omit<Estados, "init" | "toJSON"> {
  id: number
  estado: string
  uf: string
  paisId: number
  paisNome: string
}

export const estadoSchema = z.object({
  estado: z.string().min(1, "Estado é obrigatório."),
  uf: z.string().min(1, "UF é obrigatória.").max(2, "UF deve ter no máximo 2 caracteres."),
  paisId: z.number({ required_error: "País é obrigatório." }).min(1, "País é obrigatório."),
})

export type EstadoFormValues = z.infer<typeof estadoSchema>
