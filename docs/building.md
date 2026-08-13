# Building

Document version: `0.1.1`

## Requirements

- Windows 10 or later
- PowerShell 7 or Windows PowerShell 5.1
- Node.js 22
- .NET SDK 10
- `Papaltine.FluxSDK` `1.8.0`
- Current stable Rust with `rustfmt` and `clippy`

Install the FluxSDK tool globally:

```powershell
dotnet tool install --global Papaltine.FluxSDK --version 1.8.0
```

## Static Verification

Maintainers should enable the tracked pre-commit gate once per clone:

```powershell
git config core.hooksPath .githooks
```

The hook runs the publication policy and `npm test`. It requires `PUBLICATION_DENYLIST` in the process environment.

```powershell
npm test
npm run lint:bridge
npm run compile:host
npm run compile:plugins
```

`npm test` validates versions, documents, SlotSpecs, aliases, references, runtime-ID policy, deployment script type checking, and all Bridge tests. ProtoGraph compilation is separate because it is slower and produces diagnostics for each module.

The publication-policy check also requires `PUBLICATION_DENYLIST` to be supplied outside the repository:

```powershell
$env:PUBLICATION_DENYLIST = '<maintainer-provided expression>'
node scripts/check-publication-policy.mjs
```

The GitHub repository must define the same value as the Actions repository variable `PUBLICATION_DENYLIST` before CI is enabled.

## Bridge Package

```powershell
npm run build:bridge
```

This creates `build/joycon-rumble-bridge-v<version>-windows-x64.zip` containing the CLI, GUI, README, and an automatic-binding configuration example. A matching `.zip.sha256` file is generated beside it. The archive does not contain controller addresses or generated calibration data.

## Scene Deployment

Deployment requires a running Resonite session with ResoniteLink available to the current user:

```powershell
npm run deploy:host
npm run deploy:joycon
npm run deploy:haptira
```

Deployment creates fresh runtime IDs under `build/`. These files are disposable and must not be committed.
