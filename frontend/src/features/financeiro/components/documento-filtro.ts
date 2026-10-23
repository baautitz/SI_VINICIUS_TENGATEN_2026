// Documento (origem + número) que agrupa as parcelas relacionadas de um título; usado para listar só as parcelas dele.
export interface DocumentoFiltro {
  numeroDocumento: string;
  origemTipo: string;
  origemId: number | null;
}
