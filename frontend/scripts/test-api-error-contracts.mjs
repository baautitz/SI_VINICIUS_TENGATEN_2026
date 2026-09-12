import assert from "node:assert/strict";
import {
  ApiRequestError,
  createApiRequestError,
  extractApiErrors,
  getApiErrorMessage,
} from "../src/utils/api-error.ts";

const skuPayload = {
  success: false,
  data: null,
  errors: [
    {
      code: "SKU_COM_MOVIMENTACAO_ESTOQUE",
      message: "Não é possível remover os SKUs 51, 52, 53.",
      field: null,
    },
    {
      code: "VALIDATION_ERROR",
      message: "Preço inválido.",
      field: "Skus[0].Preco",
    },
  ],
};

const parsed = extractApiErrors(JSON.stringify(skuPayload));
assert.equal(parsed.globalError, skuPayload.errors[0].message);
assert.equal(parsed.fieldErrors["skus[0].preco"], "Preço inválido.");
assert.equal(parsed.errors[0].code, "SKU_COM_MOVIMENTACAO_ESTOQUE");

const typed = createApiRequestError(400, JSON.stringify(skuPayload));
assert.ok(typed instanceof ApiRequestError);
assert.equal(typed.status, 400);
assert.equal(typed.message, skuPayload.errors[0].message);
assert.equal(
  extractApiErrors(typed).fieldErrors["skus[0].preco"],
  "Preço inválido.",
);

const network = createApiRequestError(0, new Error("socket offline"));
assert.equal(network.status, 0);
assert.match(network.message, /Sem conexão com o servidor/);

const quietLookup = createApiRequestError(404, "", true);
assert.equal(quietLookup.suppressToast, true);

const http200Failure = createApiRequestError(200, {
  success: false,
  data: null,
  errors: [{ code: "REGRA", message: "Operação rejeitada.", field: null }],
});
assert.equal(http200Failure.status, 200);
assert.equal(http200Failure.message, "Operação rejeitada.");

const validation422 = createApiRequestError(422, {
  errors: [{ code: "VALIDATION_ERROR", message: "Campo inválido.", field: "Nome" }],
});
assert.equal(validation422.status, 422);
assert.equal(extractApiErrors(validation422).fieldErrors.nome, "Campo inválido.");

const server500 = createApiRequestError(500, "Falha interna do servidor.");
assert.equal(server500.status, 500);
assert.equal(server500.message, "Falha interna do servidor.");

const empty400 = createApiRequestError(400, "");
assert.match(empty400.message, /Não foi possível concluir/);

const rawObject = { status: 400, response: JSON.stringify(skuPayload) };
assert.equal(extractApiErrors(rawObject).globalError, skuPayload.errors[0].message);
assert.notEqual(getApiErrorMessage(rawObject), "[object Object]");

console.log("API error contracts verified.");
