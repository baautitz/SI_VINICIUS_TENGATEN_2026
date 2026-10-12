import { Badge } from "@/ui/primitives";
import { statusLabels, type StatusNfe } from "./types";

const variants = {
  EMITIDA: "success",
  CANCELADA: "destructive",
  RASCUNHO: "secondary",
} as const satisfies Record<StatusNfe, "success" | "destructive" | "secondary">;

export function NfeStatusBadge({ status }: { status: StatusNfe }) {
  return <Badge variant={variants[status]}>{statusLabels[status]}</Badge>;
}
