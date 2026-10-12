import { Badge } from "@/ui/primitives";

interface StatusBadgeProps {
  ativo: boolean;
}

export function StatusBadge({ ativo }: StatusBadgeProps) {
  return <Badge variant={ativo ? "success" : "secondary"}>{ativo ? "Ativo" : "Inativo"}</Badge>;
}
