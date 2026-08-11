import { spawnSync } from "node:child_process";

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
