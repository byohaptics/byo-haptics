import { execFileSync } from "node:child_process";

const denylist = process.env.PUBLICATION_DENYLIST;
if (!denylist) throw new Error("PUBLICATION_DENYLIST is required for publication checks.");

try {
  execFileSync("rg", ["-n", "-i", "--hidden", "--glob", "!.git/**", denylist, "."], {
    stdio: "inherit",
  });
  process.exitCode = 1;
} catch (error) {
  if (error.status !== 1) throw error;
  console.log("Publication policy check passed.");
}
