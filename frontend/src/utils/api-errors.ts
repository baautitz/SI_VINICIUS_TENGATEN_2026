export interface ApiError {
  field?: string;
  message?: string;
}

export interface ApiErrorResponse {
  errors?: ApiError[];
}

export function extractApiErrors(
  error: unknown,
  setGlobalError: (msg: string | null) => void,
  setFieldErrors: (errors: Record<string, string>) => void,
): boolean {
  const errObj = error as {
    message?: string;
    status?: number;
    response?: string;
    result?: { errors?: unknown[] };
  };
  console.error("Form Error Details:", {
    message: errObj?.message,
    status: errObj?.status,
    response: errObj?.response,
    result: errObj?.result,
  });

  setGlobalError(null);
  setFieldErrors({});

  const processErrorsArray = (errors: unknown[]) => {
    const errorsMap: Record<string, string> = {};
    let hasGlobal = false;

    errors.forEach((err: unknown) => {
      const e = err as { field?: string; message?: string };
      if (e.field) {
        errorsMap[e.field.toLowerCase()] = e.message || "Erro";
      } else {
        setGlobalError(e.message || "Erro de validação no servidor.");
        hasGlobal = true;
      }
    });

    if (Object.keys(errorsMap).length > 0) {
      setFieldErrors(errorsMap);
    }

    return Object.keys(errorsMap).length > 0 || hasGlobal;
  };

  if (errObj?.result && errObj.result.errors) {
    if (processErrorsArray(errObj.result.errors)) return true;
  }

  if (errObj?.response) {
    try {
      const errorData = JSON.parse(errObj.response);
      if (errorData.errors && Array.isArray(errorData.errors)) {
        if (processErrorsArray(errorData.errors)) return true;
      }
    } catch {}
  }

  setGlobalError(
    errObj?.message || "Erro ao processar requisição no servidor.",
  );
  return true;
}
