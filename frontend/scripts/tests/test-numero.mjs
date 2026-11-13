import assert from "node:assert/strict";
import { comMilhares, editarNumero } from "../../src/lib/numero.ts";

assert.equal(comMilhares(""), "");
assert.equal(comMilhares("123"), "123");
assert.equal(comMilhares("1234"), "1.234");
assert.equal(comMilhares("1234567,5"), "1.234.567,5");
assert.equal(comMilhares("-1234,50"), "-1.234,50");
assert.equal(comMilhares("-123"), "-123");
assert.equal(comMilhares(",5"), ",5");

const editar = (texto, cursor, anterior, opcoes = {}, digitado, tipo) => {
  const r = editarNumero(texto, cursor, anterior, { decimais: 2, ...opcoes }, digitado, tipo);
  return [r.valor, r.formatado, r.cursor];
};

// Digitar no fim: o cursor acompanha quando nasce um ponto de milhar.
assert.deepEqual(editar("9999", 4, "999", {}, "9", "insertText"), ["9999", "9.999", 5]);

// Digitar no meio: "1.2|34,50" + "9" -> cursor logo depois do 9.
assert.deepEqual(editar("1.2934,50", 4, "1234,50", {}, "9", "insertText"), ["12934,50", "12.934,50", 4]);

// Digitar no inicio de um valor com milhar.
assert.deepEqual(editar("91.234", 1, "1234", {}, "9", "insertText"), ["91234", "91.234", 1]);

// Backspace no meio: "12.3|45" apaga o 3 e o ponto some sem deslocar o cursor logico.
assert.deepEqual(editar("12.45", 3, "12345", {}, null, "deleteContentBackward"), ["1245", "1.245", 3]);

// Backspace logo depois do ponto de milhar: apaga o digito anterior ao ponto.
assert.deepEqual(editar("1234,50", 1, "1234,50", {}, null, "deleteContentBackward"), ["234,50", "234,50", 0]);

// Delete logo antes do ponto de milhar: apaga o digito seguinte ao ponto.
assert.deepEqual(editar("1234,50", 1, "1234,50", {}, null, "deleteContentForward"), ["134,50", "134,50", 1]);

// Apagar a virgula junta as duas partes num inteiro.
assert.deepEqual(editar("1.23450", 5, "1234,50", {}, null, "deleteContentBackward"), ["123450", "123.450", 5]);

// Ponto digitado vira virgula; os pontos de milhar da tela nao viram.
assert.deepEqual(editar("1.234.", 6, "1234", {}, ".", "insertText"), ["1234,", "1.234,", 6]);
assert.deepEqual(editar("12.34", 3, "1234", {}, ".", "insertText"), ["12,34", "12,34", 3]);

// Segunda virgula e ignorada; casas alem do limite sao cortadas.
assert.deepEqual(editar("12,3,4", 5, "12,34", {}, ",", "insertText"), ["12,34", "12,34", 4]);
assert.deepEqual(editar("1,239", 5, "1,23", {}, "9", "insertText"), ["1,23", "1,23", 4]);
assert.deepEqual(editar("1,923", 3, "1,23", {}, "9", "insertText"), ["1,92", "1,92", 3]);

// Casas decimais configuraveis e campo inteiro.
assert.deepEqual(editar("1,2345", 6, "1,234", { decimais: 4 }, "5", "insertText"), ["1,2345", "1,2345", 6]);
assert.deepEqual(editar("12,", 3, "12", { decimais: 0 }, ",", "insertText"), ["12", "12", 2]);

// Negativo: so quando permitido e so na frente.
assert.deepEqual(editar("-1234", 1, "1234", { negativo: true }, "-", "insertText"), ["-1234", "-1.234", 1]);
assert.deepEqual(editar("-1234", 1, "1234", {}, "-", "insertText"), ["1234", "1.234", 0]);
assert.deepEqual(editar("12-34", 3, "1234", { negativo: true }, "-", "insertText"), ["1234", "1.234", 3]);
assert.deepEqual(editar("-", 1, "", { negativo: true }, "-", "insertText"), ["-", "-", 1]);

// Colar valor formatado e com lixo.
assert.deepEqual(editar("R$ 1.234,56", 11, "", {}, null, "insertFromPaste"), ["1234,56", "1.234,56", 8]);

// Letra nao muda o valor nem move o cursor.
assert.deepEqual(editar("1.2x34", 4, "1234", {}, "x", "insertText"), ["1234", "1.234", 3]);

// Apagar tudo.
assert.deepEqual(editar("", 0, "1234,50", {}, null, "deleteContentBackward"), ["", "", 0]);

console.log("ok");
