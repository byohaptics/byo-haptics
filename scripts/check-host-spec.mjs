import fs from "node:fs";

const spec = JSON.parse(fs.readFileSync(new URL("../specs/byo-haptics.resoslots.json", import.meta.url), "utf8"));
const version = JSON.parse(fs.readFileSync(new URL("../versions.json", import.meta.url), "utf8")).product;
const components = new Map(
  spec.slots.flatMap((slot) => (slot.components ?? []).map((component) => [component.alias, component])),
);
const slots = new Map(spec.slots.map((slot) => [slot.path, slot]));

const value = (alias, member = "Value") => components.get(alias)?.members?.[member]?.value;
const expect = (label, actual, expected) => {
  if (actual !== expected) throw new Error(`${label}: ${JSON.stringify(actual)} != ${JSON.stringify(expected)}`);
};

expect("toolName", spec.toolName, "BYOHaptics");
expect("toolParent", spec.toolParent?.kind, "worldRoot");
expect("Config version", value("config.version"), `v${version}`);
expect("Panel title", value("ui.title.text", "Content"), `BYO Haptics v${version}`);
expect("Contract version", value("output.bus.contractVersion.variable"), 1);
expect("Contract namespace", value("output.pluginSocket.space", "SpaceName"), "BYOHaptics.Output.v1");

for (const [row, target] of ["left", "right", "head", "hips"].entries()) {
  expect(`Row ${row} Target`, value(`row${row}.target`), target);
  expect(`Row ${row} Target label`, value(`ui.row${row}.targetField.text.text`, "Content"), target);
  expect(`Row ${row} Node mode`, value(`config.row${row}UseBodyNode`), true);
  expect(`Row ${row} BodyNode`, value(`row${row}.selectedBodyNode`), "NONE");
  expect(`Row ${row} Visual`, value(`row${row}.sampler`, "ShowDebugVisual"), false);
  expect(`Row ${row} index label`, value(`ui.row${row}.index.text`, "Content"), `${row}:`);
  expect(
    `Row ${row} index order`,
    slots.get(`UI/Vertical layout/Main Area/Row_${String(row).padStart(3, "0")}/Index`)?.orderOffset,
    -10,
  );
  expect(`Row ${row} Sampler Edit label`, value(`context.row${row}SamplerEdit.source`, "Label"), `${row}`);
}

expect("RequireCredit", value("tool.license", "RequireCredit"), true);
expect("CreditString", value("tool.license", "CreditString"), "byohaptics");
expect("CanExport", value("tool.license", "CanExport"), false);

const serialized = JSON.stringify(spec);
if (/PluginError|plugin\.error|pluginActiveLed\.error/i.test(serialized)) {
  throw new Error("Dead PluginError state must not be present");
}
if (serialized.includes("BYOHaptics.Output.v2")) throw new Error("Legacy output contract v2 is present");
if (/context\.row\dSamplerEdit\.targetLabel\.copy/.test(serialized)) {
  throw new Error("Sampler Edit labels must not be copied from Target");
}

console.log("Host SlotSpec defaults and public contract are consistent");
