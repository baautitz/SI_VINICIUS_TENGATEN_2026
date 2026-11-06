import { useRef } from "react";
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
