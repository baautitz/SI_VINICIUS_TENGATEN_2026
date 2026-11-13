import assert from "node:assert/strict";
import { ATALHOS } from "../../src/lib/atalhos.ts";

const entradas = Object.entries(ATALHOS);
assert.ok(entradas.length > 0);

const contextosDe = (c) => (Array.isArray(c) ? c : [c]);
const colidem = (a, b) => {
  const ca = contextosDe(a);
  const cb = contextosDe(b);
  return ca.includes("global") || cb.includes("global") || ca.some((x) => cb.includes(x));
};
for (const [id, { combinacao, rotulo, contexto }] of entradas) {
  assert.ok(rotulo.length > 0, `${id} sem rotulo`);
  assert.ok(contextosDe(contexto).every((c) => c.length > 0), `${id} sem contexto`);
  for (const [outroId, outro] of entradas) {
    if (outroId === id || outro.combinacao.toLowerCase() !== combinacao.toLowerCase()) continue;
    assert.ok(!colidem(contexto, outro.contexto), `${combinacao} repetida em ${id} e ${outroId} no mesmo contexto`);
  }
}

// Global colide com qualquer contexto; telas distintas nao colidem.
assert.ok(colidem("global", "venda") && colidem(["a", "b"], "b") && !colidem("a", ["b", "c"]));

assert.equal(ATALHOS.novo.combinacao, "Alt+N");
assert.equal(ATALHOS.baixarXml.combinacao, "Alt+D");
assert.equal(ATALHOS.verProdutos.combinacao, "Alt+P");
assert.equal(ATALHOS.importarXml.combinacao, "Alt+X");
assert.equal(ATALHOS.verXml.combinacao, "Alt+X");
assert.equal(ATALHOS.imprimir.combinacao, "Alt+I");
assert.equal(ATALHOS.confirmar.combinacao, "Alt+Enter");
assert.equal(ATALHOS.editarLinha.combinacao, "Alt+E");
assert.equal(ATALHOS.abrirLinha.combinacao, "Enter");
assert.equal(ATALHOS.excluirLinha.combinacao, "Delete");
assert.equal(ATALHOS.excluirLinhaBackspace.combinacao, "Backspace");
assert.equal(ATALHOS.anterior.combinacao, "Alt+ArrowLeft");
assert.equal(ATALHOS.seguinte.combinacao, "Alt+ArrowRight");
assert.equal(ATALHOS.devolucaoParcial.combinacao, "Alt+1");
assert.equal(ATALHOS.devolucaoTotal.combinacao, "Alt+2");
assert.equal(ATALHOS.reembolsos.combinacao, "Alt+Shift+B");
