import { TipoPessoa } from "@/api/types";
import { Badge } from "@/ui/primitives";

export function TipoPessoaBadge({ tipo }: { tipo: TipoPessoa }) {
  return <Badge variant="outline">{tipo === TipoPessoa.FISICA ? "Física" : "Jurídica"}</Badge>;
}
