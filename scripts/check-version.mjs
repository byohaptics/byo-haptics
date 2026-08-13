import fs from "node:fs";

const versions = JSON.parse(fs.readFileSync(new URL("../versions.json", import.meta.url), "utf8"));
const packageJson = JSON.parse(fs.readFileSync(new URL("../package.json", import.meta.url), "utf8"));
const protograph = fs.readFileSync(new URL("../protograph.toml", import.meta.url), "utf8");
const bridgeCargo = fs.readFileSync(new URL("../bridges/joycon-rumble/Cargo.toml", import.meta.url), "utf8");
const protographVersion = protograph.match(/^version = "([^"]+)"$/m)?.[1];
const readDoc = (name) => fs.readFileSync(new URL(`../docs/${name}`, import.meta.url), "utf8");

for (const [name, actual] of [["package.json", packageJson.version], ["protograph.toml", protographVersion]]) {
  if (actual !== versions.product) throw new Error(`${name}: ${actual ?? "<missing>"} != ${versions.product}`);
}

for (const name of fs.readdirSync(new URL("../docs", import.meta.url)).filter((name) => name.endsWith(".md"))) {
  const content = fs.readFileSync(new URL(`../docs/${name}`, import.meta.url), "utf8");
  const documentVersion = content.match(/^Document version: `([^`]+)`$/m)?.[1];
  if (documentVersion && documentVersion !== versions.documents) {
    throw new Error(`docs/${name}: ${documentVersion} != ${versions.documents}`);
  }
}

const contract = readDoc("output-plugin-contract.md").match(/^- Contract integer: `([0-9]+)`$/m)?.[1];
const joycon = readDoc("joycon-plugin.md");
const haptira = readDoc("haptira-plugin.md");
const demo = readDoc("demo-plugin.md");
const bridge = readDoc("joycon-bridge.md");
for (const [name, actual, expected] of [
  ["Output contract", contract, String(versions.outputContract)],
  ["Joy-Con plugin", joycon.match(/^- Plugin version: `([^`]+)`$/m)?.[1], versions.plugins.joyconOsc],
  ["Joy-Con bridge API", joycon.match(/^- Bridge API version: `([^`]+)`$/m)?.[1], versions.bridgeApis.joyconOsc],
  ["Joy-Con bridge", bridge.match(/^- Bridge version: `([^`]+)`$/m)?.[1], versions.bridges.joyconRumble],
  ["Joy-Con bridge Cargo", bridgeCargo.match(/^version = "([^"]+)"$/m)?.[1], versions.bridges.joyconRumble],
  ["Joy-Con bridge API document", bridge.match(/^- Bridge API version: `([^`]+)`$/m)?.[1], versions.bridgeApis.joyconOsc],
  ["Haptira plugin", haptira.match(/^- Plugin version: `([^`]+)`$/m)?.[1], versions.plugins.haptiraOsc],
  ["Demo plugin", demo.match(/^- Plugin version: `([^`]+)`$/m)?.[1], versions.plugins.demo],
]) {
  if (actual !== expected) throw new Error(`${name}: ${actual ?? "<missing>"} != ${expected}`);
}

console.log(`Version references are consistent: product=${versions.product} contract=${versions.outputContract}`);
