# Building

Document version: `0.2.0`

## Requirements

- Windows 10 or later
- PowerShell 7 or Windows PowerShell 5.1
- Node.js 22
- ripgrep (`rg`) available on `PATH`
- .NET SDK `10.0.115`, selected by `global.json`
- `Papaltine.FluxSDK` `1.9.0`
- A local Resonite installation for ProtoGraph compilation; set `RESONITE_DIR`
  when it is not in the default Steam library. Resonite need not be running to compile.

Install the FluxSDK tool globally:

```powershell
dotnet tool install --global Papaltine.FluxSDK --version 1.9.0
```

## Static Verification

Maintainers should enable the tracked pre-commit gate once per clone:

```powershell
git config core.hooksPath .githooks
```

The hook runs the publication policy and `npm test`. It requires `PUBLICATION_DENYLIST` in the process environment.

```powershell
npm test
npm run compile:host
npm run compile:plugins
```

`npm test` validates versions, documents, SlotSpecs including the authoring template, aliases, references, runtime-ID policy, and deployment script type checking. Type checks do not execute the deployment scripts. Use the SDK selected by `global.json`; the F# compiler in SDK `10.0.404` has shown type-check stalls with FluxSDK `1.9.0`. ProtoGraph compilation is separate because it is slower and produces diagnostics for each module.

The publication-policy check also requires `PUBLICATION_DENYLIST` to be supplied outside the repository:

```powershell
$env:PUBLICATION_DENYLIST = '<maintainer-provided expression>'
node scripts/check-publication-policy.mjs
```

Keep the expression outside the project folder and supply it through the process environment. For GitHub Actions, define it as the repository secret `PUBLICATION_DENYLIST` before CI is enabled. The expression itself must not be committed. Fork pull requests do not receive this secret and cannot pass the publication gate until reviewed and run in a trusted context.

Hosted CI runs `npm test` and publication checks without engine DLLs. Full
ProtoGraph compilation is a separate, manual `validate` workflow run on `main`,
using a trusted Windows x64 self-hosted runner labeled `resonite` with Resonite
installed. Set the Actions variable `RESONITE_DIR` to its installation directory
when needed. Never run untrusted pull-request code on that runner. The full
compile remains a release gate; passing hosted CI alone does not satisfy it.

## Joy-Con Bridge

The Windows application is built and released from [byohaptics/joycon-bridge](https://github.com/byohaptics/joycon-bridge). This repository owns the [Bridge API contract](joycon-bridge-contract.md) and Joy-Con Output Plugin, not the Bridge executable.

## Scene Deployment

Deployment requires a running Resonite session with ResoniteLink available to the current user:

Set `RESONITELINK_URL` to the endpoint shown by ResoniteLink for that session.
Replace `<port>` with its current port; there is no fixed deployment port.

```powershell
$env:RESONITELINK_URL = 'ws://localhost:<port>'
npm run deploy:host
npm run deploy:joycon
npm run deploy:haptira
npm run deploy:demo
```

See [Output Plugin Authoring](plugin-authoring.md) for validating and deploying an arbitrary plugin outside the three built-in wrappers.

To replace an existing Plugin Package on Windows, call the wrapper directly so the switch is preserved:

```powershell
scripts/deploy-plugin.ps1 -Plugin demo -Recreate
```

Deployment creates fresh runtime IDs under `build/`. These files are disposable and must not be committed.

Wrappers validate and compile before scene construction. Replacement is limited
to one matching direct child of the declared parent; installed avatar items are
not searched. The previous item stays intact until the inactive replacement and
all its graphs are ready. On failure, inspect the inactive staging item; the
previous generated IDs are not overwritten. Recreate still resets item settings
on success, so save any custom settings or contained plugins before replacement.

## External BYO Haptic Actuator plugin

The BHA plugin's sources and workflow are under `plugin/` in the separate
`byo-haptic-actuator` checkout. Run its commands there; its README describes
`BYO_HAPTICS_ROOT` for locating this repository's generic build tools.
The Host's test/version/compile commands do not depend on that checkout.
