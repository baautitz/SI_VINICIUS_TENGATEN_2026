import { z } from "zod"
import type { TransportadoraResumo } from "@/features/logistica/transportadoras/types"
import type { Estado } from "@/features/localizacao/estados/types"

export interface VeiculoResumo {
  id: number
  placa: string
  estadoSigla: string
  marcaModelo?: string
  transportadoraNome?: string
  ativo: boolean
}

export interface Veiculo {
  id: number
  placa: string
  estadoId: number
  estado?: Estado
  transportadoraId?: number
  transportadora?: TransportadoraResumo
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
