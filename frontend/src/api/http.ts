import { toast } from "sonner";

export const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:8080";

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  try {
    const res = await fetch(`${API_URL}${path}`, {
      headers: { "Content-Type": "application/json" },
      ...options,
    });

    if (!res.ok) {
      const body = await res.text();
      
      if (res.status >= 500) {
        toast.error("Ocorreu um erro inesperado no servidor (Erro 500).");
      } else {
        try {
          const parsed = JSON.parse(body);
          if (parsed.errors) {
            if (Array.isArray(parsed.errors)) {
              (parsed.errors as Array<unknown>).forEach((e) => {
                if (e && typeof e === "object" && "message" in e) {
                  toast.error(String((e as { message: unknown }).message || e));
                } else {
                  toast.error(String(e));
                }
              });
            } else if (typeof parsed.errors === "object" && parsed.errors !== null) {
              Object.values(parsed.errors as Record<string, unknown>).forEach((messages) => {
                if (Array.isArray(messages)) {
                  (messages as Array<unknown>).forEach((msg) => toast.error(String(msg)));
                } else {
                  toast.error(String(messages));
                }
              });
            }
          } else if (parsed.message) {
            toast.error(parsed.message);
          } else if (parsed.result && Array.isArray(parsed.result.errors)) {
            (parsed.result.errors as Array<unknown>).forEach((e) => {
              if (e && typeof e === "object" && "message" in e) {
                toast.error(String((e as { message: unknown }).message || "Erro de validação."));
              } else {
                toast.error("Erro de validação.");
              }
            });
          }
        } catch {}
      }

      throw { status: res.status, response: body };
    }

    const text = await res.text();
    return text ? JSON.parse(text) : (undefined as unknown as T);
  } catch (error: unknown) {
    if (error && typeof error === "object" && !("status" in error)) {
      toast.error("Sem conexão com o servidor. Por favor, verifique se o servidor está online.");
    }
    throw error;
  }
}

export const http = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, data: unknown) => request<T>(path, { method: "POST", body: JSON.stringify(data) }),
  put: <T>(path: string, data: unknown) => request<T>(path, { method: "PUT", body: JSON.stringify(data) }),
  delete: (path: string) => request<void>(path, { method: "DELETE" }),
};
