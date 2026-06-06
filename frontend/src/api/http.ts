import { toast } from "sonner";
import { extractApiErrors } from "@/utils/api-error";

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
          const { globalError, fieldErrors } = extractApiErrors(parsed);
          
          if (globalError) {
            toast.error(globalError);
          }

          Object.values(fieldErrors).forEach((msg) => toast.error(msg));
        } catch {
          toast.error("Erro ao processar resposta do servidor.");
        }
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
