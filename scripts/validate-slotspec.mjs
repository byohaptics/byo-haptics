import fs from "node:fs";

const specPath = process.argv[2];
if (!specPath) throw new Error("Usage: node scripts/validate-slotspec.mjs <spec>");

const spec = JSON.parse(fs.readFileSync(specPath, "utf8").replace(/^\uFEFF/, ""));
const errors = [];
const slots = spec.slots ?? [];
const components = slots.flatMap((slot) => slot.components ?? []);
const paths = slots.map((slot) => slot.path);
const aliases = components.map((component) => component.alias);
const pathSet = new Set([""]);
for (const path of paths) {
  const parts = path.split("/").filter(Boolean);
  for (let length = 1; length <= parts.length; length += 1) {
    pathSet.add(parts.slice(0, length).join("/"));
  }
}
const aliasSet = new Set(aliases);
const supportedTypes = new Set([
  "bool", "int", "float", "string", "uri", "float2", "float3", "float4", "floatQ",
  "colorX", "enum", "texture2DRawData", "stringList", "float3List", "float4List", "syncObject",
  "componentRef", "componentRefList",
]);

const duplicates = (values) => [...new Set(values.filter((value, index) => values.indexOf(value) !== index))];
const checkAlias = (alias, context) => {
  if (!aliasSet.has(alias)) errors.push(`${context}: missing component ${alias}`);
};
const fieldAlias = (reference) => aliases
  .filter((alias) => reference.startsWith(`${alias}.`))
  .sort((left, right) => right.length - left.length)[0];
function checkField(reference, context) {
  const alias = typeof reference === "string" && fieldAlias(reference);
  if (!alias) {
    errors.push(`${context}: unresolved ${reference}`);
    return;
  }
  const [name, index, ...tail] = reference.slice(alias.length + 1).split(".");
  const member = components.find(c => c.alias === alias)?.members?.[name];
  if (member?.type?.endsWith("List") && index !== undefined &&
      (!/^\d+$/.test(index) || Number(index) >= member.value?.length || tail.length)) {
    errors.push(`${context}: invalid list element ${reference}`);
  }
}

function checkMember(member, context) {
  if (!member || !supportedTypes.has(member.type)) {
    errors.push(`${context}: unsupported member type ${member?.type ?? "<missing>"}`);
    return;
  }
  if (member.type === "componentRef") checkAlias(member.component, context);
  if (member.type === "float4List" && (!Array.isArray(member.value) ||
      member.value.some((v) => !v || ["x", "y", "z", "w"].some((lane) => !Number.isFinite(v[lane]))))) {
    errors.push(`${context}: expected an array of float4 values`);
  }
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
  const references = entry.fieldRef === undefined
    ? entry.fieldRefList
    : entry.fieldRefList === undefined ? [entry.fieldRef] : undefined;
  if (!Array.isArray(references)) {
    errors.push("fieldReferences: expected fieldRef or an array fieldRefList");
  } else {
    for (const reference of references) {
      checkField(reference, "fieldReferences");
    }
  }
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
  if (input.fieldRef) checkField(input.fieldRef, `fluxInputs.${name}`);
  if (input.componentRef) checkAlias(input.componentRef, `fluxInputs.${name}`);
  if (input.slotPath !== undefined && !pathSet.has(input.slotPath)) errors.push(`fluxInputs.${name}: missing slot ${input.slotPath}`);
  if (input.slotField && !pathSet.has(input.slotField.slotPath)) {
    errors.push(`fluxInputs.${name}: missing slot ${input.slotField.slotPath}`);
  }
}

if (errors.length) throw new Error(errors.join("\n"));
console.log(`Valid ${specPath}: slots=${slots.length} components=${components.length}`);
