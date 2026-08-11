param(
    [Parameter(Mandatory = $true)]
    [string]$ScriptPath,
    [string[]]$ScriptArguments = @(),
    [switch]$TypeCheckOnly
)

$ErrorActionPreference = 'Stop'

if ($env:FLUXSDK_TOOLS_DIR) {
    $fluxSdkTools = $env:FLUXSDK_TOOLS_DIR
} else {
    $store = Join-Path $HOME '.dotnet\tools\.store\papaltine.fluxsdk'
    if (-not (Test-Path -LiteralPath $store)) {
        throw 'FluxSDK was not found. Install papaltine.fluxsdk or set FLUXSDK_TOOLS_DIR.'
    }

    $fluxSdkTools = Get-ChildItem -LiteralPath $store -Recurse -Filter 'FluxSDK.Build.dll' -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1 -ExpandProperty DirectoryName
}

if (-not $fluxSdkTools -or -not (Test-Path -LiteralPath (Join-Path $fluxSdkTools 'FluxSDK.Build.dll'))) {
    throw "FLUXSDK_TOOLS_DIR does not contain FluxSDK.Build.dll: $fluxSdkTools"
}

$resolvedScript = (Resolve-Path -LiteralPath $ScriptPath).Path
$fsiArguments = @("--lib:$fluxSdkTools")
if ($TypeCheckOnly) { $fsiArguments += '--typecheck-only' }
$fsiArguments += $resolvedScript
& dotnet fsi @fsiArguments @ScriptArguments
exit $LASTEXITCODE
