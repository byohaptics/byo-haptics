import fs from "node:fs";

const spec = JSON.parse(fs.readFileSync(new URL("../specs/byo-haptics.resoslots.json", import.meta.url), "utf8"));
const samplerGraph = fs.readFileSync(new URL("../flux/BYOHapticsSampler.pg", import.meta.url), "utf8");
const outputBusGraph = fs.readFileSync(new URL("../flux/BYOHapticsOutputBus.pg", import.meta.url), "utf8");
const version = JSON.parse(fs.readFileSync(new URL("../versions.json", import.meta.url), "utf8")).product;
const components = new Map(
  spec.slots.flatMap((slot) => (slot.components ?? []).map((component) => [component.alias, component])),
);
const slots = new Map(spec.slots.map((slot) => [slot.path, slot]));
const componentSlot = new Map(
  spec.slots.flatMap((slot) => (slot.components ?? []).map((component) => [component.alias, slot.path])),
);
const fieldReference = (component, member) =>
  spec.fieldReferences.find((reference) => reference.component === component && reference.member === member);
const slotFieldReference = (component, member) =>
  spec.slotFieldReferences.find((reference) => reference.component === component && reference.member === member);

const value = (alias, member = "Value") => components.get(alias)?.members?.[member]?.value;
const expect = (label, actual, expected) => {
  if (actual !== expected) throw new Error(`${label}: ${JSON.stringify(actual)} != ${JSON.stringify(expected)}`);
};

expect("toolName", spec.toolName, "BYOHaptics");
expect("toolParent", spec.toolParent?.kind, "worldRoot");
expect("Panel title", value("ui.title.text", "Content"), `BYO Haptics v${version}`);
expect("Contract version", value("output.bus.contractVersion.variable"), 1);
expect("Contract namespace", value("output.pluginSocket.space", "SpaceName"), "BYOHaptics.Output.v1");
expect("Minimum pulse duration", value("config.minimumPulseDuration"), 0.08);
expect("Sampler user override owner", componentSlot.get("samplers.userOverride"), "");
expect("Sampler active ProtoFlux slot", spec.fluxInputs.SamplersActive?.slotField?.slotPath, "Samplers");
expect("Sampler active ProtoFlux member", spec.fluxInputs.SamplersActive?.slotField?.slotMember, "IsActive");
expect("Sampler active override target", slotFieldReference("samplers.userOverride", "Target")?.slotPath, "Samplers");
const diagnosticAliases = [
  "diagnostics.plugin.present",
  "diagnostics.plugin.connected",
  "diagnostics.plugin.disconnected",
];
for (const alias of diagnosticAliases) expect(`${alias} owner`, componentSlot.get(alias), "Diagnostics/Plugin");
if ([...slots.keys()].some((path) => path.startsWith("Diagnostics/") && path !== "Diagnostics/Plugin")) {
  throw new Error("Unused read-only diagnostic mirror slots must not be present");
}

for (const alias of [
  "config.installed",
  "config.panelVisible",
  ...[0, 1, 2, 3].flatMap((row) => [
    `config.row${row}SamplerEdit`,
    `config.row${row}SamplerOffset`,
    `config.row${row}SourceValid`,
    `config.row${row}UseBodyNode`,
  ]),
]) expect(`${alias} owner`, componentSlot.get(alias), "Config/State");

for (const alias of [...components.keys()].filter((alias) => alias.startsWith("output.bus."))) {
  expect(`${alias} owner`, componentSlot.get(alias), "Outputs/PluginSocket/Bus");
}

for (const row of [0, 1, 2, 3]) {
  expect(`Row ${row} hold vector type`, components.get(`runtime.row${row}HoldRemaining`)?.type, "[FrooxEngine]FrooxEngine.ValueField<float4>");
  expect(`Row ${row} hold vector input`, spec.fluxInputs[`Row${row}HoldRemaining`]?.fieldRef, `runtime.row${row}HoldRemaining.Value`);
  for (const sensation of ["Force", "Vibration", "Pain", "Temperature"]) {
    if (components.has(`runtime.row${row}${sensation}HoldRemaining`) || spec.fluxInputs[`Row${row}${sensation}HoldRemaining`]) {
      throw new Error(`Row ${row} ${sensation} scalar hold state must not remain`);
    }
    if (!outputBusGraph.includes(`if Row${row}${sensation}Out > 0.0 then HoldWindow else`)) {
      throw new Error(`Row ${row} ${sensation} must reset only its own hold lane`);
    }
  }
  if (!outputBusGraph.includes(`in Row${row}HoldRemaining: float4 mutable`)) throw new Error(`Row ${row} float4 hold input missing`);
  if (!outputBusGraph.includes(`pack(Row${row}Force, Row${row}Vibration, Row${row}Pain, Row${row}Temperature)`)) {
    throw new Error(`Row ${row} sensation lane order must be Force, Vibration, Pain, Temperature`);
  }
}

if (outputBusGraph.includes("SecondsTimer") || outputBusGraph.includes("MinimumPulseDuration + 0.01")) {
  throw new Error("Output Bus must use LocalUpdate and DeltaTime instead of the fixed 10 ms timer");
}
if (!outputBusGraph.includes("FrameTime = DeltaTime")) throw new Error("Output Bus DeltaTime countdown missing");

for (const [row, target] of ["left", "right", "head", "hips"].entries()) {
  expect(`Row ${row} Target`, value(`row${row}.target`), target);
  expect(`Row ${row} Target label`, value(`ui.row${row}.targetField.text.text`, "Content"), target);
  expect(`Row ${row} Node mode`, value(`config.row${row}UseBodyNode`), true);
  expect(`Row ${row} BodyNode`, value(`row${row}.selectedBodyNode`), "NONE");
  expect(`Row ${row} Visual`, value(`row${row}.sampler`, "ShowDebugVisual"), false);
  expect(`Row ${row} index label`, value(`ui.row${row}.index.text`, "Content"), `${row}:`);
  expect(`Row ${row} index font size`, value(`ui.row${row}.index.text`, "Size"), 32);
  expect(`Row ${row} index width`, value(`ui.row${row}.index.layoutElement`, "PreferredWidth"), 60);
  expect(`Row ${row} Target wrapper`, componentSlot.get(`ui.row${row}.targetField.button`), `UI/Vertical layout/Main Area/Row_${String(row).padStart(3, "0")}/TargetFieldArea`);
  expect(`Row ${row} Visual wrapper`, componentSlot.get(`ui.row${row}.visual.checkbox`), `UI/Vertical layout/Main Area/Row_${String(row).padStart(3, "0")}/Visual`);
  expect(`Row ${row} Node wrapper`, componentSlot.get(`ui.row${row}.mode.checkbox`), `UI/Vertical layout/Main Area/Row_${String(row).padStart(3, "0")}/SourceFieldArea/Mode`);
  expect(
    `Row ${row} index order`,
    slots.get(`UI/Vertical layout/Main Area/Row_${String(row).padStart(3, "0")}/Index`)?.orderOffset,
    -10,
  );
  expect(`Row ${row} Sampler Edit label`, value(`context.row${row}SamplerEdit.source`, "Label"), `${row}`);
  const rowPath = `UI/Vertical layout/Main Area/Row_${String(row).padStart(3, "0")}/SourceFieldArea/Clear`;
  expect(`Row ${row} BodyNode clear owner`, componentSlot.get(`ui.row${row}.clear.bodyNodeSet`), rowPath);
  expect(`Row ${row} BodyNode clear value`, value(`ui.row${row}.clear.bodyNodeSet`, "SetValue"), "NONE");
  expect(
    `Row ${row} BodyNode clear target`,
    fieldReference(`ui.row${row}.clear.bodyNodeSet`, "TargetValue")?.fieldRef,
    `row${row}.selectedBodyNode.Value`,
  );
  expect(`Row ${row} manual Source clear owner`, componentSlot.get(`ui.row${row}.clear.manualSourceSet`), rowPath);
  expect(
    `Row ${row} manual Source clear target`,
    fieldReference(`ui.row${row}.clear.manualSourceSet`, "TargetReference")?.fieldRef,
    `row${row}.source.Reference`,
  );
}

if (samplerGraph.includes("BodyNode.NONE") || samplerGraph.includes("null<Slot>")) {
  throw new Error("Sampler clear must not emit unsupported ProtoFlux constant nodes");
}

expect("Header index width", value("ui.columnHeader.index.layoutElement", "PreferredWidth"), 60);

if (spec.slots.length > 174) throw new Error(`Compacted Host slot budget exceeded: ${spec.slots.length}`);
if (components.size > 597) throw new Error(`Compacted Host component budget exceeded: ${components.size}`);

expect("RequireCredit", value("tool.license", "RequireCredit"), true);
expect("CreditString", value("tool.license", "CreditString"), "byohaptics");
expect("CanExport", value("tool.license", "CanExport"), false);

const serialized = JSON.stringify(spec);
for (const alias of ["config.enabled", "config.sendInterval", "config.defaultRadius", "config.version"]) {
  if (components.has(alias)) throw new Error(`Unused Host state must not be present: ${alias}`);
}
for (const row of [0, 1, 2, 3]) {
  if (components.has(`config.row${row}SourceConfigured`) || spec.fluxInputs[`Row${row}SourceConfigured`]) {
    throw new Error(`Row ${row} constant SourceConfigured state must not be present`);
  }
  if (spec.fluxInputs[`OutputBusTarget${row}`]) throw new Error(`Row ${row} unused OutputBusTarget input must not be present`);
}
if (/PluginError|plugin\.error|pluginActiveLed\.error/i.test(serialized)) {
  throw new Error("Dead PluginError state must not be present");
}
if (serialized.includes("BYOHaptics.Output.v2")) throw new Error("Legacy output contract v2 is present");
if (/context\.row\dSamplerEdit\.targetLabel\.copy/.test(serialized)) {
  throw new Error("Sampler Edit labels must not be copied from Target");
}

console.log("Host SlotSpec defaults and public contract are consistent");
