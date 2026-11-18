import fs from "node:fs";

const versions = JSON.parse(fs.readFileSync(new URL("../versions.json", import.meta.url), "utf8"));
const value = (components, alias, member = "Value") => components.get(alias)?.members?.[member]?.value;

function checkRows(path, graph) {
  const update = graph.slice(graph.indexOf("switch LocalUpdate"));
  const gate = update.indexOf("If(Condition=CanManageRuntime, OnTrue=impulse {");
  for (const field of ["PackageInstalled", "PackageVisualActive", "PackageGrabbableEnabled"]) {
    const writes = [...update.matchAll(new RegExp(`${field} <-`, "g"))];
    if (gate < 0 || writes.length !== 1 || writes[0].index >= gate) {
      throw new Error(`${path}: ${field} must update once before the runtime ownership gate`);
    }
  }
  if (update.indexOf("WriteDynamicValueVariable(") < gate || update.indexOf("DiagnosticsActive <-") < gate) {
    throw new Error(`${path}: runtime writes must remain inside the ownership gate`);
  }
  for (const required of [
    '~input<SyncFieldList<float4>>("BYOHaptics.Output.v2/Rows")',
    'RowList = ContinuouslyChangingObjectRelay<SyncFieldList<float4>>(Input=BusRows.Value)',
    'RowCount = if RowsPresent then ReadOnlyCount<_,float4>(Collection=RowList) else 0',
    '~input<SyncFieldList<string>>("BYOHaptics.Output.v2/Targets")',
    'TargetList = ContinuouslyChangingObjectRelay<SyncFieldList<string>>(Input=BusTargets.Value)',
    'TargetCount = if TargetsPresent then ReadOnlyCount<_,string>(Collection=TargetList) else 0',
    'if RowCount > 0 then RowCount == TargetCount else false',
    'Selected = if ListsValid then',
    '~input<string>("BYOHaptics.Output.v2/ActivePluginId")',
    'if BusPluginId.HasValue then BusPluginId.Value == ThisPluginId else false',
    'ReadOnlyForEachWithIndexValue<_,float4>(Collection=RowList, Reverse=false)',
    'ReadOnlyGetAtObject<_,string>(Collection=TargetList, Index=Loop.Index)',
    'Force, Vibration, Pain, Temperature = Loop.Element->unpack',
    'Path="BYOHaptics.Output.v2/ConnectionState"',
  ]) if (!graph.includes(required)) throw new Error(`${path}: missing variable-row binding: ${required}`);
  if (/ReadOnlyGetAt(Value|Object)<[^>]+>\(Collection=(RowList|TargetList), Index=[0-9]+\)/.test(graph)) {
    throw new Error(`${path}: fixed-index bus read remains`);
  }
  if (/ReadOnlyCount<_,(float4|string)>\(Collection=(RowList|TargetList)\) == 4/.test(graph)) {
    throw new Error(`${path}: fixed four-row count remains`);
  }
  if (/Output\.v2\/Row[0-3](Force|Vibration|Pain|Temperature)/.test(graph)) throw new Error(`${path}: scalar row DV remains`);
  if (/Output\.v2\/(Active|SelectedPluginId|ContractVersion|Connected|ConnectionStatusAvailable|Target[0-3])"/.test(graph)) throw new Error(`${path}: superseded bus DV remains`);
}

function check(path, expected) {
  const spec = JSON.parse(fs.readFileSync(new URL(`../${path}`, import.meta.url), "utf8"));
  const components = new Map(spec.slots.flatMap((slot) => (slot.components ?? []).map((component) => [component.alias, component])));
  const componentSlots = new Map(spec.slots.flatMap((slot) => (slot.components ?? []).map((component) => [component.alias, slot.path])));
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
  if (expected.transport === "osc") {
    expect("Sender disabled by default", value(components, "runtime.sender", "Enabled"), false);
    expect("Sender control binding", spec.fluxInputs.SenderEnabled?.fieldRef, "runtime.sender.Enabled");
    expect("Local sender default", value(components, "runtime.senderEnabledOverride", "Default"), false);
    expect("Sender override target", spec.fieldReferences.find(r => r.component === "runtime.senderEnabledOverride")?.fieldRef, "runtime.sender.Enabled");
    if (JSON.stringify(spec).includes("IsSending")) throw new Error(`${path}: Resonite-owned IsSending must not be written or overridden`);
  }
  if (expected.id === "io.github.byohaptics.output.joycon.osc") {
    for (const alias of components.keys()) {
      if (/^runtime\.row[23](Force|Vibration|Pain)(Value|ValueOverride|Field)$/.test(alias)) {
        throw new Error(`${path}: unused fixed-row Joy-Con transport remains: ${alias}`);
      }
    }
  }
  expect("RequireCredit", value(components, "package.license", "RequireCredit"), true);
  expect("CreditString", value(components, "package.license", "CreditString"), "byohaptics");
  expect("CanExport", value(components, "package.license", "CanExport"), false);
  expect("Card mask type", components.get("package.background.mask")?.type, "[FrooxEngine]FrooxEngine.UIX.Mask");
  expect("Card mask graphic", value(components, "package.background.mask", "ShowMaskGraphic"), true);
  expect("Accent mask parent", componentSlots.get("package.accent.image"), "Package/Visual/Background/Accent");
  if (expected.cardFields !== false) {
    expect("Card version", value(components, "package.version.text", "Content"), `v${expected.version}`);
    expect("Card contract", value(components, "package.contract.text", "Content"), "Contract: BYOHaptics.Output.v2");
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
if (!demoComponents.has("package.grabbable")) throw new Error("Demo package must remain grabbable");
for (const side of ["left", "right"]) {
  if (demoComponents.has(`device.${side}.grabbable`)) throw new Error(`Demo ${side} device must not be independently grabbable`);
}
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
if (demoSlot("Devices/Left")?.position?.x !== -0.34 || demoSlot("Devices/Right")?.position?.x !== 0.34) {
  throw new Error("Demo devices must remain close to and clear of the Host panel at x = +/-0.34");
}
for (const [side, path] of [["left", "Devices/Left/Visual"], ["right", "Devices/Right/Visual"]]) {
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
  checkRows(path, graph);
  if (path.includes("OSCOutput")) {
    if (graph.includes("IsSending")) throw new Error(`${path}: IsSending is status, not a control input`);
    for (const required of [
      "store StopTailRemaining: float;",
      "StopTailRemaining <- 0.0;",
      "SenderEnabled <- if ActiveState then true else StopTailRemaining > 0.0;",
      "StopTailRemaining <- if ActiveState then 0.25 else ValueMax(A=0.0, B=StopTailRemaining - DeltaTime);",
    ]) if (!graph.includes(required)) throw new Error(`${path}: missing bounded local shutdown: ${required}`);
    const inactive = graph.slice(graph.lastIndexOf("OnFalse=impulse {"));
    const channels = path.includes("JoyCon")
      ? ["Row0Force", "Row0Vibration", "Row0Pain", "Row1Force", "Row1Vibration", "Row1Pain"]
      : Array.from({length: 16}, (_, i) => `Channel${String(i).padStart(2, "0")}`);
    for (const channel of channels) {
      if (!inactive.includes(`${channel}Value <- 0.0`)) throw new Error(`${path}: shutdown must zero ${channel}`);
    }
    if (!inactive.includes("0.05")) throw new Error(`${path}: shutdown zero resend missing`);
  }
  if (!graph.includes(`ThisPluginId = "${id}"`)) throw new Error(`${path}: incorrect PluginId`);
  if (!graph.includes("IsUnderLocalUser = IsChildOf(Instance=PluginRoot") ||
      !graph.includes("CanManageRuntime = if PackageInstalledState then IsUnderLocalUser else true") ||
      !graph.includes("If(Condition=CanManageRuntime, OnTrue=impulse")) {
    throw new Error(`${path}: local-owner runtime gate missing`);
  }
  if (/BYOHaptics\.Output\.v1|BusContractVersion\.Value == 1/.test(graph)) throw new Error(`${path}: legacy contract remains`);
  if (path.includes("Haptira")) {
    if (!graph.includes("store ResendTailRemaining: float;") || graph.includes("DelayValue(")) {
      throw new Error(`${path}: resend must use a stored countdown, not a delayed pulse`);
    }
    for (const channel of Array.from({length: 16}, (_, i) => `Channel${String(i).padStart(2, "0")}`)) {
      if (!graph.includes(`${channel}Value <- ${channel}Output;`)) throw new Error(`${path}: output must preserve exact zero`);
    }
  }
  if (path.includes("Demo")) {
    for (const target of ["left", "right"]) {
      if (!graph.includes(`== "${target}"`)) throw new Error(`${path}: Target ${target} routing missing`);
    }
    if (!graph.includes("LeftWigglerMagnitude <- Pack_Float3") || !graph.includes("RightWigglerMagnitude <- Pack_Float3")) {
      throw new Error(`${path}: Wiggler magnitude writers missing`);
    }
    if (!graph.includes("LeftOutput * 30.0") || !graph.includes("RightOutput * 30.0")) {
      throw new Error(`${path}: visible thirty-degree Demo magnitude missing`);
    }
  }
}

const templatePath = "templates/output-plugin/minimal-output-plugin.resoslots.json";
const templateSpec = JSON.parse(fs.readFileSync(new URL(`../${templatePath}`, import.meta.url), "utf8"));
const templateComponents = new Map(templateSpec.slots.flatMap((slot) => (slot.components ?? []).map((component) => [component.alias, component])));
const templateId = value(templateComponents, "manifest.pluginId");
if (templateSpec.toolName !== templateId) throw new Error(`${templatePath}: toolName and PluginId differ`);
if (templateSpec.tag !== "byo-haptics.output-plugin") throw new Error(`${templatePath}: plugin tag missing`);
if (value(templateComponents, "manifest.contractVersion") !== versions.outputContract) throw new Error(`${templatePath}: contract version mismatch`);
const templateGraph = fs.readFileSync(new URL("../templates/output-plugin/MinimalOutputPlugin.pg", import.meta.url), "utf8");
checkRows("MinimalOutputPlugin.pg", templateGraph);
if (!templateGraph.includes(`ThisPluginId = "${templateId}"`)) throw new Error("MinimalOutputPlugin.pg: PluginId mismatch");
if (!templateGraph.includes("IsUnderLocalUser = IsChildOf(Instance=PluginRoot") ||
    !templateGraph.includes("CanManageRuntime = if InstalledState then IsUnderLocalUser else true") ||
    !templateGraph.includes("If(Condition=CanManageRuntime, OnTrue=impulse")) {
  throw new Error("MinimalOutputPlugin.pg: local-owner runtime gate missing");
}

console.log("Plugin Package manifests and defaults are consistent");
