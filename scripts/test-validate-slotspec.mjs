import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { spawnSync } from "node:child_process";

const run = (spec) => spawnSync(process.execPath, ["scripts/validate-slotspec.mjs", spec], {
  cwd: new URL("..", import.meta.url),
  encoding: "utf8",
});

if (run("specs/smoke.resoslots.json").status !== 0) throw new Error("valid smoke spec failed");

const invalidPath = path.join(os.tmpdir(), `invalid-slotspec-${process.pid}.json`);
const invalid = JSON.parse(fs.readFileSync(new URL("../specs/smoke.resoslots.json", import.meta.url)));
invalid.slots[1].components[0].alias = "smoke.enabled";
fs.writeFileSync(invalidPath, JSON.stringify(invalid));
try {
  if (run(invalidPath).status === 0) throw new Error("duplicate alias was accepted");
} finally {
  fs.rmSync(invalidPath, { force: true });
}

console.log("SlotSpec validator self-check passed.");
