export interface ApiError {
  field?: string
  message?: string
}

export interface ApiErrorResponse {
  errors?: ApiError[]
}

/**
 * Utilitário para abstrair o parsing de erros da API gerados pelo NSwag
 * e erros de constraint do Postgres, garantindo que o código de `upsert`
 * fique limpo.
 *
 * @param error O objeto de erro retornado pelo `catch`
 * @param setGlobalError Função para setar um erro global no modal
 * @param setFieldErrors Função para setar mapa de erros nos campos (key/value)
 * @returns true se algum erro foi processado, false se não houve erro identificável
 */
export function extractApiErrors(
  error: unknown,
  setGlobalError: (msg: string | null) => void,
  setFieldErrors: (errors: Record<string, string>) => void
): boolean {
  const errObj = error as { message?: string, status?: number, response?: string, result?: { errors?: unknown[] } }
  console.error("Form Error Details:", {
    message: errObj?.message,
    status: errObj?.status,
    response: errObj?.response,
    result: errObj?.result,
  })

  // Limpa estados iniciais
  setGlobalError(null)
  setFieldErrors({})

  const processErrorsArray = (errors: unknown[]) => {
    const errorsMap: Record<string, string> = {}
    let hasGlobal = false

    errors.forEach((err: unknown) => {
      const e = err as { field?: string; message?: string }
      if (e.field) {
        errorsMap[e.field.toLowerCase()] = e.message || "Erro"
      } else {
        setGlobalError(e.message || "Erro de validação no servidor.")
        hasGlobal = true
      }
    })

    if (Object.keys(errorsMap).length > 0) {
      setFieldErrors(errorsMap)
    }

    return Object.keys(errorsMap).length > 0 || hasGlobal
  }

  // 1. Result processado pelo NSwag (ApiException com DTO Result)
  if (errObj?.result && errObj.result.errors) {
    if (processErrorsArray(errObj.result.errors)) return true
  }

  // 2. Erros de validação brutos (caso falhe o parse nativo e venha como string JSON em response)
  if (errObj?.response) {
    try {
      const errorData = JSON.parse(errObj.response)
      if (errorData.errors && Array.isArray(errorData.errors)) {
        if (processErrorsArray(errorData.errors)) return true
      }
    } catch {
      // Ignora falha de parse JSON
    }

    // Fallbacks para constraints do Postgres
    const respStr = errObj.response as string
    if (
      respStr.includes("23505") ||
      respStr.includes("unique constraint") ||
      respStr.includes("já existe")
    ) {
      setGlobalError("Este registro já existe (duplicado).")
      return true
    }
  }

  // Fallback genérico
  setGlobalError(errObj?.message || "Erro ao processar requisição no servidor.")
  return true
}
