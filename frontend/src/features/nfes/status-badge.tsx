import { Badge } from "@/ui/primitives";
import { statusLabels, type StatusNfe } from "./types";

const classes: Record<StatusNfe, string> = {
  EMITIDA: "bg-emerald-500 text-white border-none hover:bg-emerald-600",
  CANCELADA: "bg-destructive text-white border-none",
  RASCUNHO: "",
};

export function NfeStatusBadge({ status }: { status: StatusNfe }) {
  return (
    <Badge variant={status === "RASCUNHO" ? "secondary" : "default"} className={classes[status]}>
      {statusLabels[status]}
    </Badge>
  );
}
