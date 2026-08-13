import fs from "node:fs";

const versions = JSON.parse(fs.readFileSync(new URL("../versions.json", import.meta.url), "utf8"));
const value = (components, alias, member = "Value") => components.get(alias)?.members?.[member]?.value;

function check(path, expected) {
  const spec = JSON.parse(fs.readFileSync(new URL(`../${path}`, import.meta.url), "utf8"));
  const components = new Map(spec.slots.flatMap((slot) => (slot.components ?? []).map((component) => [component.alias, component])));
  const expect = (label, actual, wanted) => {
    if (actual !== wanted) throw new Error(`${path} ${label}: ${JSON.stringify(actual)} != ${JSON.stringify(wanted)}`);
  };

  expect("toolName", spec.toolName, expected.id);
  expect("tag", spec.tag, "byo-haptics.output-plugin");
  expect("parent", spec.toolParent?.kind, "worldRoot");
  expect("PluginId", value(components, "manifest.pluginId"), expected.id);
  expect("DisplayName", value(components, "manifest.displayName"), expected.name);
  expect("ContractVersion", value(components, "manifest.contractVersion"), versions.outputContract);
  expect("PluginVersion", value(components, "manifest.pluginVersion"), `v${expected.version}`);
  expect("Author", value(components, "manifest.author"), "byohaptics");
  expect("Transport", value(components, "manifest.transport"), "osc");
  expect("CanReportConnection", value(components, "manifest.canReportConnection"), expected.canReportConnection);
  expect("Card version", value(components, "package.version.text", "Content"), `v${expected.version}`);
  expect("Card contract", value(components, "package.contract.text", "Content"), "Contract: BYOHaptics.Output.v1");
  for (const [alias, wanted] of Object.entries(expected.config)) expect(alias, value(components, alias), wanted);
}

check("specs/joycon-osc-plugin.resoslots.json", {
  id: "io.github.byohaptics.output.joycon.osc",
  name: "Joy-Con OSC",
  version: versions.plugins.joyconOsc,
  canReportConnection: true,
  config: {
    "config.bridgeAddress": "127.0.0.1",
    "config.bridgePort": 9010,
    "config.endpoint": "osc://127.0.0.1:9010",
    "config.statusPort": 9002,
    "config.heartbeatAddress": "/avatar/parameters/joyconrumble/heartbeat",
    "config.statusPortAddress": "/avatar/parameters/joyconrumble/status/port",
    "config.statusHeartbeatAddress": "/avatar/parameters/joyconrumble/status/heartbeat",
    "config.channelPrefix": "/avatar/parameters/joyconrumble/channel/",
  },
});

check("specs/haptira-osc-plugin.resoslots.json", {
  id: "io.github.byohaptics.output.haptira.osc",
  name: "Haptira OSC",
  version: versions.plugins.haptiraOsc,
  canReportConnection: false,
  config: {
    "config.deviceAddress": "",
    "config.devicePort": 8000,
    "config.channelPrefix": "/avatar/parameters/haptira/channel/",
    "config.channelSuffix": "/value",
  },
});

for (const [path, id] of [
  ["flux/BYOHapticsJoyConOSCOutput.pg", "io.github.byohaptics.output.joycon.osc"],
  ["flux/BYOHapticsHaptiraOSCOutput.pg", "io.github.byohaptics.output.haptira.osc"],
]) {
  const graph = fs.readFileSync(new URL(`../${path}`, import.meta.url), "utf8");
  if (!graph.includes(`ThisPluginId = "${id}"`)) throw new Error(`${path}: incorrect PluginId`);
  if (!graph.includes("BYOHaptics.Output.v1/Active")) throw new Error(`${path}: contract namespace missing`);
  if (!graph.includes("BusContractVersion.Value == 1")) throw new Error(`${path}: contract integer check missing`);
  if (/BYOHaptics\.Output\.v2|BusContractVersion\.Value == 2/.test(graph)) throw new Error(`${path}: legacy contract remains`);
}

console.log("Plugin Package manifests and defaults are consistent");
