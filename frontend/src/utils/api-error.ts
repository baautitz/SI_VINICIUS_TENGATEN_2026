export interface ApiErrorItem {
  code: string;
  message: string;
  field: string | null;
}

export interface ApiErrorResult {
  errors: ApiErrorItem[];
  fieldErrors: Record<string, string>;
  globalError: string | null;
}

export interface ApiErrorEnvelope {
  success?: boolean;
  data?: unknown;
  errors?: unknown;
  message?: unknown;
}

export class ApiRequestError extends Error {
  readonly status: number;
  readonly responseBody: unknown;
  readonly errors: ApiErrorItem[];
  readonly suppressToast: boolean;

  constructor(
    status: number,
    responseBody: unknown,
    errors: ApiErrorItem[],
    suppressToast = false,
  ) {
    const fallback =
      status === 0
        ? "Sem conexão com o servidor. Verifique se o servidor está online."
        : status >= 500
          ? "Ocorreu um erro inesperado no servidor."
          : "Não foi possível concluir a operação.";
    const message =
      status === 0
        ? fallback
        : errors.find((error) => !error.field)?.message ??
          errors[0]?.message ??
          fallback;

    super(message);
    this.name = "ApiRequestError";
    this.status = status;
    this.responseBody = responseBody;
    this.errors = errors;
    this.suppressToast = suppressToast;
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function asText(value: unknown): string | null {
  return typeof value === "string" && value.trim() ? value.trim() : null;
}

function normalizeField(field: unknown): string | null {
  const value = asText(field);
  return value ? value.toLowerCase() : null;
}

function readErrorItem(value: unknown): ApiErrorItem | null {
  if (typeof value === "string" && value.trim()) {
    return { code: "ERRO_API", message: value.trim(), field: null };
  }

  if (!isRecord(value)) return null;

  const message =
    asText(value.message) ??
    asText(value.detail) ??
    asText(value.title);
  if (!message) return null;

  return {
    code: asText(value.code) ?? "ERRO_API",
    message,
    field: normalizeField(value.field),
  };
}

function parseJson(value: unknown): unknown {
  if (typeof value !== "string" || !value.trim()) return value;
  try {
    return JSON.parse(value);
  } catch {
    return value;
  }
}

function collectErrors(value: unknown, target: ApiErrorItem[]): void {
  const parsed = parseJson(value);
  if (Array.isArray(parsed)) {
    parsed.forEach((item) => collectErrors(item, target));
    return;
  }

  const item = readErrorItem(parsed);
  if (item) {
    target.push(item);
    return;
  }

  if (!isRecord(parsed)) return;

  if (parsed.errors !== undefined) collectErrors(parsed.errors, target);
  if (parsed.result !== undefined) collectErrors(parsed.result, target);

  // ASP.NET validation errors may be returned as { Field: ["message"] }.
  for (const [field, messages] of Object.entries(parsed)) {
    if (
      field === "errors" ||
      field === "result" ||
      field === "data" ||
      field === "status" ||
      field === "response" ||
      field === "responseBody" ||
      field === "suppressToast" ||
      field === "name" ||
      field === "message"
    ) continue;
    if (Array.isArray(messages)) {
      messages.forEach((message) => {
        const text = asText(message);
        if (text) {
          target.push({
            code: "VALIDATION_ERROR",
            message: text,
            field: normalizeField(field),
          });
        }
      });
    } else if (typeof messages === "string" && messages.trim()) {
      target.push({
        code: "VALIDATION_ERROR",
        message: messages.trim(),
        field: normalizeField(field),
      });
    }
  }
}

function deduplicateErrors(errors: ApiErrorItem[]): ApiErrorItem[] {
  const seen = new Set<string>();
  return errors.filter((error) => {
    const key = `${error.code}|${error.field ?? ""}|${error.message}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

export function extractApiErrors(error: unknown): ApiErrorResult {
  if (error instanceof ApiRequestError && error.status === 0) {
    return { errors: [], fieldErrors: {}, globalError: null };
  }

  const responseBody = isRecord(error)
    ? error.responseBody ?? error.response
    : undefined;
  const errors: ApiErrorItem[] = [];

  if (isRecord(error) && Array.isArray(error.errors)) {
    collectErrors(error.errors, errors);
  } else {
    collectErrors(error, errors);
  }
  if (responseBody !== undefined) collectErrors(responseBody, errors);

  const uniqueErrors = deduplicateErrors(errors);
  const fieldErrors: Record<string, string> = {};
  let globalError: string | null = null;

  for (const apiError of uniqueErrors) {
    if (apiError.field) {
      fieldErrors[apiError.field] = apiError.message;
    } else if (!globalError) {
      globalError = apiError.message;
    }
  }

  if (!globalError && isRecord(error)) {
    const message = asText(error.message);
    if (message && message !== "[object Object]") globalError = message;
  }

  return { errors: uniqueErrors, fieldErrors, globalError };
}

export function createApiRequestError(
  status: number,
  responseBody: unknown,
  suppressToast = false,
): ApiRequestError {
  return new ApiRequestError(
    status,
    responseBody,
    extractApiErrors(responseBody).errors,
    suppressToast,
  );
}

export function getApiErrorMessage(
  error: unknown,
  fallback = "Ocorreu um erro inesperado.",
): string {
  if (error instanceof ApiRequestError && error.status === 0) {
    return error.message;
  }

  const parsed = extractApiErrors(error);
  return parsed.globalError ?? parsed.errors[0]?.message ?? fallback;
}
