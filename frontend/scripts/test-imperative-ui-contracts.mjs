import assert from "node:assert/strict"
import { execFileSync } from "node:child_process"
import { readFileSync } from "node:fs"
import { dirname, resolve } from "node:path"
import { fileURLToPath } from "node:url"

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..")
const read = (relativePath) => readFileSync(resolve(root, relativePath), "utf8")

const controller = read("src/ui/imperative/controller.ts")
const contracts = [
  read("src/ui/contracts/primitives.ts"),
  read("src/ui/contracts/windows.ts"),
  read("src/ui/contracts/services.ts"),
].join("\n")
const provider = read("src/imperative-ui/provider.tsx")
const host = read("src/imperative-ui/host.tsx")
const commands = read("src/imperative-ui/commands.ts")
const dialog = read("src/components/ui/dialog.tsx")
const sheet = read("src/components/ui/sheet.tsx")
const alertDialog = read("src/components/ui/alert-dialog.tsx")
const dataTable = read("src/components/ui/data-table.tsx")
const entityInput = read("src/components/ui/entity-input.tsx")
const skuInput = read("src/components/entity-inputs/sku-input.tsx")
const zodConfig = read("src/lib/zod-config.ts")
const docs = read("docs/imperative-ui.md")
const primitives = read("src/ui/primitives/index.ts")

assert.match(controller, /export class WindowController/)
assert.match(controller, /open<TResult, TProps>/)
assert.match(controller, /resolve<TResult>\(id: WindowId/)
assert.match(controller, /dismiss\(id: WindowId/)
assert.match(controller, /requestDismiss\(/)
assert.match(controller, /markDirty\(/)
assert.match(controller, /snapshot = \(\)/)
assert.match(controller, /subscribe = \(listener/)
assert.match(controller, /dispose\(\)/)
assert.match(controller, /if \(!current \|\| current\.id !== id \|\| this\.pendingClose\.has\(id\)\)/)
assert.match(controller, /status: "cancelled", reason: "unmounted"/)
assert.match(contracts, /export interface WindowPorts/)
assert.match(contracts, /confirmDiscard/)
assert.match(contracts, /export interface Ui/)
assert.match(contracts, /windows: WindowService/)
assert.match(contracts, /confirm\(options: ConfirmOptions\): Promise<boolean>/)
assert.match(contracts, /WindowIcon/)
assert.match(contracts, /"confirmation"/)
assert.doesNotMatch(contracts, /className\??:/)
assert.match(provider, /return result\.status === "confirmed"/)
assert.match(primitives, /ui\/adapters\/shadcn/)

assert.doesNotMatch(host, /modal\s*=/)
assert.doesNotMatch(host, /showOverlay|hasBaseOverlay/)
assert.doesNotMatch(host, /opacity-95|pointer-events-none|\bdepth\b/)
assert.doesNotMatch(host, /!w-\[90vw\]|!max-w-\[90vw\]/)
assert.match(host, /<Dialog\s*\n\s*open/)
assert.match(host, /<Sheet\s*\n\s*open/)
assert.match(dialog, /<DialogOverlay \/>/)
assert.match(sheet, /<SheetOverlay \/>/)
assert.match(host, /data-window-titlebar/)
assert.match(host, /surface === "confirmation"/)
assert.match(host, /onOpenAutoFocus/)
assert.match(host, /focusFirstField/)
assert.match(host, /Conteúdo da janela\./)
assert.match(host, /Confirme ou cancele esta ação\./)
assert.match(alertDialog, /<AlertDialogOverlay \/>/)
assert.match(alertDialog, /max-w-md/)
assert.doesNotMatch(dialog, /showOverlay/)
assert.doesNotMatch(sheet, /showOverlay/)
assert.match(dialog, /data-\[state=open\]/)
assert.match(sheet, /data-\[state=open\]/)

assert.doesNotMatch(dataTable, /MutationObserver|querySelectorAll|setTimeout|requestAnimationFrame/)
assert.doesNotMatch(entityInput, /querySelectorAll|setTimeout|requestAnimationFrame|getElementById/)
assert.match(dataTable, /preventDefault: false/)
assert.match(dataTable, /stopPropagation: false/)
assert.ok(
  entityInput.indexOf("await openSelection()") < entityInput.indexOf("onSelectId(null)"),
  "abrir um seletor vazio não pode limpar o campo antes de abrir a janela",
)
assert.doesNotMatch(
  skuInput,
  /onSelectSku\(null\);\s*setSelectedSku\(null\);\s*const selected = await openSelector\(/,
)
assert.match(entityInput, /openingSelectorRef/)
assert.match(entityInput, /searchInFlightRef/)
assert.match(skuInput, /selectorOpeningRef/)
assert.match(skuInput, /interactionInFlightRef/)
assert.match(zodConfig, /z\.setErrorMap\(portugueseErrorMap\)/)
assert.match(zodConfig, /Selecione uma opção válida\./)
assert.match(provider, /useSyncExternalStore/)
assert.match(provider, /controller\.requestDismiss/)
assert.match(provider, /controller\.dismissAll/)
assert.match(commands, /target: options\.target/)
assert.doesNotMatch(commands, /target: options\.target\s*\?\?\s*activeWindow\?\.scopeRef/)
assert.match(docs, /WindowController|useUi|useWindow/i)
assert.match(docs, /cada janela|próprio overlay|overlay próprio/i)

execFileSync("node", ["scripts/check-imperative-ui-boundaries.mjs"], { cwd: root, stdio: "inherit" })
execFileSync("node", ["scripts/test-window-controller.mjs"], { cwd: root, stdio: "inherit" })
console.log("Contratos Imperative UI verificados.")
