# Building

Document version: `0.1.9`

## Requirements

- Windows 10 or later
- PowerShell 7 or Windows PowerShell 5.1
- Node.js 22
- .NET SDK 10
- `Papaltine.FluxSDK` `1.8.0`

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
npm run compile:host
npm run compile:plugins
```

`npm test` validates versions, documents, SlotSpecs including the authoring template, aliases, references, runtime-ID policy, and deployment script type checking. ProtoGraph compilation is separate because it is slower and produces diagnostics for each module.

The publication-policy check also requires `PUBLICATION_DENYLIST` to be supplied outside the repository:

```powershell
$env:PUBLICATION_DENYLIST = '<maintainer-provided expression>'
node scripts/check-publication-policy.mjs
```

The GitHub repository must define the same value as the Actions repository variable `PUBLICATION_DENYLIST` before CI is enabled.

## Joy-Con Bridge

The Windows application is built and released from [byohaptics/joycon-bridge](https://github.com/byohaptics/joycon-bridge). This repository owns the [Bridge API contract](joycon-bridge-contract.md) and Joy-Con Output Plugin, not the Bridge executable.

## Scene Deployment

Deployment requires a running Resonite session with ResoniteLink available to the current user:

```powershell
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
