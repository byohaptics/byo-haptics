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
  expect("Transport", value(components, "manifest.transport"), expected.transport);
  expect("CanReportConnection", value(components, "manifest.canReportConnection"), expected.canReportConnection);
  expect("RequireCredit", value(components, "package.license", "RequireCredit"), true);
  expect("CreditString", value(components, "package.license", "CreditString"), "byohaptics");
  expect("CanExport", value(components, "package.license", "CanExport"), false);
  if (expected.cardFields !== false) {
    expect("Card version", value(components, "package.version.text", "Content"), `v${expected.version}`);
    expect("Card contract", value(components, "package.contract.text", "Content"), "Contract: BYOHaptics.Output.v1");
  }
  for (const [alias, wanted] of Object.entries(expected.config)) expect(alias, value(components, alias), wanted);
}

check("specs/joycon-osc-plugin.resoslots.json", {
  id: "io.github.byohaptics.output.joycon.osc",
  name: "Joy-Con OSC",
  version: versions.plugins.joyconOsc,
  canReportConnection: true,
  transport: "osc",
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
  transport: "osc",
  config: {
    "config.deviceAddress": "",
    "config.devicePort": 8000,
    "config.channelPrefix": "/avatar/parameters/haptira/channel/",
    "config.channelSuffix": "/value",
  },
});

check("specs/demo-output-plugin.resoslots.json", {
  id: "io.github.byohaptics.output.demo",
  name: "Demo",
  version: versions.plugins.demo,
  canReportConnection: true,
  transport: "scene",
  cardFields: false,
  config: {},
});

const demoSpec = JSON.parse(fs.readFileSync(new URL("../specs/demo-output-plugin.resoslots.json", import.meta.url), "utf8"));
const demoComponents = new Map(demoSpec.slots.flatMap((slot) => (slot.components ?? []).map((component) => [component.alias, component])));
for (const alias of ["device.left.intensity", "device.right.intensity"]) {
  if (!demoComponents.has(alias)) throw new Error(`Demo device component missing: ${alias}`);
}
const demoSlot = (path) => demoSpec.slots.find((slot) => slot.path === path);
const demoCanvasSize = demoComponents.get("package.canvas")?.members?.Size?.value;
if (demoCanvasSize?.x !== 420 || demoCanvasSize?.y !== 186) throw new Error("Demo card must fit its four rows at 420 x 186");
for (const alias of ["package.roundedTexture", "package.backing.image", "package.accent.image", "package.content.layout"]) {
  if (!demoComponents.has(alias)) throw new Error(`Demo Joy-Con card-style component missing: ${alias}`);
}
const accent = demoComponents.get("package.accent.image")?.members?.Tint?.value;
if (accent?.r !== 0.62 || accent?.g !== 0.28 || accent?.b !== 0.88) throw new Error("Demo accent must use its distinct purple color");
const demoLayout = demoComponents.get("package.content.layout")?.members;
if (demoLayout?.PaddingTop?.value !== 24 || demoLayout?.PaddingBottom?.value !== 12) {
  throw new Error("Demo card padding must fit its content without inherited empty space");
}
if (demoSlot("Devices/Left")?.position?.x !== -0.55 || demoSlot("Devices/Right")?.position?.x !== 0.55) {
  throw new Error("Demo devices must remain clear of the Host panel at x = +/-0.55");
}
for (const [side, path] of [["left", "Devices/Left"], ["right", "Devices/Right"]]) {
  const reference = demoSpec.slotFieldReferences?.find((item) => item.component === `device.${side}.wiggler`);
  if (reference?.member !== "_target" || reference?.slotPath !== path || reference?.slotMember !== "Rotation") {
    throw new Error(`Demo ${side} Wiggler must directly target ${path}.Rotation`);
  }
}

for (const [path, id] of [
  ["flux/BYOHapticsJoyConOSCOutput.pg", "io.github.byohaptics.output.joycon.osc"],
  ["flux/BYOHapticsHaptiraOSCOutput.pg", "io.github.byohaptics.output.haptira.osc"],
  ["flux/BYOHapticsDemoOutput.pg", "io.github.byohaptics.output.demo"],
]) {
  const graph = fs.readFileSync(new URL(`../${path}`, import.meta.url), "utf8");
  if (!graph.includes(`ThisPluginId = "${id}"`)) throw new Error(`${path}: incorrect PluginId`);
  if (!graph.includes("BYOHaptics.Output.v1/Active")) throw new Error(`${path}: contract namespace missing`);
  if (!graph.includes("BusContractVersion.Value == 1")) throw new Error(`${path}: contract integer check missing`);
  if (/BYOHaptics\.Output\.v2|BusContractVersion\.Value == 2/.test(graph)) throw new Error(`${path}: legacy contract remains`);
  if (path.includes("Demo")) {
    for (const target of ["left", "right"]) {
      if (!graph.includes(`== "${target}"`)) throw new Error(`${path}: Target ${target} routing missing`);
    }
    if (!graph.includes("LeftWigglerMagnitude <- LeftMagnitudeValue") || !graph.includes("RightWigglerMagnitude <- RightMagnitudeValue")) {
      throw new Error(`${path}: Wiggler magnitude writers missing`);
    }
    if (!graph.includes("LeftOutput * 12.0") || !graph.includes("RightOutput * 12.0")) {
      throw new Error(`${path}: visible twelve-degree Demo magnitude missing`);
    }
  }
}

console.log("Plugin Package manifests and defaults are consistent");
