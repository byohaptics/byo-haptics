import { spawnSync } from "node:child_process";
import assert from "node:assert/strict";
import fs from "node:fs";

const builder = fs.readFileSync(new URL("./build-from-resonite-slotspec.fsx", import.meta.url), "utf8");
assert(!builder.includes("RemoveSlot"), "Construction must never delete the previous item");
assert(!builder.includes("tryGeneratedSlot"), "Cached IDs must not select replacement targets");
assert(builder.indexOf("validateComponentType componentType\n") < builder.indexOf("runBatch slotOps"));
assert(builder.includes('if path = "" then false else isActive'), "Staging root must be inactive");
assert(builder.includes('let pendingPath = outputPath + ".pending.json"'));
for (const file of ["deploy-byo-haptics.fsx", "deploy-output-plugin.fsx"]) {
  const consumer = fs.readFileSync(new URL(`./${file}`, import.meta.url), "utf8");
  assert(!/ws:\/\/(?:localhost|127\.0\.0\.1):\d+/.test(consumer), `${file}: require the current session endpoint instead of a fixed local port`);
  assert(/if File\.Exists\((?:path|idsPath) \+ "\.pending\.json"\) then/.test(consumer), `${file}: reject stale published IDs while a pending build exists`);
}
for (const name of ["host", "plugin"]) {
  const wrapper = fs.readFileSync(new URL(`./deploy-${name}.ps1`, import.meta.url), "utf8");
  const ordered = ["validate-slotspec.mjs", name === "host" ? "compile-host.ps1" : "compile-protograph-module.fsx", "build-from-resonite-slotspec.fsx", name === "host" ? "deploy-byo-haptics.fsx" : "deploy-output-plugin.fsx", "finalize-slotspec-build.fsx"];
  let previous = -1;
  for (const step of ordered) {
    const index = wrapper.indexOf(step);
    assert(index > previous, `${name}: preflight/build/deploy/finalize order`);
    previous = index;
  }
  assert(wrapper.includes(".pending.json"));
  assert.equal((wrapper.match(/if \(\$LASTEXITCODE -ne 0\)/g) ?? []).length, 4);
}

for (const args of [
  ["fsi", "scripts/test-slotspec-build-safety.fsx"],
  ["fsi", "--typecheck-only", "scripts/finalize-slotspec-build.fsx"],
]) {
  const check = spawnSync("dotnet", args, { stdio: "inherit", timeout: 120_000 });
  if (check.status !== 0) throw new Error(`SlotSpec safety check failed: ${args.join(" ")}`);
}

const result = spawnSync("dotnet", ["fsi", "scripts/build-from-resonite-slotspec.fsx"], {
  env: {
    ...process.env,
    RESONITE_SLOTSPEC_PATH: "specs/smoke.resoslots.json",
    RESONITE_SLOTSPEC_VALIDATE_ONLY: "1",
  },
  stdio: "inherit",
});

if (result.status !== 0) throw new Error("SlotSpec builder offline compile check failed.");
console.log("Builder offline compile check passed.");
