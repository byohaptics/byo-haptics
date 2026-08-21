import fs from "node:fs";

const required = [
  "README.md",
  "AGENTS.md",
  "CONTRIBUTING.md",
  "SECURITY.md",
  "docs/project-scope.md",
  "docs/architecture.md",
  "docs/building.md",
  "docs/output-plugin-contract.md",
  "docs/output-plugin-package.md",
  "docs/plugin-authoring.md",
  "docs/joycon-plugin.md",
  "docs/joycon-bridge-contract.md",
  "docs/haptira-plugin.md",
  "docs/slotspec.md",
  "docs/host-ui.md",
  "docs/implementation-map.md",
  "docs/lifecycle.md",
  "docs/limitations.md",
  "docs/sampler.md",
  "docs/development-loop.md",
  "docs/todo.md",
  "docs/test-plan.md",
  "docs/test-results.md",
  "docs/current-handoff.md",
  "docs/release-checklist.md",
  "docs/versioning.md",
];

const missing = required.filter((path) => !fs.existsSync(new URL(`../${path}`, import.meta.url)));
if (missing.length) throw new Error(`Missing required documents: ${missing.join(", ")}`);

console.log(`Required documents are present: ${required.length}`);
