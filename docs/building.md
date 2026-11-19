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

The hook runs the local publication policy and `npm test`. Keep the publication
rule file in a private folder outside the project, and register its absolute
path in your user-level Git configuration:

```powershell
git config --global byohaptics.publicationDenylistFile '<absolute path to private rule file>'
```

The UTF-8 file contains one non-empty ripgrep regular expression. Matching is
case-insensitive. Missing configuration, an unreadable file or an empty rule
stops the local publication check. Neither the rule nor its actual local path
belongs in the repository.

```powershell
npm test
npm run compile:host
npm run compile:plugins
```

`npm test` validates versions, documents, SlotSpecs including the authoring template, aliases, references, runtime-ID policy, and deployment script type checking. Type checks do not execute the deployment scripts. Use the SDK selected by `global.json`; the F# compiler in SDK `10.0.404` has shown type-check stalls with FluxSDK `1.9.0`. ProtoGraph compilation is separate because it is slower and produces diagnostics for each module.

To run the local publication check separately:

```powershell
node scripts/check-publication-policy.mjs
```

Hosted CI runs `npm test` without engine DLLs or access to the private rule
file. The real publication-policy check runs locally before publication;
GitHub Actions does not require a publication-policy Secret. Full
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

The [SlotSpec build procedure](slotspec.md#build-order) defines staging,
replacement boundaries, finalization and failure recovery.
[Generated IDs](slotspec.md#generated-ids) belong under `build/` and must not be
committed. Recreate resets item settings on success, so save custom settings
and contained plugins before replacement.

## External BYO Haptic Actuator plugin

The BHA plugin's sources and workflow are under `plugin/` in the separate
`byo-haptic-actuator` checkout. Run its commands there; its README describes
`BYO_HAPTICS_ROOT` for locating this repository's generic build tools.
The Host's test/version/compile commands do not depend on that checkout.
