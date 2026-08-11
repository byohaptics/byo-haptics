import { spawnSync } from "node:child_process";

const targets = process.argv.slice(2);
const paths = targets.length ? targets : ["scripts", "flux", "specs", "protograph.toml"];
const patterns = [
  "Reso_[A-Za-z0-9]+",
  "SlotSpec_[0-9]+",
  "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
].join("|");
const result = spawnSync("rg", ["-n", patterns, "--glob", "!check-runtime-ids.mjs", ...paths], {
  encoding: "utf8",
});

if (result.status === 0) throw new Error(`Fixed runtime IDs found:\n${result.stdout}`);
if (result.status !== 1) throw new Error(result.stderr || `rg failed: ${result.status}`);
console.log("No fixed runtime IDs found.");
