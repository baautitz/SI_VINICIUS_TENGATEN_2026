import assert from "node:assert/strict";
import { placaValida } from "../../src/lib/placa.ts";

for (const ok of ["ABC1234", "abc-1234", "ABC1D23", "bra2e19"]) assert.equal(placaValida(ok), true, ok);
for (const ruim of ["", "ABC123", "AB12345", "ABCD123", "ABC12D3", "ABC1D234"]) assert.equal(placaValida(ruim), false, ruim);
