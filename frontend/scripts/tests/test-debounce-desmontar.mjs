import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { execSync } from "node:child_process";

const ler = (p) => readFileSync(new URL(`../../${p}`, import.meta.url), "utf8");
const hook = ler("src/hooks/use-debounce.ts");
const lista = ler("src/hooks/use-feature-list.ts");
const tabela = ler("src/components/ui/data-table.tsx");
const filtros = ler("src/features/financeiro/components/filtros-avancados.tsx");

// Mecanismo central: callback ref com limpeza, sem useEffect.
assert.match(hook, /export function useRefComLimpeza/);
assert.doesNotMatch(hook, /useEffect\(/);
// DataTable anexa ao campo de busca e cancela via onGlobalFilterChange.
assert.match(tabela, /ref=\{refBusca\}/);
assert.match(tabela, /cancelar: true/);
// useFeatureList trata o cancelar sem alterar estado; Enter/limpar seguem imediatos.
assert.match(lista, /opcoes\?\.cancelar[\s\S]*debounceBusca\.cancelar\(\)/);
assert.match(lista, /val === "" \|\| opcoes\?\.imediato/);
// Filtros avancados reaproveitam o mesmo hook.
assert.match(filtros, /useRefComLimpeza<HTMLButtonElement>\(debounceFiltros\.cancelar\)/);
// Consumidores nao tem timer, debounce ou cancelamento proprio.
const proprios = execSync(
  "grep -rlE 'setTimeout|criarDebounce|useRefComLimpeza' src/features || true",
  { cwd: new URL("../..", import.meta.url), encoding: "utf8" },
).split("\n").filter(Boolean);
assert.deepEqual(proprios, ["src/features/financeiro/components/filtros-avancados.tsx"]);
console.log("debounce desmontar ok");
