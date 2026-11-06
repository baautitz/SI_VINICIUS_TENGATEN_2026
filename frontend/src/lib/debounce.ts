export interface Debounce<T> {
  chamar: (valor: T) => void;
  imediato: (valor: T) => void;
  cancelar: () => void;
}

export function criarDebounce<T>(executar: (valor: T) => void, atrasoMs: number): Debounce<T> {
  let timer: ReturnType<typeof setTimeout> | undefined;
  const cancelar = () => {
    clearTimeout(timer);
    timer = undefined;
  };
  return {
    chamar: (valor) => {
      cancelar();
      timer = setTimeout(() => {
        timer = undefined;
        executar(valor);
      }, atrasoMs);
    },
    imediato: (valor) => {
      cancelar();
      executar(valor);
    },
    cancelar,
  };
}
