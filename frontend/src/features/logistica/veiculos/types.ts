import { z } from "zod"
import type { Transportadora } from "@/features/logistica/transportadoras/types"
import type { Estado } from "@/features/localizacao/estados/types"

export interface Veiculo {
  id: number
  placa: string
  estado: Estado
  transportadora?: Transportadora
  rntrc?: string
  renavam?: string
  tipoVeiculo?: string
  marcaModelo?: string
  ativo: boolean
  observacao?: string
}

export const veiculoSchema = z.object({
  placa: z.string().min(1, "Placa é obrigatória."),
  estadoId: z.number().min(1, "Estado é obrigatório."),
  transportadoraId: z.number().nullable().optional(),
  rntrc: z.string().optional(),
  renavam: z.string().optional(),
  tipoVeiculo: z.string().optional(),
  marcaModelo: z.string().optional(),
  observacao: z.string().optional(),
  ativo: z.boolean().default(true),
})

export type VeiculoFormValues = z.infer<typeof veiculoSchema>
