import assert from "node:assert/strict";
import {
  ehAVista,
  entradaFaltante,
  pagamentosCompletos,
  parcelasDaLinha,
  restante,
  trocoDaLinha,
} from "../../src/features/vendas/pagamentos.ts";

const aVista = { entradaMinimaPercentual: 100, condicoesPagamentosParcelas: [] };
const duasX = {
  entradaMinimaPercentual: 0,
  condicoesPagamentosParcelas: [
    { percentual: 50, prazoDias: 30 },
    { percentual: 50, prazoDias: 60 },
  ],
};
const duasXEntrada30 = { ...duasX, entradaMinimaPercentual: 30 };

assert.equal(ehAVista(aVista), true);
assert.equal(ehAVista(duasX), false);

// restante
assert.equal(restante(100, [{ condicao: aVista, valor: 30 }]), 70);
assert.equal(restante(100, [{ condicao: aVista, valor: 30 }, { condicao: duasX, valor: 70 }]), 0);
assert.equal(restante(0.3, [{ condicao: aVista, valor: 0.1 }, { condicao: aVista, valor: 0.2 }]), 0);
assert.equal(restante(100, [{ condicao: aVista, valor: 120 }]), -20);

// finalizar so com restante 0, condicao e valor > 0
assert.equal(pagamentosCompletos(100, [{ condicao: aVista, valor: 100 }]), true);
assert.equal(pagamentosCompletos(100, [{ condicao: aVista, valor: 60 }]), false);
assert.equal(pagamentosCompletos(100, [{ condicao: null, valor: 100 }]), false);
assert.equal(pagamentosCompletos(100, [{ condicao: aVista, valor: 100 }, { condicao: duasX, valor: 0 }]), false);
assert.equal(pagamentosCompletos(0, [{ condicao: aVista, valor: 0 }]), false);

// entrada minima sobre a soma das linhas a vista
assert.equal(entradaFaltante(100, [{ condicao: duasXEntrada30, valor: 100 }]), 30);
assert.equal(entradaFaltante(100, [{ condicao: aVista, valor: 20 }, { condicao: duasXEntrada30, valor: 80 }]), 10);
assert.equal(entradaFaltante(100, [{ condicao: aVista, valor: 15 }, { condicao: aVista, valor: 15 }, { condicao: duasXEntrada30, valor: 70 }]), 0);
assert.equal(entradaFaltante(100, [{ condicao: aVista, valor: 100 }]), 0);

// troco
assert.equal(trocoDaLinha(30, 50), 20);
assert.equal(trocoDaLinha(30, 20), 0);

// parcelas
assert.deepEqual(parcelasDaLinha(aVista, 30, "2026-01-10"), [{ numeroParcela: 1, dataVencimento: "2026-01-10", valorParcela: 30 }]);
const p = parcelasDaLinha(duasX, 70.01, "2026-01-10");
assert.equal(p.length, 2);
assert.equal(p[0].dataVencimento, "2026-02-09");
assert.equal(p[1].dataVencimento, "2026-03-11");
assert.equal(Math.round((p[0].valorParcela + p[1].valorParcela) * 100), 7001);

console.log("test-vendas-pagamentos: ok");
