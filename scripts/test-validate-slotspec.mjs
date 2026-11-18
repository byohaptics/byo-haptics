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
const smoke = JSON.parse(fs.readFileSync(new URL("../specs/smoke.resoslots.json", import.meta.url)));
const invalidCases = [
  ["duplicate alias", (spec) => { spec.slots[1].components[0].alias = "smoke.enabled"; }],
  ["non-array float4List", (spec) => { spec.slots[0].components[3].members.Values.value = {}; }],
  ["missing float4 lane", (spec) => { delete spec.slots[0].components[3].members.Values.value[0].w; }],
  ["non-numeric float4 lane", (spec) => { spec.slots[0].components[3].members.Values.value[0].x = "1"; }],
  ["out-of-range list index", (spec) => { spec.fluxInputs.Row0.fieldRef = "smoke.rows.Values.2"; }],
  ["negative list index", (spec) => { spec.fluxInputs.Row0.fieldRef = "smoke.rows.Values.-1"; }],
  ["non-numeric list index", (spec) => { spec.fluxInputs.Row0.fieldRef = "smoke.rows.Values.first"; }],
  ["both field reference forms", (spec) => { spec.fieldReferences[0].fieldRefList = []; }],
  ["missing field reference form", (spec) => { delete spec.fieldReferences[0].fieldRef; }],
  ["non-array fieldRefList", (spec) => { spec.fieldReferences[1].fieldRefList = "smoke.targetA.Value"; }],
  ["unresolved fieldRefList item", (spec) => { spec.fieldReferences[1].fieldRefList[1] = "smoke.missing.Value"; }],
];
try {
  for (const [name, mutate] of invalidCases) {
    const invalid = structuredClone(smoke);
    mutate(invalid);
    fs.writeFileSync(invalidPath, JSON.stringify(invalid));
    if (run(invalidPath).status === 0) throw new Error(`${name} was accepted`);
  }
} finally {
  fs.rmSync(invalidPath, { force: true });
}

console.log("SlotSpec validator self-check passed.");
