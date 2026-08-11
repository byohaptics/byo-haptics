import fs from "node:fs";

const [target, version] = process.argv.slice(2);
const paths = {
  product: ["product"],
  documents: ["documents"],
  "joycon-osc": ["plugins", "joyconOsc"],
  "haptira-osc": ["plugins", "haptiraOsc"],
  "joycon-bridge-api": ["bridgeApis", "joyconOsc"],
};
if (!paths[target] || !/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/.test(version ?? "")) {
  throw new Error(`Usage: node scripts/set-version.mjs <${Object.keys(paths).join("|")}> <semver>`);
}

const versionsPath = new URL("../versions.json", import.meta.url);
const versions = JSON.parse(fs.readFileSync(versionsPath, "utf8"));
const replace = (path, pattern, replacement) => {
  const url = new URL(`../${path}`, import.meta.url);
  const content = fs.readFileSync(url, "utf8");
  fs.writeFileSync(url, content.replace(pattern, replacement));
};
const updatePluginSpec = (path) => {
  const url = new URL(`../${path}`, import.meta.url);
  const spec = JSON.parse(fs.readFileSync(url, "utf8"));
  for (const slot of spec.slots) for (const component of slot.components ?? []) {
    if (component.alias === "manifest.pluginVersion") component.members.Value.value = `v${version}`;
    if (component.alias === "package.version.text") component.members.Content.value = `v${version}`;
  }
  fs.writeFileSync(url, `${JSON.stringify(spec, null, 2)}\n`);
};
const keys = paths[target];
let owner = versions;
for (const key of keys.slice(0, -1)) owner = owner[key];
owner[keys.at(-1)] = version;
fs.writeFileSync(versionsPath, `${JSON.stringify(versions, null, 2)}\n`);

if (target === "product") {
  const packagePath = new URL("../package.json", import.meta.url);
  const packageJson = JSON.parse(fs.readFileSync(packagePath, "utf8"));
  packageJson.version = version;
  fs.writeFileSync(packagePath, `${JSON.stringify(packageJson, null, 2)}\n`);

  const protographPath = new URL("../protograph.toml", import.meta.url);
  const protograph = fs.readFileSync(protographPath, "utf8");
  fs.writeFileSync(protographPath, protograph.replace(/^version = ".*"$/m, `version = "${version}"`));

  const specPath = new URL("../specs/byo-haptics.resoslots.json", import.meta.url);
  const spec = JSON.parse(fs.readFileSync(specPath, "utf8"));
  for (const slot of spec.slots) for (const component of slot.components ?? []) {
    if (component.alias === "config.version") component.members.Value.value = `v${version}`;
    if (component.alias === "ui.title.text") component.members.Content.value = `BYO Haptics v${version}`;
  }
  fs.writeFileSync(specPath, `${JSON.stringify(spec, null, 2)}\n`);
} else if (target === "documents") {
  for (const name of fs.readdirSync(new URL("../docs", import.meta.url)).filter((name) => name.endsWith(".md"))) {
    replace(`docs/${name}`, /^Document version: `[^`]+`$/m, `Document version: \`${version}\``);
  }
} else if (target === "joycon-osc") {
  replace("docs/joycon-plugin.md", /^- Plugin version: `[^`]+`$/m, `- Plugin version: \`${version}\``);
  updatePluginSpec("specs/joycon-osc-plugin.resoslots.json");
} else if (target === "haptira-osc") {
  replace("docs/haptira-plugin.md", /^- Plugin version: `[^`]+`$/m, `- Plugin version: \`${version}\``);
  updatePluginSpec("specs/haptira-osc-plugin.resoslots.json");
} else if (target === "joycon-bridge-api") {
  replace("docs/joycon-plugin.md", /^- Bridge API version: `[^`]+`$/m, `- Bridge API version: \`${version}\``);
}

console.log(`${target} version set to ${version}`);
