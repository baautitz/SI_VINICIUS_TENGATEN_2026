import { useCallback, useRef, type RefObject } from "react";
import { criarDebounce, type Debounce } from "@/lib/debounce";

// Debounce estavel entre renders, sem useEffect. A funcao mais recente e
// chamada quando o atraso vence.
export function useDebounce<T>(executar: (valor: T) => void, atrasoMs: number): Debounce<T> {
  const ultima = useRef(executar);
  ultima.current = executar;
  const debounce = useRef<Debounce<T> | null>(null);
  if (debounce.current === null) {
    debounce.current = criarDebounce<T>((v) => ultima.current(v), atrasoMs);
  }
  return debounce.current;
}

// Callback ref (React 19) cuja funcao de limpeza roda quando o elemento sai da
// arvore. E o ponto unico de cancelamento no desmontar, sem useEffect. A
// identidade e estavel: so desmonta de verdade, nao a cada render.
export function useRefComLimpeza<E>(limpar: () => void, alvo?: RefObject<E | null>) {
  const ultima = useRef(limpar);
  ultima.current = limpar;
  return useCallback(
    (el: E | null) => {
      if (alvo) alvo.current = el;
      return () => {
        if (alvo) alvo.current = null;
        ultima.current();
      };
    },
    [alvo],
  );
}
