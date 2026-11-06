import assert from "node:assert/strict";
import { criarDebounce } from "../../src/lib/debounce.ts";

const esperar = (ms) => new Promise((r) => setTimeout(r, ms));

// Varias chamadas seguidas geram uma unica execucao com o ultimo valor.
{
  const chamadas = [];
  const d = criarDebounce((v) => chamadas.push(v), 30);
  d.chamar("a"); d.chamar("ab"); d.chamar("abc");
  assert.deepEqual(chamadas, []);
  await esperar(60);
  assert.deepEqual(chamadas, ["abc"]);
}

// imediato executa na hora e descarta o que estava pendente.
{
  const chamadas = [];
  const d = criarDebounce((v) => chamadas.push(v), 30);
  d.chamar("a");
  d.imediato("");
  assert.deepEqual(chamadas, [""]);
  await esperar(60);
  assert.deepEqual(chamadas, [""]);
}

// cancelar descarta a execucao pendente.
{
  const chamadas = [];
  const d = criarDebounce((v) => chamadas.push(v), 20);
  d.chamar("x");
  d.cancelar();
  await esperar(50);
  assert.deepEqual(chamadas, []);
}
console.log("debounce ok");
