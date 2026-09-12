import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";

const root = path.resolve("src");

function filesIn(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const absolute = path.join(directory, entry.name);
    if (entry.isDirectory()) return filesIn(absolute);
    return /\.(ts|tsx)$/.test(entry.name) ? [absolute] : [];
  });
}

const sourceFiles = filesIn(root);
const read = (file) => fs.readFileSync(file, "utf8");
const relative = (file) => path.relative(process.cwd(), file);

const runtimeConsole = sourceFiles.filter((file) =>
  /console\.(error|warn)\s*\(/.test(read(file)),
);
assert.deepEqual(runtimeConsole, [], `runtime console logs found: ${runtimeConsole.map(relative).join(", ")}`);

const featureFiles = sourceFiles.filter((file) => file.includes(`${path.sep}features${path.sep}`));
for (const file of featureFiles) {
  const content = read(file);
  assert.equal(/from ["']sonner["']/.test(content), false, `${relative(file)} imports Sonner directly`);
  assert.equal(content.includes("@/components/ui"), false, `${relative(file)} bypasses ui/primitives`);
  assert.equal(content.includes("globalError"), false, `${relative(file)} renders operational globalError`);
}

const http = read(path.join(root, "api/http.ts"));
const feedback = read(path.join(root, "ui/adapters/feedback-sonner.ts"));
const upsertMutation = read(path.join(root, "hooks/use-upsert-mutation.ts"));
const commands = read(path.join(root, "imperative-ui/commands.ts"));
assert.equal(/from ["']sonner["']/.test(http), false, "HTTP transport imports Sonner directly");
assert.equal(/throw\s*\{/.test(http), false, "HTTP transport throws a raw object");
assert.equal(fs.existsSync(path.join(root, "utils/api-errors.ts")), false, "legacy error parser remains");
assert.match(feedback, /WeakSet<object>/, "feedback adapter must deduplicate error objects");
assert.match(upsertMutation, /safeMutateAsync/, "upsert mutations must consume rejected promises");
assert.match(commands, /fireAndForget/, "hotkey commands must consume rejected promises");

console.log("UI feedback contracts verified.");
