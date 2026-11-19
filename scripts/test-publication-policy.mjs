import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const fixture = fs.mkdtempSync(path.join(os.tmpdir(), "publication-policy-"));
assert.equal(path.dirname(fixture), path.resolve(os.tmpdir()));
const source = path.join(fixture, "source");
const rule = path.join(fixture, "rule.txt");
const config = path.join(fixture, "gitconfig");
const checker = fileURLToPath(new URL("./check-publication-policy.mjs", import.meta.url));
const env = Object.fromEntries(Object.entries(process.env).filter(([key]) => !key.startsWith("GIT_")));
env.GIT_CONFIG_GLOBAL = config;
env.GIT_CONFIG_NOSYSTEM = "1";
const run = () => spawnSync(process.execPath, [checker], { cwd: source, env }).status;

try {
  fs.mkdirSync(source);
  fs.writeFileSync(path.join(source, "README.md"), "Public source\n");
  fs.writeFileSync(config, "");
  assert.notEqual(run(), 0, "Missing configuration must stop publication");
  fs.writeFileSync(config, `[byohaptics]\npublicationDenylistFile = "${rule.replaceAll("\\", "/")}"\n`);
  assert.notEqual(run(), 0, "Missing rule file must stop publication");
  fs.writeFileSync(rule, "");
  assert.notEqual(run(), 0, "Empty rule must stop publication");
  fs.writeFileSync(rule, "FORBIDDEN_PUBLICATION_TEST");
  assert.equal(run(), 0, "Clean source must pass using the external file");
  fs.writeFileSync(path.join(source, ".hidden.md"), "forbidden_publication_test\n");
  assert.equal(run(), 1, "Hidden files and case-insensitive matches must be checked");
  fs.writeFileSync(rule, "[");
  assert.notEqual(run(), 0, "Invalid regex must stop publication");
} finally {
  fs.rmSync(fixture, { recursive: true, force: true });
}

console.log("Publication policy self-check passed.");
