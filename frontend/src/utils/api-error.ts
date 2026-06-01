export interface ApiErrorResult {
  fieldErrors: Record<string, string>;
  globalError: string | null;
}

/**
 * Utilitário para extrair erros da API gerada pelo NSwag.
 * Processa o padrão Result Pattern (errors[].field, errors[].message) 
 * e trata erros globais ou de banco de dados (ex: Duplicidade/23505).
 */
export function extractApiErrors(error: unknown): ApiErrorResult {
  const result: ApiErrorResult = { fieldErrors: {}, globalError: null };

  const processErrors = (errors: unknown[]) => {
    let handled = false;
    errors.forEach((err: unknown) => {
      const e = err as { field?: string; message?: string };
      if (e.field) {
        result.fieldErrors[e.field.toLowerCase()] = e.message || "Valor inválido";
        handled = true;
      } else {
        result.globalError = e.message || "Erro de validação no servidor.";
        handled = true;
      }
    });
    return handled;
  };

  const errObj = error as { result?: { errors?: unknown[] }, response?: string, message?: string };

  // 1. O NSwag costuma encapsular a resposta em erro.result quando consegue fazer parse
  if (errObj.result && errObj.result.errors) {
    if (processErrors(errObj.result.errors)) return result;
  }

  // 2. Erros crus de string no erro.response (caso não consiga dar parse no result)
  if (errObj.response) {
    try {
      const errorData = JSON.parse(errObj.response);
      if (errorData.errors && Array.isArray(errorData.errors)) {
        if (processErrors(errorData.errors)) return result;
      }
    } catch {
      // A resposta não é JSON
    }

    // Identificação por chave de restrição de banco (Unique Constraints comuns)
    const rawResponse = String(errObj.response).toLowerCase();
    if (
      rawResponse.includes("23505") || 
      rawResponse.includes("unique constraint") || 
      rawResponse.includes("já existe")
    ) {
      result.globalError = "Este registro já existe (dados duplicados).";
      return result;
    }
  }

  // 3. Fallback genérico (caso o erro não tenha os atributos de API)
  result.globalError = errObj.message || "Erro inesperado ao processar requisição no servidor.";
  return result;
}
