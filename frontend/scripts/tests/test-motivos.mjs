import assert from "node:assert/strict";
import {
  diferencaDoItem,
  erroDaDivergencia,
  geraNfeDeBaixa,
  itensComBaixa,
  motivosDaDivergencia,
  motivosFalta,
  motivosSaidaManual,
  motivosSobra,
  resumoDoFechamento,
} from "../../src/features/estoque/motivos.ts";

// Só perda, furto e vencimento geram NF-e de baixa.
for (const m of ["PERDA_AVARIA", "FURTO_ROUBO", "VENCIMENTO"]) assert.equal(geraNfeDeBaixa(m), true, m);
for (const m of ["ERRO_DE_CONTAGEM", "OUTROS", "", null, undefined]) assert.equal(geraNfeDeBaixa(m), false, String(m));

// Listas de motivos: erro de contagem só existe no balanço; falta nunca é "outros"; sobra nunca é perda.
assert.deepEqual(motivosSaidaManual, ["PERDA_AVARIA", "FURTO_ROUBO", "VENCIMENTO"]);
assert.deepEqual(motivosFalta, ["PERDA_AVARIA", "FURTO_ROUBO", "VENCIMENTO", "ERRO_DE_CONTAGEM"]);
assert.deepEqual(motivosSobra, ["ERRO_DE_CONTAGEM", "OUTROS"]);
assert.deepEqual(motivosDaDivergencia(-1), motivosFalta);
assert.deepEqual(motivosDaDivergencia(2), motivosSobra);
assert.deepEqual(motivosDaDivergencia(0), []);

const item = (sistema, contada, motivo, recontado = false) => ({
  sku: "SKU-1",
  quantidadeSistema: sistema,
  quantidadeContada: contada,
  motivoDivergencia: motivo,
  recontado,
});

// Diferença: contada menos saldo, sem ruído de ponto flutuante; null enquanto não contado.
assert.equal(diferencaDoItem(item(10, 8)), -2);
assert.equal(diferencaDoItem(item(0.3, 0.1)), -0.2);
assert.equal(diferencaDoItem(item(10, undefined)), null);
assert.equal(diferencaDoItem(item(10, null)), null);

// Sem divergência ou sem contagem, nada a justificar.
assert.equal(erroDaDivergencia(item(10, 10, null)), null);
assert.equal(erroDaDivergencia(item(10, null, null)), null);

// Falta: exige motivo; aceita os quatro de perda; recusa "outros"; não exige recontagem.
assert.match(erroDaDivergencia(item(10, 8, null)), /motivo da falta/);
for (const m of motivosFalta) assert.equal(erroDaDivergencia(item(10, 8, m)), null, m);
assert.match(erroDaDivergencia(item(10, 8, "OUTROS")), /falta só pode/);

// Sobra: exige motivo, motivo de contagem ou outros, e recontagem.
assert.match(erroDaDivergencia(item(10, 12, null, true)), /motivo da sobra/);
assert.match(erroDaDivergencia(item(10, 12, "PERDA_AVARIA", true)), /sobra só pode/);
assert.match(erroDaDivergencia(item(10, 12, "ERRO_DE_CONTAGEM", false)), /recontagem/);
assert.equal(erroDaDivergencia(item(10, 12, "ERRO_DE_CONTAGEM", true)), null);
assert.equal(erroDaDivergencia(item(10, 12, "OUTROS", true)), null);

const lista = [item(10, 8, "PERDA_AVARIA"), item(10, 9, "ERRO_DE_CONTAGEM"), item(10, 12, "ERRO_DE_CONTAGEM", true), item(10, 10, null)];
assert.equal(itensComBaixa(lista).length, 3);

// Resumo do fechamento: erros das divergências; emitente só quando há baixa a emitir.
assert.deepEqual(resumoDoFechamento([item(10, 10, null)], null), { erros: [], exigeEmitente: false });
assert.deepEqual(resumoDoFechamento([item(10, 9, "ERRO_DE_CONTAGEM")], 3), { erros: [], exigeEmitente: true });
assert.deepEqual(resumoDoFechamento([item(10, 12, "OUTROS", true)], null), { erros: ["Informe o emitente da NF-e de ajuste de estoque."], exigeEmitente: true });
assert.deepEqual(resumoDoFechamento([item(10, 8, "PERDA_AVARIA")], 3), { erros: [], exigeEmitente: true });
assert.deepEqual(resumoDoFechamento([item(10, 8, "PERDA_AVARIA")], null), { erros: ["Informe o emitente da NF-e de ajuste de estoque."], exigeEmitente: true });
assert.equal(resumoDoFechamento([item(10, 8, null), item(10, 12, "OUTROS", false)], null).erros.length, 2);
// Com divergência sem motivo, o erro do item vem antes do pedido de emitente.
assert.equal(resumoDoFechamento([item(10, 8, "PERDA_AVARIA"), item(10, 8, null)], null).erros.length, 1);

console.log("ok");
