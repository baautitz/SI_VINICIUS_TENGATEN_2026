export interface ApiErrorResult {
  fieldErrors: Record<string, string>;
  globalError: string | null;
}

export function extractApiErrors(error: unknown): ApiErrorResult {
  const result: ApiErrorResult = { fieldErrors: {}, globalError: null };

  const processErrors = (errors: unknown[]) => {
    let handled = false;
    errors.forEach((err: unknown) => {
      const e = err as { field?: string; message?: string };
      if (e.field) {
        result.fieldErrors[e.field.toLowerCase()] =
          e.message || "Valor inválido";
        handled = true;
      } else {
        result.globalError = e.message || "Erro de validação no servidor.";
        handled = true;
      }
    });
    return handled;
  };

  const errObj = error as {
    result?: { errors?: unknown[] };
    response?: string;
    message?: string;
  };

  if (errObj.result && errObj.result.errors) {
    if (processErrors(errObj.result.errors)) return result;
  }

  if (errObj.response) {
    try {
      const errorData = JSON.parse(errObj.response);
      if (errorData.errors && Array.isArray(errorData.errors)) {
        if (processErrors(errorData.errors)) return result;
      }
    } catch {}
  }

  result.globalError =
    errObj.message || "Erro inesperado ao processar requisição no servidor.";
  return result;
}
