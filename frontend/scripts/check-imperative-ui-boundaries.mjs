import ts from "typescript"
import { readFileSync } from "node:fs"
import { glob } from "node:fs/promises"
import { resolve, sep } from "node:path"
import { fileURLToPath } from "node:url"

const projectRoot = resolve(fileURLToPath(new URL("..", import.meta.url)))
const sourceRoot = resolve(projectRoot, "src")

const authorized = (file) => {
  const path = file.split(sep).join("/")
  return (
    path.startsWith("src/imperative-ui/") ||
    path.startsWith("src/ui/imperative/") ||
    path.startsWith("src/ui/primitives/") ||
    path.startsWith("src/ui/composites/") ||
    path.startsWith("src/ui/adapters/") ||
    path.startsWith("src/components/ui/") ||
    path === "src/providers/query-provider.tsx" ||
    path === "src/api/http.ts"
  )
}

const violations = []
const report = (file, sourceFile, node, label) => {
  const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile))
  violations.push(`${file}:${position.line + 1} — ${label}`)
}

const windowTags = new Set(["Dialog", "Sheet", "UpsertDialog", "DeleteDialog", "ListDialog"])
for await (const relativePath of glob("**/*.{ts,tsx}", { cwd: sourceRoot })) {
  const file = `src/${relativePath.split(sep).join("/")}`
  if (authorized(file)) continue

  const content = readFileSync(resolve(projectRoot, file), "utf8")
  const sourceFile = ts.createSourceFile(file, content, ts.ScriptTarget.Latest, true)
  const visit = (node) => {
    if (ts.isImportDeclaration(node)) {
      const moduleName = node.moduleSpecifier.text
      if (moduleName === "@tanstack/react-hotkeys") report(file, sourceFile, node, "useHotkeys direto")
      if (moduleName === "sonner") report(file, sourceFile, node, "Sonner direto")
      if (moduleName === "next/navigation") report(file, sourceFile, node, "Router direto")
      if (/^(?:radix-ui|@radix-ui\/|@base-ui\/)/.test(moduleName)) {
        report(file, sourceFile, node, "primitiva de biblioteca direta")
      }
      if (/^@\/components\/ui\//.test(moduleName) || /^(?:\.\.\/|\.\/)+ui\//.test(moduleName)) {
        report(file, sourceFile, node, "implementação shadcn direta")
      }
      if (moduleName === "@/imperative-ui") report(file, sourceFile, node, "infraestrutura imperativa interna")
      if (/^@\/components\/ui\/(?:dialog|sheet|upsert-dialog|delete-dialog|list-dialog)$/.test(moduleName)) {
        report(file, sourceFile, node, "superfície de janela direta")
      }
    }

    if (ts.isJsxOpeningLikeElement(node)) {
      const tagName = node.tagName.getText(sourceFile)
      if (windowTags.has(tagName)) {
        for (const attribute of node.attributes.properties) {
          if (!ts.isJsxAttribute(attribute)) continue
          const name = attribute.name.text
          if (name === "open" || name === "isOpen") report(file, sourceFile, attribute, "visibilidade declarativa")
          if (name === "onOpenChange") report(file, sourceFile, attribute, "onOpenChange declarativo")
        }
      }
    }
    ts.forEachChild(node, visit)
  }
  visit(sourceFile)
}

if (violations.length) {
  console.error("Fronteira Imperative UI violada:")
  console.error(violations.sort().join("\n"))
  process.exitCode = 1
} else {
  console.log("Fronteiras Imperative UI verificadas sem violações.")
}
