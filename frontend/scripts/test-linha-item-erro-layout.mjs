import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const src = readFileSync(resolve(root, "src/features/vendas/upsert.tsx"), "utf8");
const failures = [];

if (/flex flex-col items-end/.test(src))
  failures.push("celulas de item nao podem empilhar a mensagem de erro (altera a altura da linha)");
if (/<span className="text-destructive text-2xs mt-0\.5/.test(src))
  failures.push("mensagem de erro do item deve usar CampoLinhaErro (sobreposta, sem ocupar altura)");
const usos = (src.match(/<CampoLinhaErro/g) ?? []).length;
if (usos < 3) failures.push(`esperado CampoLinhaErro em quantidade, desconto e preco (encontrado ${usos})`);
if (!/aria-describedby/.test(src)) failures.push("campo deve apontar para a mensagem via aria-describedby");

if (failures.length) {
  console.error(failures.join("\n"));
  process.exit(1);
}
console.log("linha de item: layout de erro ok");
