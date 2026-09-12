import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(fileURLToPath(new URL("..", import.meta.url)));
const read = (relativePath) =>
  readFileSync(resolve(root, relativePath), "utf8");

const failures = [];
const check = (file, pattern, message) => {
  if (!pattern.test(read(file))) failures.push(`${file}: ${message}`);
};

check(
  "src/components/ui/dialog.tsx",
  /function DialogBody[\s\S]*overflow-y-auto overscroll-contain p-1/,
  "DialogBody deve ser o body rolável com margem interna segura",
);
check(
  "src/components/ui/dialog.tsx",
  /data-slot="dialog-content"[\s\S]*overflow-hidden/,
  "DialogContent não pode ser a área de rolagem",
);
check(
  "src/components/ui/dialog.tsx",
  /data-slot="dialog-header"[\s\S]*shrink-0/,
  "DialogHeader deve permanecer fixo",
);
check(
  "src/components/ui/alert-dialog.tsx",
  /function AlertDialogBody[\s\S]*overflow-y-auto overscroll-contain p-1/,
  "AlertDialogBody deve ser o body rolável com margem interna segura",
);
check(
  "src/components/ui/alert-dialog.tsx",
  /data-slot="alert-dialog-content"[\s\S]*overflow-hidden/,
  "AlertDialogContent não pode ser a área de rolagem",
);
check(
  "src/components/ui/sheet.tsx",
  /function SheetBody[\s\S]*overflow-y-auto overscroll-contain p-1/,
  "SheetBody deve ser o body rolável com margem interna segura",
);
check(
  "src/components/ui/sheet.tsx",
  /data-slot="sheet-content"[\s\S]*overflow-hidden/,
  "SheetContent não pode ser a área de rolagem",
);
check(
  "src/imperative-ui/host.tsx",
  /<AlertDialogBody>[\s\S]*<\/AlertDialogBody>/,
  "Host deve separar o body de AlertDialog",
);
check(
  "src/imperative-ui/host.tsx",
  /<SheetBody>[\s\S]*<\/SheetBody>/,
  "Host deve separar o body de Sheet",
);
check(
  "src/imperative-ui/host.tsx",
  /<DialogBody>[\s\S]*<\/DialogBody>/,
  "Host deve separar o body de Dialog",
);

const windowActionFiles = [
  "src/components/entity-inputs/sku-input.tsx",
  "src/features/vendas/index.tsx",
  "src/features/vendas/upsert.tsx",
  "src/features/financeiro/components/baixa-parcela-dialog.tsx",
  "src/features/estoque/movimentacoes/upsert-form.tsx",
];

for (const file of windowActionFiles) {
  check(file, /data-window-actions/, "janela especial sem marcador de footer");
}

if (failures.length > 0) {
  console.error("Contrato de layout das janelas violado:");
  console.error(failures.join("\n"));
  process.exitCode = 1;
} else {
  console.log("Contrato de layout das janelas verificado.");
}
