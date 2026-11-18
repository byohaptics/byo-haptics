# Versioning

Document version: `0.2.0`

`versions.json` is the only version source of truth.

- `product`: BYO Haptics host and repository release
- `documents`: public specification revision
- `outputContract`: runtime Output Bus compatibility integer
- `plugins.joyconOsc`: Joy-Con OSC Plugin Package
- `plugins.haptiraOsc`: Haptira OSC Plugin Package
- `plugins.demo`: Demo Output Plugin Package
- `bridgeApis.joyconOsc`: Joy-Con Bridge API defined by `docs/joycon-bridge-contract.md`

Product, documents, plugins, and Bridge APIs use semantic versioning and may advance independently. The Bridge application is versioned in its own repository. An incompatible runtime contract increments `outputContract` and changes the Output Bus namespace.

Use `scripts/set-version.mjs` for semantic versions. Change `outputContract` only in the same commit that updates the contract specification, host, all built-in plugins, validators, and migration notes.

Product-version updates synchronize package metadata, ProtoGraph metadata, the
Host title, the documented Host title and the README development version. The
Host title retains the current Output Contract label and its 65% size markup.
Independent plugin, document and Bridge API versions are not changed by a
product-only update. Versioning does not publish or tag a release automatically.
