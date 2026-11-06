import assert from "node:assert/strict";
import { proximaOrdenacao, paramsDeOrdenacao } from "../../src/lib/ordenacao.ts";

// Clicar alterna ascendente, descendente e padrao (sem ordenacao).
{
  const asc = proximaOrdenacao(null, "status");
  assert.deepEqual(asc, { campo: "status", direcao: "asc" });
  const desc = proximaOrdenacao(asc, "status");
  assert.deepEqual(desc, { campo: "status", direcao: "desc" });
  assert.equal(proximaOrdenacao(desc, "status"), null);
}

// Clicar noutra coluna recomeca em ascendente.
{
  const outra = proximaOrdenacao({ campo: "status", direcao: "desc" }, "dataAbertura");
  assert.deepEqual(outra, { campo: "dataAbertura", direcao: "asc" });
}

// Sem escolha nao ha parametros: vale a ordenacao padrao da API.
{
  assert.equal(paramsDeOrdenacao(null), "");
  assert.equal(paramsDeOrdenacao({ campo: "dataAbertura", direcao: "desc" }), "&ordenarPor=dataAbertura&direcao=desc");
}
console.log("ordenacao ok");
