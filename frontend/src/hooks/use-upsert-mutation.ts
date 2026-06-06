import React from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { extractApiErrors } from "@/utils/api-error";

interface UseUpsertMutationOptions<TValue, TResponse> {
  mutationFn: (value: TValue) => Promise<TResponse>;
  queryKey: string[] | string[][];
  onSuccessCallback?: (data?: TResponse) => void;
  onClose?: () => void;
}

export interface BackendError {
  field?: string;
  message?: string;
}

export interface BackendResult<TData = unknown> {
  success?: boolean;
  errors?: BackendError[];
  data?: TData; // Standardize to match backend Resultado<T>
}

export function useUpsertMutation<TValue, TResponse = BackendResult>({
  mutationFn,
  queryKey,
  onSuccessCallback,
  onClose,
}: UseUpsertMutationOptions<TValue, TResponse>) {
  const [backendFieldErrors, setBackendFieldErrors] = React.useState<
    Record<string, string>
  >({});
  const [globalError, setGlobalError] = React.useState<string | null>(null);

  const queryClient = useQueryClient();

  const mutation = useMutation<TResponse, unknown, TValue>({
    mutationFn,
    onSuccess: async (res) => {
      const typedRes = res as BackendResult<unknown>;
      if (!typedRes || typedRes.success || typedRes.success === undefined) {
        // Handle both single key array ["key"] or multiple keys [["key1"], ["key2"]]
        const keysToInvalidate = Array.isArray(queryKey[0]) 
          ? (queryKey as string[][]) 
          : [queryKey as string[]];

        await Promise.all(
          keysToInvalidate.map(key => queryClient.invalidateQueries({ queryKey: key }))
        );

        onSuccessCallback?.(res);
        onClose?.();
      } else if (typedRes.errors) {
        const apiErrors = extractApiErrors(res);
        setBackendFieldErrors(apiErrors.fieldErrors);
        if (apiErrors.globalError) setGlobalError(apiErrors.globalError);
      }
    },
    onError: (e: unknown) => {
      const apiErrors = extractApiErrors(e);
      setBackendFieldErrors(apiErrors.fieldErrors);
      if (apiErrors.globalError) setGlobalError(apiErrors.globalError);
    },
  });

  const resetErrors = () => {
    setBackendFieldErrors({});
    setGlobalError(null);
  };

  const getFieldError = (name: string, formErrors: unknown[]) => {
    const formError = formErrors?.[0];
    return formError
      ? (formError as { message?: string })?.message || String(formError)
      : backendFieldErrors[name.toLowerCase()] || undefined;
  };

  return {
    mutation,
    backendFieldErrors,
    globalError,
    getFieldError,
    resetErrors,
  };
}
