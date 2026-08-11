import fs from "node:fs";

const specPath = process.argv[2];
if (!specPath) throw new Error("Usage: node scripts/validate-slotspec.mjs <spec>");

const spec = JSON.parse(fs.readFileSync(specPath, "utf8").replace(/^\uFEFF/, ""));
const errors = [];
const slots = spec.slots ?? [];
const components = slots.flatMap((slot) => slot.components ?? []);
const paths = slots.map((slot) => slot.path);
const aliases = components.map((component) => component.alias);
const pathSet = new Set(paths);
const aliasSet = new Set(aliases);
const supportedTypes = new Set([
  "bool", "int", "float", "string", "uri", "float2", "float3", "float4", "floatQ",
  "colorX", "enum", "texture2DRawData", "stringList", "float3List", "syncObject",
  "componentRef", "componentRefList",
]);

const duplicates = (values) => [...new Set(values.filter((value, index) => values.indexOf(value) !== index))];
const checkAlias = (alias, context) => {
  if (!aliasSet.has(alias)) errors.push(`${context}: missing component ${alias}`);
};
const fieldAlias = (reference) => aliases
  .filter((alias) => reference.startsWith(`${alias}.`))
  .sort((left, right) => right.length - left.length)[0];

function checkMember(member, context) {
  if (!member || !supportedTypes.has(member.type)) {
    errors.push(`${context}: unsupported member type ${member?.type ?? "<missing>"}`);
    return;
  }
  if (member.type === "componentRef") checkAlias(member.component, context);
  if (member.type === "componentRefList") {
    for (const alias of member.components ?? []) checkAlias(alias, context);
  }
  if (member.type === "syncObject") {
    for (const [name, nested] of Object.entries(member.value ?? {})) {
      checkMember(nested, `${context}.${name}`);
    }
  }
}

if (spec.schema !== "resonite-slotspec/v1") errors.push(`unsupported schema ${spec.schema}`);
if (!spec.toolName) errors.push("missing toolName");
if (!["worldRoot", "avatarRoot"].includes(spec.toolParent?.kind)) errors.push("invalid toolParent.kind");
for (const path of duplicates(paths)) errors.push(`duplicate slot path ${path}`);
for (const alias of duplicates(aliases)) errors.push(`duplicate component alias ${alias}`);

for (const component of components) {
  if (!component.alias || !component.type) errors.push("component requires alias and type");
  for (const [name, member] of Object.entries(component.members ?? {})) {
    checkMember(member, `${component.alias}.${name}`);
  }
}

for (const entry of spec.fieldReferences ?? []) {
  checkAlias(entry.component, "fieldReferences");
  if (!fieldAlias(entry.fieldRef ?? "")) errors.push(`fieldReferences: unresolved ${entry.fieldRef}`);
}
for (const entry of spec.componentReferences ?? []) {
  checkAlias(entry.component, "componentReferences");
  checkAlias(entry.componentRef, "componentReferences target");
}
for (const collection of ["localSlotReferences", "slotFieldReferences"]) {
  for (const entry of spec[collection] ?? []) {
    checkAlias(entry.component, collection);
    if (!pathSet.has(entry.slotPath)) errors.push(`${collection}: missing slot ${entry.slotPath}`);
  }
}
for (const collection of ["sceneSlotReferences", "sceneComponentReferences"]) {
  for (const entry of spec[collection] ?? []) checkAlias(entry.component, collection);
}

for (const [name, input] of Object.entries(spec.fluxInputs ?? {})) {
  const kinds = ["fieldRef", "componentRef", "slotPath", "slotField"].filter((key) => input[key] !== undefined);
  if (kinds.length !== 1) errors.push(`fluxInputs.${name}: expected exactly one source`);
  if (input.fieldRef && !fieldAlias(input.fieldRef)) errors.push(`fluxInputs.${name}: unresolved ${input.fieldRef}`);
  if (input.componentRef) checkAlias(input.componentRef, `fluxInputs.${name}`);
  if (input.slotPath !== undefined && !pathSet.has(input.slotPath)) errors.push(`fluxInputs.${name}: missing slot ${input.slotPath}`);
  if (input.slotField && !pathSet.has(input.slotField.slotPath)) {
    errors.push(`fluxInputs.${name}: missing slot ${input.slotField.slotPath}`);
  }
}

if (errors.length) throw new Error(errors.join("\n"));
console.log(`Valid ${specPath}: slots=${slots.length} components=${components.length}`);
