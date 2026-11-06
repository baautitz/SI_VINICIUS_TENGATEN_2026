import assert from "node:assert/strict";
import {
  descontoLinha,
  descontoUnitarioDe,
  descontoUnitarioExcedePreco,
  ratearCompra,
  valorLiquidoItem,
} from "../src/features/compras/types.ts";

// desconto por unidade: 10 un a 5,00 com 0,50 de desconto cada = 45,00
assert.equal(descontoLinha(10, 0.5), 5);
assert.equal(valorLiquidoItem(10, 5, 0.5), 45);
assert.equal(valorLiquidoItem(3, 10, 0), 30);

// arredondamento em 2 casas
assert.equal(valorLiquidoItem(3, 10, 0.3333), 29);

// reabrir: desconto total gravado volta a desconto unitario sem perda no total
const gravado = 1; // 3 un, desconto total 1,00
const unit = descontoUnitarioDe(gravado, 3);
assert.equal(descontoLinha(3, unit), gravado);
assert.equal(descontoUnitarioDe(5, 0), 0);

// validacao
assert.equal(descontoUnitarioExcedePreco(5, 5), false);
assert.equal(descontoUnitarioExcedePreco(5, 5.01), true);

// total geral e rateio refletem o desconto unitario
const valores = [valorLiquidoItem(10, 5, 0.5), valorLiquidoItem(2, 20, 0)]; // 45 e 40
assert.deepEqual(valores, [45, 40]);
assert.equal(valores.reduce((s, v) => s + v, 0), 85);
const rat = ratearCompra(valores, 8.5);
assert.deepEqual(rat, [4.5, 4]);
assert.equal(rat.reduce((s, v) => s + v, 0), 8.5);
console.log("ok");
