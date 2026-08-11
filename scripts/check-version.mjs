import fs from "node:fs";

const version = fs.readFileSync(new URL("../VERSION", import.meta.url), "utf8").trim();
const packageJson = JSON.parse(fs.readFileSync(new URL("../package.json", import.meta.url), "utf8"));
const protograph = fs.readFileSync(new URL("../protograph.toml", import.meta.url), "utf8");
const protographVersion = protograph.match(/^version = "([^"]+)"$/m)?.[1];

for (const [name, actual] of [["package.json", packageJson.version], ["protograph.toml", protographVersion]]) {
  if (actual !== version) throw new Error(`${name}: ${actual ?? "<missing>"} != ${version}`);
}

console.log(`Version references are consistent: ${version}`);
