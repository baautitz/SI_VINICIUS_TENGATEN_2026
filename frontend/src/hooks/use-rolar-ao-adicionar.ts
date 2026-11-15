import { useEffect, useRef } from "react";

/**
 * Rola até a última linha quando a lista cresce (produto adicionado), para a linha nova
 * não ficar escondida embaixo da área rolável. Não rola na montagem nem ao remover.
 * Use o ref no `TableBody`.
 */
export function useRolarAoAdicionar(quantidade: number) {
  const ref = useRef<HTMLTableSectionElement>(null);
  const anterior = useRef(quantidade);

  useEffect(() => {
    if (quantidade > anterior.current) {
      ref.current?.lastElementChild?.scrollIntoView({ block: "nearest" });
    }
    anterior.current = quantidade;
  }, [quantidade]);

  return ref;
}
