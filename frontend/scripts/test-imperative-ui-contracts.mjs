import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const read = (relativePath) =>
  readFileSync(resolve(root, relativePath), "utf8");

const controller = read("src/ui/imperative/controller.ts");
const contracts = [
  read("src/ui/contracts/primitives.ts"),
  read("src/ui/contracts/windows.ts"),
  read("src/ui/contracts/services.ts"),
].join("\n");
const provider = read("src/imperative-ui/provider.tsx");
const host = read("src/imperative-ui/host.tsx");
const commands = read("src/imperative-ui/commands.ts");
const dialog = read("src/components/ui/dialog.tsx");
const sheet = read("src/components/ui/sheet.tsx");
const alertDialog = read("src/components/ui/alert-dialog.tsx");
const dataTable = read("src/components/ui/data-table.tsx");
const keyboardNavigation = read("src/ui/keyboard-navigation.tsx");
const datePicker = read("src/components/ui/date-picker.tsx");
const combobox = read("src/components/ui/combobox.tsx");
const entityInput = read("src/components/ui/entity-input.tsx");
const multiEntityInput = read("src/components/ui/multi-entity-input.tsx");
const skuInput = read("src/components/entity-inputs/sku-input.tsx");
const http = read("src/api/http.ts");
const catalogoApi = read("src/api/catalogo.ts");
const vendasUpsert = read("src/features/vendas/upsert.tsx");
const movimentacoesUpsert = read(
  "src/features/estoque/movimentacoes/upsert-form.tsx",
);
const zodConfig = read("src/lib/zod-config.ts");
const docs = read("docs/imperative-ui.md");
const primitives = read("src/ui/primitives/index.ts");

assert.match(controller, /export class WindowController/);
assert.match(controller, /open<TResult, TProps>/);
assert.match(controller, /resolve<TResult>\(id: WindowId/);
assert.match(controller, /dismiss\(id: WindowId/);
assert.match(controller, /requestDismiss\(/);
assert.match(controller, /markDirty\(/);
assert.match(controller, /snapshot = \(\)/);
assert.match(controller, /subscribe = \(listener/);
assert.match(controller, /dispose\(\)/);
assert.match(
  controller,
  /if \(!current \|\| current\.id !== id \|\| this\.pendingClose\.has\(id\)\)/,
);
assert.match(controller, /status: "cancelled", reason: "unmounted"/);
assert.match(contracts, /export interface WindowPorts/);
assert.match(contracts, /confirmDiscard/);
assert.match(contracts, /export interface Ui/);
assert.match(contracts, /windows: WindowService/);
assert.match(contracts, /confirm\(options: ConfirmOptions\): Promise<boolean>/);
assert.match(contracts, /WindowIcon/);
assert.match(contracts, /"confirmation"/);
assert.doesNotMatch(contracts, /className\??:/);
assert.match(provider, /return result\.status === "confirmed"/);
assert.match(primitives, /ui\/adapters\/shadcn/);
assert.match(provider, /\{cancelLabel\} <Kbd>Esc<\/Kbd>/);
assert.match(
  provider,
  /<KbdGroup className="ml-2">[\s\S]*<Kbd>Alt<\/Kbd>[\s\S]*<Kbd>Enter<\/Kbd>/,
);
assert.match(provider, /data-default-confirmation="true"/);
assert.match(provider, /useWindowCommands\(commands\)/);
assert.match(provider, /hotkey: "Alt\+Enter"/);
assert.doesNotMatch(provider, /onKeyDown/);
assert.match(commands, /Control\+Enter/);
assert.match(commands, /conflictBehavior: "allow" as const/);
assert.match(commands, /conflictBehavior: "allow",/);
assert.doesNotMatch(commands, /conflictBehavior: "warn"/);

assert.doesNotMatch(host, /modal\s*=/);
assert.doesNotMatch(host, /showOverlay|hasBaseOverlay/);
assert.doesNotMatch(host, /opacity-95|pointer-events-none|\bdepth\b/);
assert.doesNotMatch(host, /!w-\[90vw\]|!max-w-\[90vw\]/);
assert.match(host, /<Dialog\s*\n\s*open/);
assert.match(host, /<Sheet\s*\n\s*open/);
assert.match(dialog, /<DialogOverlay \/>/);
assert.match(sheet, /<SheetOverlay \/>/);
assert.match(host, /data-window-titlebar/);
assert.match(host, /surface === "confirmation"/);
assert.match(host, /onOpenAutoFocus/);
assert.match(host, /focusFirstField/);
assert.match(host, /observer\.observe\(document\.body/);
assert.match(host, /Never let Radix choose the decorative close button/);
assert.match(host, /function findFirstAction/);
assert.match(
  keyboardNavigation,
  /Dialogs and sheets render through a Radix portal/,
);
assert.match(keyboardNavigation, /requestAnimationFrame\(registerWhenReady\)/);
assert.match(keyboardNavigation, /event\.key === "ArrowLeft"/);
assert.match(keyboardNavigation, /event\.key === "ArrowRight"/);
assert.match(keyboardNavigation, /event\.key === "F2"/);
assert.match(keyboardNavigation, /data-navigation-editing/);
assert.match(keyboardNavigation, /data-navigation-chip-remove='true'/);
assert.match(
  keyboardNavigation,
  /isPopupInteraction\(target\)[\s\S]*direction !== "forward"[\s\S]*direction !== "backward"/,
);
assert.match(keyboardNavigation, /export function markKeyboardFocus/);
assert.match(host, /markKeyboardFocus\(\)/);
assert.match(datePicker, /event\.key === "Enter" \|\| event\.key === " " /);
assert.match(host, /Conteúdo da janela\./);
assert.match(host, /Confirme ou cancele esta ação\./);
assert.match(alertDialog, /<AlertDialogOverlay \/>/);
assert.match(alertDialog, /max-w-xl/);
assert.match(dialog, /data-slot="dialog-content"[\s\S]*overflow-hidden/);
assert.match(
  dialog,
  /function DialogBody[\s\S]*overflow-y-auto overscroll-contain/,
);
assert.match(
  alertDialog,
  /data-slot="alert-dialog-content"[\s\S]*overflow-hidden/,
);
assert.match(
  alertDialog,
  /function AlertDialogBody[\s\S]*overflow-y-auto overscroll-contain/,
);
assert.match(sheet, /data-slot="sheet-content"[\s\S]*overflow-hidden/);
assert.match(
  sheet,
  /function SheetBody[\s\S]*overflow-y-auto overscroll-contain/,
);
assert.doesNotMatch(dialog, /grid-rows-\[auto_minmax\(0,1fr\)\]/);
assert.doesNotMatch(alertDialog, /grid-rows-\[auto_minmax\(0,1fr\)\]/);
assert.match(
  host,
  /full: "h-\[95dvh\] max-h-\[95dvh\] w-\[95dvw\] max-w-\[95dvw\]"/,
);
assert.doesNotMatch(dialog, /showOverlay/);
assert.doesNotMatch(sheet, /showOverlay/);
assert.match(dialog, /data-\[state=open\]/);
assert.match(sheet, /data-\[state=open\]/);

assert.doesNotMatch(
  dataTable,
  /MutationObserver|querySelectorAll|setTimeout|requestAnimationFrame/,
);
assert.doesNotMatch(dataTable, /handleRowKeyDown/);
assert.match(dataTable, /<InputGroupInput[\s\S]*autoFocus/);
assert.doesNotMatch(
  entityInput,
  /querySelectorAll|setTimeout|requestAnimationFrame|getElementById/,
);
assert.match(dataTable, /preventDefault: false/);
assert.match(dataTable, /stopPropagation: false/);
assert.ok(
  entityInput.indexOf("await openSelection()") <
    entityInput.indexOf("onSelectId(null)"),
  "abrir um seletor vazio não pode limpar o campo antes de abrir a janela",
);
assert.doesNotMatch(
  skuInput,
  /onSelectSku\(null\);\s*setSelectedSku\(null\);\s*const selected = await openSelector\(/,
);
assert.match(entityInput, /openingSelectorRef/);
assert.match(entityInput, /searchInFlightRef/);
assert.match(entityInput, /getSelectionSearchTerm/);
assert.match(entityInput, /visibleText === selectedLabel/);
assert.match(entityInput, /e\.key === "Enter" && !e\.altKey && !e\.ctrlKey/);
assert.match(entityInput, /title: modalTitle,\s*icon,\s*size: "full"/);
assert.match(combobox, /e\.key === "Enter" && !e\.altKey/);
assert.match(combobox, /import \{ X \} from "lucide-react"/);
assert.match(combobox, /onRemove\?: \(\) => void/);
assert.match(combobox, /event\.key !== "Delete" && event\.key !== "Backspace"/);
assert.match(combobox, /data-navigation-chip-remove="true"/);
assert.match(combobox, /tabIndex=\{-1\}/);
assert.match(
  combobox,
  /const isCursorMode = e\.currentTarget\.dataset\.navigationEditing === "true"/,
);
assert.match(combobox, /e\.key === "ArrowDown" && context\.isOpen/);
assert.match(combobox, /e\.key === "ArrowUp" && context\.isOpen/);
assert.match(multiEntityInput, /onRemove=\{\(\) => \{/);
assert.match(multiEntityInput, /removeLabel=\{`Remover \$\{displayLabel\}`\}/);
assert.match(skuInput, /selectorOpeningRef/);
assert.match(skuInput, /interactionInFlightRef/);
assert.match(skuInput, /export function parseSkuInput/);
assert.match(skuInput, /match\[2\]\.trim\(\)/);
assert.match(skuInput, /\[\+\-\]\?/);
assert.match(skuInput, /quantityIsFinal/);
assert.match(skuInput, /lookupQuantity < 0/);
assert.match(skuInput, /selectSku\(\s*selected,\s*lookupQuantity/);
assert.doesNotMatch(skuInput, /console\.error\("Erro ao buscar SKU"/);
assert.match(skuInput, /finally \{[\s\S]*refocusAfterWindow\(\)/);
assert.match(http, /getQuietly/);
assert.match(catalogoApi, /getQuietly<Sku>/);
assert.match(catalogoApi, /encodeURIComponent\(sku\)/);
assert.match(
  vendasUpsert,
  /requestAnimationFrame\(\(\) => skuInputRef\.current\?\.focus\(\)\)/,
);
assert.match(
  movimentacoesUpsert,
  /requestAnimationFrame\(\(\) => skuInputRef\.current\?\.focus\(\)\)/,
);
assert.match(vendasUpsert, /return handleRemoveItem\(existingIndex\)/);
assert.match(movimentacoesUpsert, /return removeItemRow\(existingIndex\)/);
assert.match(zodConfig, /z\.setErrorMap\(portugueseErrorMap\)/);
assert.match(zodConfig, /Selecione uma opção válida\./);
assert.match(provider, /useSyncExternalStore/);
assert.match(provider, /controller\.requestDismiss/);
assert.match(provider, /controller\.dismissAll/);
assert.match(commands, /target: options\.target/);
assert.doesNotMatch(
  commands,
  /target: options\.target\s*\?\?\s*activeWindow\?\.scopeRef/,
);
assert.match(docs, /WindowController|useUi|useWindow/i);
assert.match(docs, /cada janela|próprio overlay|overlay próprio/i);

execFileSync("node", ["scripts/check-imperative-ui-boundaries.mjs"], {
  cwd: root,
  stdio: "inherit",
});
execFileSync("node", ["scripts/test-window-controller.mjs"], {
  cwd: root,
  stdio: "inherit",
});
console.log("Contratos Imperative UI verificados.");
