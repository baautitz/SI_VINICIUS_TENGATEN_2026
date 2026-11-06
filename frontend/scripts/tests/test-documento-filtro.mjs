import assert from "node:assert/strict";
import { documentoDeContaPagar, documentoDeContaReceber, contraparteParaApi } from "../../src/features/financeiro/components/documento-filtro.ts";

// Dois fornecedores com o mesmo numero de documento manual geram documentos distintos.
{
  const base = { numeroDocumento: "NF-1", origemTipo: "MANUAL", origemId: null };
  const a = documentoDeContaPagar({ ...base, fornecedor: { id: 1 } });
  const b = documentoDeContaPagar({ ...base, fornecedor: { id: 2 } });
  assert.notDeepEqual(a, b);
  assert.deepEqual(contraparteParaApi(a), { fornecedorId: 1, clienteId: undefined, transportadoraId: undefined });
}

// Reembolso (cliente) e frete de terceiro (transportadora) usam a propria contraparte.
{
  const base = { numeroDocumento: "X", origemTipo: "MANUAL" };
  assert.deepEqual(contraparteParaApi(documentoDeContaPagar({ ...base, clienteId: 5 })), { fornecedorId: undefined, clienteId: 5, transportadoraId: undefined });
  assert.deepEqual(contraparteParaApi(documentoDeContaPagar({ ...base, transportadoraId: 9 })), { fornecedorId: undefined, clienteId: undefined, transportadoraId: 9 });
}

// Contas a receber usam o cliente.
{
  const d = documentoDeContaReceber({ numeroDocumento: "V-1", origemTipo: "VENDA", origemId: 7, cliente: { id: 3 } });
  assert.equal(d.origemId, 7);
  assert.equal(contraparteParaApi(d).clienteId, 3);
}

// Sem documento nao ha filtro de contraparte.
assert.deepEqual(contraparteParaApi(null), { fornecedorId: undefined, clienteId: undefined, transportadoraId: undefined });
console.log("ok");
