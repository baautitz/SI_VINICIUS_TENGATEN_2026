import assert from "node:assert/strict"
import { mkdtempSync, readFileSync, writeFileSync } from "node:fs"
import { tmpdir } from "node:os"
import { join, resolve } from "node:path"
import { fileURLToPath, pathToFileURL } from "node:url"
import ts from "typescript"

const projectRoot = resolve(fileURLToPath(new URL("..", import.meta.url)))
const source = readFileSync(join(projectRoot, "src/ui/imperative/controller.ts"), "utf8")
const compiled = ts.transpileModule(source, {
  compilerOptions: { target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.CommonJS },
}).outputText
const tempRoot = mkdtempSync(join(tmpdir(), "imperative-ui-controller-"))
const modulePath = join(tempRoot, "controller.cjs")
writeFileSync(modulePath, compiled)
const { WindowController } = await import(pathToFileURL(modulePath).href)

const View = () => null
const focusCalls = []
const focusTarget = { isConnected: true, focus: () => focusCalls.push("focused") }
let confirmCalls = 0
let confirmResult = false
let releaseConfirm

const controller = new WindowController({
  restoreFocus: (target) => target?.focus(),
  confirmDiscard: () => {
    confirmCalls += 1
    return new Promise((resolve) => {
      releaseConfirm = () => resolve(confirmResult)
    })
  },
})

const first = controller.open(View, {}, {}, focusTarget)
const firstId = controller.snapshot().activeId
const second = controller.open(View, {}, {})
const secondId = controller.snapshot().activeId
assert.notEqual(firstId, secondId)

controller.resolve(firstId, "ignored")
assert.equal(controller.snapshot().activeId, secondId, "janela inferior não pode resolver")
controller.dismiss(secondId)
assert.equal((await second).reason, "cancel")
assert.deepEqual(focusCalls, [])

controller.markDirty(firstId, true)
controller.requestDismiss(firstId, "escape")
controller.requestDismiss(firstId, "escape")
assert.equal(confirmCalls, 1, "fechamentos repetidos não podem abrir confirmações duplicadas")
releaseConfirm()
await new Promise((resolve) => setImmediate(resolve))
assert.equal(controller.snapshot().activeId, firstId, "cancelar descarte preserva a janela")

confirmResult = true
controller.requestDismiss(firstId, "cancel")
assert.equal(confirmCalls, 2)
releaseConfirm()
await new Promise((resolve) => setImmediate(resolve))
const firstResult = await first
assert.deepEqual(firstResult, { status: "cancelled", reason: "discarded" })
assert.deepEqual(focusCalls, ["focused"], "fechamento restaura o opener correto")
controller.resolve(firstId, "ignored")
assert.equal(controller.snapshot().activeId, null)

const pendingA = controller.open(View, {})
const pendingB = controller.open(View, {})
controller.dismissAll()
assert.equal((await pendingA).reason, "unmounted")
assert.equal((await pendingB).reason, "unmounted")

console.log("WindowController runtime tests passed.")
