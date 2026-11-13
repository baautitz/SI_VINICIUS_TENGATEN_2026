interface ZodLike {
  safeParse: (valor: unknown) => { success: boolean }
}

function ehZod(v: unknown): v is ZodLike {
  return typeof v === "object" && v !== null && typeof (v as ZodLike).safeParse === "function"
}

/**
 * Um campo e obrigatorio quando o schema de validacao (onChange) rejeita
 * tanto valor ausente quanto string vazia.
 */
export function campoObrigatorio(field: {
  options?: { validators?: { onChange?: unknown } }
}): boolean {
  const schema = field.options?.validators?.onChange
  if (!ehZod(schema)) return false
  return !schema.safeParse(undefined).success && !schema.safeParse("").success
}
