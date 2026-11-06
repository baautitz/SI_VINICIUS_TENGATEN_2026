import { FILTROS_AVANCADOS_VAZIOS, type FiltrosAvancados } from "./filtros-avancados";
import type { DocumentoFiltro } from "./documento-filtro";

export const TODOS = "TODOS";

// Estado de filtros das telas de contas a pagar/receber; um filtro novo entra aqui e passa pelas duas telas.
export interface FiltrosTitulos {
  status: string;
  origem: string;
  documento: DocumentoFiltro | null;
  avancados: FiltrosAvancados;
}

export const FILTROS_TITULOS_VAZIOS: FiltrosTitulos = {
  status: TODOS,
  origem: TODOS,
  documento: null,
  avancados: FILTROS_AVANCADOS_VAZIOS,
};
