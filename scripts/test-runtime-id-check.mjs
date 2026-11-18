import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { spawnSync } from "node:child_process";

const fixture = path.join(os.tmpdir(), `runtime-id-${process.pid}.txt`);
fs.writeFileSync(fixture, "Res" + "o_123ABC");
try {
  const result = spawnSync(process.execPath, ["scripts/check-runtime-ids.mjs", fixture]);
  if (result.status === 0) throw new Error("fixed runtime ID was accepted");
} finally {
  fs.rmSync(fixture, { force: true });
}

console.log("Runtime ID checker self-check passed.");
