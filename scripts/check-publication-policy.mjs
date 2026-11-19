import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";

const denylistFile = execFileSync("git", ["config", "--get", "byohaptics.publicationDenylistFile"], { encoding: "utf8" }).trim();
const denylist = readFileSync(denylistFile, "utf8").trim();
if (!denylist) throw new Error("The private publication rule file must not be empty.");

try {
  execFileSync("rg", ["-n", "-i", "--hidden", "--glob", "!.git/**", denylist, "."], {
    stdio: "inherit",
  });
  process.exitCode = 1;
} catch (error) {
  if (error.status !== 1) throw error;
  console.log("Publication policy check passed.");
}
