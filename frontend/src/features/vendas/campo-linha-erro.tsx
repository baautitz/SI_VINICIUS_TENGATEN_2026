import type { ReactNode } from "react";

type CampoLinhaErroProps = {
  id: string;
  mensagem?: string | null;
  children: (describedBy: string | undefined) => ReactNode;
};

/**
 * Campo de linha de tabela com mensagem de erro sobreposta: a mensagem nao
 * ocupa altura, entao a linha mantem altura e alinhamento.
 */
export function CampoLinhaErro({ id, mensagem, children }: CampoLinhaErroProps) {
  return (
    <div className="relative">
      {children(mensagem ? id : undefined)}
      {mensagem && (
        <span
          id={id}
          className="text-destructive text-2xs pointer-events-none absolute inset-x-0 -bottom-2 text-right leading-none font-semibold whitespace-nowrap"
        >
          {mensagem}
        </span>
      )}
    </div>
  );
}
