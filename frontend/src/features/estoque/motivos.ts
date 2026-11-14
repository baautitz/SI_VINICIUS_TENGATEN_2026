// Regras do motivo da movimentação (ADR 0006). Sem React nem imports de alias: o teste roda em Node puro.

export type MotivoMovimentacao = "PERDA_AVARIA" | "FURTO_ROUBO" | "VENCIMENTO" | "ERRO_DE_CONTAGEM" | "OUTROS";

export const motivoMovimentacaoLabels: Record<MotivoMovimentacao, string> = {
  PERDA_AVARIA: "Perda / avaria",
  FURTO_ROUBO: "Furto / roubo",
  VENCIMENTO: "Vencimento",
  ERRO_DE_CONTAGEM: "Erro de contagem",
  OUTROS: "Outros",
};

// Saída manual: erro de contagem só existe no balanço.
export const motivosSaidaManual: MotivoMovimentacao[] = ["PERDA_AVARIA", "FURTO_ROUBO", "VENCIMENTO"];
export const motivosFalta: MotivoMovimentacao[] = ["PERDA_AVARIA", "FURTO_ROUBO", "VENCIMENTO", "ERRO_DE_CONTAGEM"];
export const motivosSobra: MotivoMovimentacao[] = ["ERRO_DE_CONTAGEM", "OUTROS"];

/** Perda, furto e vencimento tiram mercadoria do estoque e exigem NF-e de baixa. */
export function geraNfeDeBaixa(motivo: string | null | undefined): boolean {
  return motivo === "PERDA_AVARIA" || motivo === "FURTO_ROUBO" || motivo === "VENCIMENTO";
}

export interface ItemDivergente {
  sku: string;
  quantidadeSistema: number;
  quantidadeContada?: number | null;
  motivoDivergencia?: string | null;
  recontado?: boolean;
}

/** Diferença contada menos saldo; null enquanto o item não foi contado. */
export function diferencaDoItem(item: ItemDivergente): number | null {
  if (item.quantidadeContada === null || item.quantidadeContada === undefined) return null;
  return Number((item.quantidadeContada - item.quantidadeSistema).toFixed(4));
}

/** Motivos que a divergência aceita: falta (diferença negativa) e sobra (positiva) têm listas diferentes. */
export function motivosDaDivergencia(diferenca: number): MotivoMovimentacao[] {
  return diferenca < 0 ? motivosFalta : diferenca > 0 ? motivosSobra : [];
}

/** O que impede o item de fechar o balanço, ou null. Espelha as regras do backend. */
export function erroDaDivergencia(item: ItemDivergente): string | null {
  const diferenca = diferencaDoItem(item);
  if (diferenca === null || diferenca === 0) return null;
  const motivo = item.motivoDivergencia as MotivoMovimentacao | null | undefined;
  if (!motivo) return `SKU ${item.sku}: informe o motivo ${diferenca < 0 ? "da falta" : "da sobra"}.`;
  if (!motivosDaDivergencia(diferenca).includes(motivo))
    return diferenca < 0
      ? `SKU ${item.sku}: falta só pode ter motivo perda/avaria, furto/roubo, vencimento ou erro de contagem.`
      : `SKU ${item.sku}: sobra só pode ter motivo erro de contagem ou outros.`;
  if (diferenca > 0 && !item.recontado) return `SKU ${item.sku}: sobra exige recontagem antes de fechar o balanço.`;
  return null;
}

/** Itens do balanço que exigem NF-e de baixa: falta por perda, furto ou vencimento. */
export function itensComBaixa(itens: ItemDivergente[]): ItemDivergente[] {
  return itens.filter((i) => (diferencaDoItem(i) ?? 0) < 0 && geraNfeDeBaixa(i.motivoDivergencia));
}

export interface ResumoFechamento {
  erros: string[];
  exigeEmitente: boolean;
}

/** O balanço só fecha com toda divergência justificada; com baixa a emitir, também com o emitente informado. */
export function resumoDoFechamento(itens: ItemDivergente[], emitenteId: number | null | undefined): ResumoFechamento {
  const erros = itens.map(erroDaDivergencia).filter((e): e is string => e !== null);
  const exigeEmitente = itensComBaixa(itens).length > 0;
  if (exigeEmitente && !emitenteId && erros.length === 0) erros.push("Informe o emitente da NF-e de baixa.");
  return { erros, exigeEmitente };
}
