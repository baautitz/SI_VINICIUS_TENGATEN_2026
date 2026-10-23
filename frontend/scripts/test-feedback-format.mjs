import assert from "node:assert/strict";
import {
  MAX_TOAST_TITLE_LENGTH,
  splitLongMessage,
  toastDuration,
} from "../src/ui/adapters/feedback-format.ts";

assert.equal(toastDuration("success"), 4000);
assert.equal(toastDuration("info"), 4000);
assert.equal(toastDuration("warning"), 6000);
assert.equal(toastDuration("error"), 8000);
assert.equal(toastDuration("loading"), undefined);
assert.equal(toastDuration("error", 1500), 1500, "explicit duration wins");

assert.deepEqual(splitLongMessage("Curta."), { title: "Curta." });

const exact = "a".repeat(MAX_TOAST_TITLE_LENGTH);
assert.deepEqual(splitLongMessage(exact), { title: exact });

const long = `${"palavra ".repeat(60)}fim`;
const split = splitLongMessage(long);
assert.ok(split.title.length <= MAX_TOAST_TITLE_LENGTH + 1, "title is bounded");
assert.ok(split.title.endsWith("…"));
assert.ok(!split.title.includes("palavr…") || split.title.endsWith(" palavra…"), "cuts at a word boundary");
assert.ok(split.description, "remainder goes to description");
assert.equal(
  split.title.slice(0, -1).trim() + " " + split.description,
  long,
  "no text is lost",
);

console.log("Feedback format contracts verified.");
