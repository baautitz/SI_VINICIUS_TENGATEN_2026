import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const read = (p) => readFileSync(resolve(root, p), "utf8");
const failures = [];
const check = (file, pattern, message) => {
  if (!pattern.test(read(file))) failures.push(`${file}: ${message}`);
};

for (const file of [
  "src/components/ui/input.tsx",
  "src/components/ui/textarea.tsx",
]) {
  check(file, /(?<![:\w-])disabled:bg-field-disabled/, "desabilitado deve usar bg-field-disabled");
  check(file, /(?<![:\w-])read-only:bg-field-disabled/, "somente leitura deve usar bg-field-disabled");
  check(file, /dark:disabled:bg-field-disabled/, "desabilitado no tema escuro deve usar bg-field-disabled");
  check(file, /dark:read-only:bg-field-disabled/, "somente leitura no tema escuro deve usar bg-field-disabled");
}

check(
  "src/features/estoque/movimentacoes/upsert-form.tsx",
  /^(?![\s\S]*bg-muted\/50 text-foreground\/80)/,
  "codigo da movimentacao nao pode ter estilo local de campo bloqueado; usar Input readOnly",
);

if (failures.length) {
  console.error(failures.join("\n"));
  process.exit(1);
}
console.log("field-disabled contract ok");
