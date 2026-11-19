import { spawnSync } from "node:child_process";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

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
  const ordered = ["validate-slotspec.mjs", name === "host" ? "compile.ps1" : "compile-protograph-module.fsx", "build-from-resonite-slotspec.fsx", name === "host" ? "deploy-byo-haptics.fsx" : "deploy-output-plugin.fsx", "finalize-slotspec-build.fsx"];
  let previous = -1;
  for (const step of ordered) {
    const index = wrapper.indexOf(step);
    assert(index > previous, `${name}: preflight/build/deploy/finalize order`);
    previous = index;
  }
  assert(wrapper.includes(".pending.json"));
  assert.equal((wrapper.match(/if \(\$LASTEXITCODE -ne 0\)/g) ?? []).length, 4);
}

const fixture = fs.mkdtempSync(path.join(os.tmpdir(), "byo-compile-"));
assert.equal(path.dirname(fixture), path.resolve(os.tmpdir()));
const calls = path.join(fixture, "calls.txt");
fs.copyFileSync(new URL("./compile.ps1", import.meta.url), path.join(fixture, "compile.ps1"));
fs.writeFileSync(path.join(fixture, "run-fluxsdk-script.ps1"), `param([string]$ScriptPath)
[IO.File]::AppendAllText($env:BYO_COMPILE_TEST_CALLS, $env:PROTOGRAPH_MODULE + [Environment]::NewLine)
if ($env:PROTOGRAPH_MODULE -eq $env:BYO_COMPILE_TEST_FAIL) { exit 23 }
exit 0
`);
const compile = (target, fail = "") => {
  fs.writeFileSync(calls, "");
  const result = spawnSync("powershell", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", path.join(fixture, "compile.ps1"), "-Target", target], {
    env: { ...process.env, BYO_COMPILE_TEST_CALLS: calls, BYO_COMPILE_TEST_FAIL: fail },
    encoding: "utf8",
  });
  if (result.error) throw result.error;
  return { status: result.status, modules: fs.readFileSync(calls, "utf8").trim().split(/\r?\n/).filter(Boolean) };
};
try {
  const host = compile("host");
  assert.equal(host.status, 0);
  assert.equal(host.modules.length, 6);
  assert.equal(host.modules[0], "flux/BYOHapticsLifecycle");
  assert.equal(host.modules.at(-1), "flux/BYOHapticsOutputBus");
  const plugins = compile("plugins");
  assert.equal(plugins.status, 0);
  assert.equal(plugins.modules.length, 4);
  assert.equal(plugins.modules[0], "flux/BYOHapticsJoyConOSCOutput");
  assert.equal(plugins.modules.at(-1), "templates/output-plugin/MinimalOutputPlugin");
  for (const [target, module] of [["host", host.modules[0]], ["plugins", plugins.modules[0]]]) {
    assert.deepEqual(compile(target, module), { status: 23, modules: [module] }, "Compiler failure must stop the group and preserve its exit code");
  }
  const invalid = compile("unknown");
  assert.notEqual(invalid.status, 0);
  assert.deepEqual(invalid.modules, []);

  const runtimeCheck = path.join(fixture, "runtime-check.fsx");
  const imports = fs.readFileSync(new URL("./compile-protograph-module.fsx", import.meta.url), "utf8").split(/\r?\n/).filter(line => line.startsWith("#r ") || line.startsWith("open ")).join("\n");
  fs.writeFileSync(runtimeCheck, `${imports}\nBuild.loadManifest Environment.CurrentDirectory |> ignore\n`);
  const runtime = spawnSync("powershell", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", fileURLToPath(new URL("./run-fluxsdk-script.ps1", import.meta.url)), "-ScriptPath", runtimeCheck], { stdio: "inherit", timeout: 120_000 });
  assert.equal(runtime.status, 0, "FluxSDK must execute with the selected SDK's FSharp.Core; typechecking alone does not prove compatibility");
} finally {
  fs.rmSync(fixture, { recursive: true, force: true });
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
