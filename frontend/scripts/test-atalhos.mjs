import assert from "node:assert/strict";
import { ATALHOS } from "../src/lib/atalhos.ts";

const entradas = Object.entries(ATALHOS);
assert.ok(entradas.length > 0);

const vistos = new Map();
for (const [id, { combinacao, rotulo }] of entradas) {
  assert.ok(rotulo.length > 0, `${id} sem rotulo`);
  const chave = combinacao.toLowerCase();
  assert.ok(!vistos.has(chave), `${combinacao} repetida em ${vistos.get(chave)} e ${id}`);
  vistos.set(chave, id);
}

assert.equal(ATALHOS.novo.combinacao, "Alt+N");
assert.equal(ATALHOS.baixarXml.combinacao, "Alt+Shift+D");
assert.equal(ATALHOS.verProdutos.combinacao, "Alt+Shift+P");
