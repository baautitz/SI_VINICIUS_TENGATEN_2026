import { Badge } from "@/ui/primitives";
import type { StatusDevolucao } from "./types";

const config = {
  NENHUMA: { variant: "success", label: "Confirmada" },
  PARCIAL: { variant: "info", label: "Devolvida parcialmente" },
  TOTAL: { variant: "destructive", label: "Devolvida" },
} as const satisfies Record<StatusDevolucao, { variant: "success" | "info" | "destructive"; label: string }>;

export function StatusVendaBadge({ status }: { status?: StatusDevolucao }) {
  const { variant, label } = config[status ?? "NENHUMA"];
  return <Badge variant={variant}>{label}</Badge>;
}
