import { Badge } from "@/components/ui/badge";

interface StatusBadgeProps {
  ativo: boolean;
}

export function StatusBadge({ ativo }: StatusBadgeProps) {
  return (
    <Badge
      variant={ativo ? "default" : "secondary"}
      className={
        ativo
          ? "bg-emerald-500 hover:bg-emerald-600 text-white border-none"
          : ""
      }
    >
      {ativo ? "Ativo" : "Inativo"}
    </Badge>
  );
}
