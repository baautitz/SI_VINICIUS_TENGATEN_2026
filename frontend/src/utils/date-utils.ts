/**
 * Formata uma data UTC para o fuso horário local do cliente.
 * @param date Data em string (ISO 8601) ou objeto Date.
 * @param options Opções de formatação do Intl.DateTimeFormat.
 * @returns String formatada.
 */
export function formatToLocal(
  date: string | Date | null | undefined,
  options: Intl.DateTimeFormatOptions = {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }
): string {
  if (!date) return "-";

  const d = typeof date === "string" ? new Date(date) : date;

  // Se a data for inválida
  if (isNaN(d.getTime())) return "-";

  return new Intl.DateTimeFormat("pt-BR", options).format(d);
}

/**
 * Retorna apenas a data (sem hora) formatada localmente.
 */
export function formatDateToLocal(date: string | Date | null | undefined): string {
  return formatToLocal(date, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

/**
 * Retorna apenas a hora formatada localmente.
 */
export function formatTimeToLocal(date: string | Date | null | undefined): string {
  return formatToLocal(date, {
    hour: "2-digit",
    minute: "2-digit",
  });
}

/**
 * Converte uma data para "YYYY-MM-DD" usando o fuso horário local
 * (toISOString usa UTC e pode adiantar o dia após as 21h em UTC-3).
 */
export function toLocalISODate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

/** Retorna a data de hoje no fuso local como "YYYY-MM-DD". */
export function todayLocalISODate(): string {
  return toLocalISODate(new Date());
}
