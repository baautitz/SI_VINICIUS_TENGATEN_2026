import { z, type ZodErrorMap } from "zod";

/**
 * Keep validation feedback consistent across every feature form.
 *
 * Schema-provided messages continue to win. This map only replaces Zod's
 * default (English) messages, so a field such as `z.number().min(1,
 * "Categoria é obrigatória.")` keeps its domain-specific text.
 */
const portugueseErrorMap: ZodErrorMap = (issue, context) => {
  if (issue.code === "invalid_type") {
    if (issue.received === "null") {
      return { message: "Selecione uma opção válida." };
    }

    if (issue.received === "undefined") {
      return { message: "Este campo é obrigatório." };
    }

    if (issue.expected === "number") {
      return { message: "Informe um número válido." };
    }

    if (issue.expected === "string") {
      return { message: "Preencha este campo." };
    }

    if (issue.expected === "boolean") {
      return { message: "Informe uma opção válida." };
    }

    const expectedMessages: Record<string, string> = {
      array: "Informe uma lista válida.",
      bigint: "Informe um número inteiro válido.",
      date: "Informe uma data válida.",
      object: "Informe um valor válido.",
      function: "Informe uma opção válida.",
    };

    if (expectedMessages[issue.expected]) {
      return { message: expectedMessages[issue.expected] };
    }

    return { message: "Informe um valor válido." };
  }

  if (issue.code === "invalid_date") {
    return { message: "Informe uma data válida." };
  }

  if (issue.code === "invalid_enum_value") {
    return { message: "Selecione uma opção válida." };
  }

  if (issue.code === "invalid_literal") {
    return { message: "Informe um valor válido." };
  }

  if (issue.code === "invalid_string") {
    const messages: Record<string, string> = {
      email: "Informe um e-mail válido.",
      url: "Informe uma URL válida.",
      uuid: "Informe um UUID válido.",
      datetime: "Informe uma data e hora válidas.",
      date: "Informe uma data válida.",
      time: "Informe um horário válido.",
    };

    if (typeof issue.validation === "string" && messages[issue.validation]) {
      return { message: messages[issue.validation] };
    }

    return { message: "Informe um texto válido." };
  }

  if (issue.code === "too_small") {
    if (issue.type === "string") {
      return {
        message: issue.minimum === 1
          ? "Preencha este campo."
          : `Informe pelo menos ${issue.minimum} caracteres.`,
      };
    }

    if (issue.type === "array") {
      return { message: `Adicione pelo menos ${issue.minimum} item(ns).` };
    }

    return { message: `O valor deve ser maior ou igual a ${issue.minimum}.` };
  }

  if (issue.code === "too_big") {
    if (issue.type === "string") {
      return { message: `Informe no máximo ${issue.maximum} caracteres.` };
    }

    if (issue.type === "array") {
      return { message: `Adicione no máximo ${issue.maximum} item(ns).` };
    }

    return { message: `O valor deve ser menor ou igual a ${issue.maximum}.` };
  }

  if (issue.code === "unrecognized_keys") {
    return { message: "Existem campos não reconhecidos." };
  }

  if (issue.code === "invalid_union" || issue.code === "invalid_union_discriminator") {
    return { message: "Informe um valor válido." };
  }

  if (issue.code === "invalid_arguments") {
    return { message: "Os dados informados são inválidos." };
  }

  if (issue.code === "invalid_return_type") {
    return { message: "O resultado informado é inválido." };
  }

  if (issue.code === "invalid_intersection_types") {
    return { message: "Os dados informados são incompatíveis." };
  }

  if (issue.code === "custom") {
    return { message: "Informe um valor válido." };
  }

  if (issue.code === "not_multiple_of") {
    return { message: `O valor deve ser múltiplo de ${issue.multipleOf}.` };
  }

  if (issue.code === "not_finite") {
    return { message: "Informe um número válido." };
  }

  return { message: context.defaultError };
};

z.setErrorMap(portugueseErrorMap);
