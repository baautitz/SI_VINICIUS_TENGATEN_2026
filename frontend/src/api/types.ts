export enum TipoPessoa {
  FISICA = 1,
  JURIDICA = 2,
}

export interface PaginatedResult<T> {
  itens: T[];
  totalDeItens: number;
  totalDePaginas: number;
  paginaAtual: number;
}

export interface Resultado<T> {
  success: boolean;
  data?: T;
  errors?: ResultadoErro[];
}

export interface ResultadoErro {
  code?: string;
  field?: string;
  message?: string;
}

export interface PaisResumo {
  id: number;
  pais: string;
  codigoIsoPais: string;
}
