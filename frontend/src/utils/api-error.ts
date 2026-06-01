export interface ApiErrorResult {
  fieldErrors: Record<string, string>;
  globalError: string | null;
}

export function extractApiErrors(error: unknown): ApiErrorResult {
  const result: ApiErrorResult = { fieldErrors: {}, globalError: null };

  const addFieldError = (field: string, message: string) => {
    const lowerField = field.toLowerCase();
    result.fieldErrors[lowerField] = message;
  };

  const processErrorsArray = (errors: unknown[]) => {
    let handled = false;
    errors.forEach((err: unknown) => {
      // Formato: { field: "FieldName", message: "Error message" }
      const e = err as { field?: string; message?: string };
      if (e.field) {
        addFieldError(e.field, e.message || "Valor inválido");
        handled = true;
      } else if (e.message) {
        result.globalError = e.message;
        handled = true;
      }
    });
    return handled;
  };

  const processErrorsObject = (errorsObj: Record<string, unknown>) => {
    // Formato ASP.NET: { "FieldName": ["Error 1", "Error 2"] }
    let handled = false;
    for (const [field, messages] of Object.entries(errorsObj)) {
      if (Array.isArray(messages) && messages.length > 0) {
        addFieldError(field, String(messages[0]));
        handled = true;
      } else if (typeof messages === "string") {
        addFieldError(field, messages);
        handled = true;
      }
    }
    return handled;
  };

  const errObj = error as {
    result?: { errors?: unknown[] };
    errors?: unknown;
    response?: string;
    message?: string;
  };

  // 1. Tenta processar 'result.errors' (wrapper customizado)
  if (errObj.result && Array.isArray(errObj.result.errors)) {
    if (processErrorsArray(errObj.result.errors)) return result;
  }

  // 2. Tenta processar 'errors' diretamente se for array ou objeto
  if (errObj.errors) {
    if (Array.isArray(errObj.errors)) {
      if (processErrorsArray(errObj.errors)) return result;
    } else if (typeof errObj.errors === "object" && errObj.errors !== null) {
      if (processErrorsObject(errObj.errors as Record<string, unknown>)) return result;
    }
  }

  // 3. Tenta processar o corpo da resposta (JSON)
  if (errObj.response) {
    try {
      const errorData = JSON.parse(errObj.response);
      
      // Caso 3a: errorData.errors é um array ou objeto
      if (errorData.errors) {
        if (Array.isArray(errorData.errors)) {
          if (processErrorsArray(errorData.errors)) return result;
        } else if (typeof errorData.errors === "object" && errorData.errors !== null) {
          if (processErrorsObject(errorData.errors)) return result;
        }
      }
      
      // Caso 3b: errorData.result.errors
      if (errorData.result && Array.isArray(errorData.result.errors)) {
        if (processErrorsArray(errorData.result.errors)) return result;
      }

      // Caso 3c: Mensagem global no topo
      if (errorData.message && !result.globalError) {
        result.globalError = errorData.message;
      }
    } catch {}
  }

  if (!result.globalError && Object.keys(result.fieldErrors).length === 0) {
    result.globalError =
      errObj.message || "Erro inesperado ao processar requisição no servidor.";
  }

  return result;
}
