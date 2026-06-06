import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { UnidadeMedida, UnidadeMedidaFormValues, UnidadeMedidaResumo } from "@/features/catalogo/unidades-medida/types";
import type { Marca, MarcaFormValues, MarcaResumo } from "@/features/catalogo/marcas/types";
import type { Categoria, CategoriaFormValues, CategoriaResumo } from "@/features/catalogo/categorias/types";
import type { SkuAtributoChave, SkuAtributoChaveFormValues, SkuAtributoChaveResumo } from "@/features/catalogo/atributos/types";
import type { Produto, ProdutoFormValues, ProdutoResumo } from "@/features/catalogo/produtos/types";

export const unidadesMedidaApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<UnidadeMedidaResumo>>(
      `/api/catalogo/unidades-medida?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<UnidadeMedida>(`/api/catalogo/unidades-medida/${id}`),
  create: (data: UnidadeMedidaFormValues) => http.post<Resultado<UnidadeMedida>>("/api/catalogo/unidades-medida", data),
  update: (id: number, data: UnidadeMedidaFormValues) => http.put<Resultado<UnidadeMedida>>(`/api/catalogo/unidades-medida/${id}`, data),
  delete: (id: number) => http.delete(`/api/catalogo/unidades-medida/${id}`),
};

export const marcasApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<MarcaResumo>>(
      `/api/catalogo/marcas?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<Marca>(`/api/catalogo/marcas/${id}`),
  create: (data: MarcaFormValues) => http.post<Resultado<Marca>>("/api/catalogo/marcas", data),
  update: (id: number, data: MarcaFormValues) => http.put<Resultado<Marca>>(`/api/catalogo/marcas/${id}`, data),
  delete: (id: number) => http.delete(`/api/catalogo/marcas/${id}`),
};

export const categoriasApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<CategoriaResumo>>(
      `/api/catalogo/categorias?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<Categoria>(`/api/catalogo/categorias/${id}`),
  create: (data: CategoriaFormValues) => http.post<Resultado<Categoria>>("/api/catalogo/categorias", data),
  update: (id: number, data: CategoriaFormValues) => http.put<Resultado<Categoria>>(`/api/catalogo/categorias/${id}`, data),
  delete: (id: number) => http.delete(`/api/catalogo/categorias/${id}`),
};

export const atributosApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<SkuAtributoChaveResumo>>(
      `/api/catalogo/atributos?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<SkuAtributoChave>(`/api/catalogo/atributos/${id}`),
  create: (data: SkuAtributoChaveFormValues) => http.post<Resultado<SkuAtributoChave>>("/api/catalogo/atributos", data),
  update: (id: number, data: SkuAtributoChaveFormValues) => http.put<Resultado<SkuAtributoChave>>(`/api/catalogo/atributos/${id}`, data),
  delete: (id: number) => http.delete(`/api/catalogo/atributos/${id}`),
};

export const produtosApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<ProdutoResumo>>(
      `/api/catalogo/produtos?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<Produto>(`/api/catalogo/produtos/${id}`),
  create: (data: ProdutoFormValues) => http.post<Resultado<Produto>>("/api/catalogo/produtos", data),
  update: (id: number, data: ProdutoFormValues) => http.put<Resultado<Produto>>(`/api/catalogo/produtos/${id}`, data),
  delete: (id: number) => http.delete(`/api/catalogo/produtos/${id}`),
};

export interface SkuResumo {
  sku: string;
  gtinEan?: string | null;
  preco: number;
  estoque: number;
  custoMedio: number;
  custoUltimaCompra: number;
  ativo: boolean;
}

export const skusApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<SkuResumo>>(
      `/api/catalogo/skus?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getBySku: (sku: string) => http.get<SkuResumo>(`/api/catalogo/skus/${sku}`),
};


