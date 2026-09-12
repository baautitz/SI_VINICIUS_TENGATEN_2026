import {
  ApiRequestError,
  createApiRequestError,
} from "@/utils/api-error";

export const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

async function request<T>(
  path: string,
  options?: RequestInit,
  silentStatuses: readonly number[] = [],
): Promise<T> {
  try {
    const res = await fetch(`${API_URL}${path}`, {
      headers: { "Content-Type": "application/json" },
      ...options,
    });

    if (!res.ok) {
      const body = await res.text();
      throw createApiRequestError(
        res.status,
        body,
        silentStatuses.includes(res.status),
      );
    }

    const text = await res.text();
    if (!text) return undefined as unknown as T;

    let body: unknown;
    try {
      body = JSON.parse(text);
    } catch {
      throw createApiRequestError(res.status, text);
    }

    if (
      typeof body === "object" &&
      body !== null &&
      "success" in body &&
      body.success === false
    ) {
      throw createApiRequestError(res.status, body);
    }

    return body as T;
  } catch (error: unknown) {
    if (error instanceof ApiRequestError) throw error;
    throw createApiRequestError(0, error);
  }
}

export const http = {
  get: <T>(path: string) => request<T>(path),
  // Some lookups deliberately use a miss as a control-flow signal (for
  // example, SKU input falls back to its selector after a 404). Keep those
  // expected statuses out of the global toast channel while preserving the
  // rejected Promise for the caller to handle.
  getQuietly: <T>(path: string, silentStatuses: readonly number[] = [404]) =>
    request<T>(path, undefined, silentStatuses),
  post: <T>(path: string, data: unknown) => request<T>(path, { method: "POST", body: JSON.stringify(data) }),
  put: <T>(path: string, data: unknown) => request<T>(path, { method: "PUT", body: JSON.stringify(data) }),
  delete: (path: string) => request<void>(path, { method: "DELETE" }),
};
