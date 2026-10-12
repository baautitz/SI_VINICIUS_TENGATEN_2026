import { Badge } from "@/ui/primitives";
import { statusTituloLabels, type StatusTituloFinanceiro } from "../contas-pagar/types";

const variants: Record<string, "success" | "info" | "warning" | "destructive"> = {
  PAGO: "success",
  PARCIAL: "info",
  ABERTO: "warning",
  CANCELADO: "destructive",
};

export function StatusTituloBadge({ status }: { status: string }) {
  return (
    <Badge variant={variants[status] ?? "secondary"}>
      {statusTituloLabels[status as StatusTituloFinanceiro] || status}
    </Badge>
  );
}
