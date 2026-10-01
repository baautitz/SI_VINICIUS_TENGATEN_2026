// Trava de useEffect/useLayoutEffect fora do núcleo. Compara com a linha de base
// em scripts/effects-baseline.json: o número por arquivo só pode cair. Quando a
// linha de base ficar vazia, qualquer efeito novo fora do núcleo quebra o script.
import { readFileSync, readdirSync, statSync } from "node:fs"
import { join, relative, resolve } from "node:path"
import { fileURLToPath } from "node:url"

const root = resolve(fileURLToPath(new URL("..", import.meta.url)))
const src = join(root, "src")
// Núcleo e libs externas (shadcn) onde o efeito é sincronização com sistema externo.
const allowed = [/^src\/ui\//, /^src\/imperative-ui\//, /^src\/components\/ui\//, /^src\/providers\//]
const baseline = JSON.parse(readFileSync(join(root, "scripts/effects-baseline.json"), "utf8"))
const pattern = /\b(?:React\.)?use(?:Layout)?Effect\s*\(/g

const counts = {}
const walk = (dir) => {
  for (const name of readdirSync(dir)) {
    const path = join(dir, name)
    if (statSync(path).isDirectory()) walk(path)
    else if (/\.(tsx?|jsx?)$/.test(name)) {
      const file = relative(root, path)
      if (allowed.some((re) => re.test(file))) continue
      const n = (readFileSync(path, "utf8").match(pattern) ?? []).length
      if (n) counts[file] = n
    }
  }
}
walk(src)

const failures = Object.entries(counts).filter(([file, n]) => n > (baseline[file] ?? 0))
const total = Object.values(counts).reduce((a, b) => a + b, 0)
const base = Object.values(baseline).reduce((a, b) => a + b, 0)
if (failures.length) {
  for (const [file, n] of failures) console.error(`${file}: ${n} efeito(s), baseline ${baseline[file] ?? 0}`)
  process.exit(1)
}
console.log(`efeitos fora do núcleo: ${total} (baseline ${base})`)
