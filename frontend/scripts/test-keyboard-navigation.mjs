import assert from "node:assert/strict"
import { mkdtempSync, readFileSync, writeFileSync } from "node:fs"
import { tmpdir } from "node:os"
import { join, resolve } from "node:path"
import { fileURLToPath, pathToFileURL } from "node:url"
import ts from "typescript"

const root = resolve(fileURLToPath(new URL("..", import.meta.url)))
const sourceCode = readFileSync(join(root, "src/ui/keyboard-navigation-core.ts"), "utf8")
const compiled = ts.transpileModule(sourceCode, {
  compilerOptions: { target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.CommonJS },
}).outputText
const tempRoot = mkdtempSync(join(tmpdir(), "keyboard-navigation-"))
const modulePath = join(tempRoot, "core.cjs")
writeFileSync(modulePath, compiled)
const { chooseSpatialDestination } = await import(pathToFileURL(modulePath).href)

const rect = (left, top, width = 100, height = 32) => ({ left, top, width, height })
const node = (id, left, top, width = 100, height = 32, cell = null) => ({
  id,
  rect: rect(left, top, width, height),
  cell,
})

const source = node("source", 10, 10)
const aligned = node("aligned", 10, 70)
const diagonal = node("diagonal", 160, 45)
assert.equal(
  chooseSpatialDestination(source, [aligned, diagonal], "down"),
  "aligned",
  "vertical navigation prioritizes overlapping columns",
)

const wideSource = node("wide-source", 0, 100, 500, 32)
const immediateLine = node("immediate-line", 600, 148, 16, 16)
const fartherAligned = node("farther-aligned", 250, 220, 220, 32)
assert.equal(
  chooseSpatialDestination(wideSource, [fartherAligned, immediateLine], "down"),
  "immediate-line",
  "vertical navigation chooses the next visual row before perpendicular overlap",
)

const left = node("left", 0, 10)
const right = node("right", 140, 10)
const below = node("below", 10, 70)
assert.equal(
  chooseSpatialDestination(source, [left, right, below], "forward"),
  "right",
  "Tab chooses the closest control to the right",
)
assert.equal(
  chooseSpatialDestination(node("edge", 140, 10), [below], "forward"),
  "below",
  "Tab falls down when no horizontal target exists",
)
assert.equal(
  chooseSpatialDestination(node("misaligned", 10, 150), [node("right-above", 140, 10)], "forward"),
  "right-above",
  "Tab can reach a right-side control even when rows are not aligned",
)
assert.equal(
  chooseSpatialDestination(source, [left, below], "backward"),
  "left",
  "Shift+Tab chooses the closest control to the left",
)
assert.equal(
  chooseSpatialDestination(source, [left, below], "left"),
  "left",
  "ArrowLeft chooses the closest control to the left",
)
assert.equal(
  chooseSpatialDestination(source, [right, below], "right"),
  "right",
  "ArrowRight chooses the closest control to the right",
)
assert.equal(
  chooseSpatialDestination(node("edge", 140, 10), [below], "right"),
  null,
  "ArrowRight does not wrap to another row",
)
assert.equal(
  chooseSpatialDestination(source, [node("far-right", 140, 70)], "right"),
  null,
  "ArrowRight ignores controls on a different visual row",
)

// Formulário de produto: campo largo seguido de uma linha com três campos.
const descricao = node("descricao", 0, 0, 900, 64)
const categoria = node("categoria", 0, 100, 280)
const marca = node("marca", 310, 100, 280)
const unidade = node("unidade", 620, 100, 280)
const row = [unidade, marca, categoria]
assert.equal(chooseSpatialDestination(descricao, row, "down"), "categoria", "ArrowDown from a wide field starts at its left edge")
assert.equal(chooseSpatialDestination(descricao, row, "down", marca.rect.left), "marca", "ArrowDown honors the remembered goal column")
assert.equal(chooseSpatialDestination(marca, [descricao, categoria], "up"), "descricao", "ArrowUp reaches the wide field")

const grid = "stock"
const gridSource = node("qty-0", 10, 10, 80, 32, { grid, row: 0, column: 0 })
const gridQtyNext = node("qty-1", 10, 70, 80, 32, { grid, row: 1, column: 0 })
const gridPriceNext = node("price-1", 120, 70, 80, 32, { grid, row: 1, column: 1 })
assert.equal(
  chooseSpatialDestination(gridSource, [gridQtyNext, gridPriceNext], "down"),
  "qty-1",
  "grid down preserves the logical column",
)
assert.equal(
  chooseSpatialDestination(gridSource, [gridPriceNext], "down"),
  null,
  "missing grid column does not jump to another column",
)

const last = node("last", 10, 100)
assert.equal(chooseSpatialDestination(last, [], "down"), null, "navigation does not wrap")

console.log("Keyboard navigation core tests passed.")
