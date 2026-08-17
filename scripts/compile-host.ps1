$ErrorActionPreference = 'Stop'

$modules = @(
    'BYOHapticsLifecycle',
    'BYOHapticsPositioning',
    'BYOHapticsSampler',
    'BYOHapticsPluginDiscovery',
    'BYOHapticsPluginPackageManager',
    'BYOHapticsOutputBus'
)

foreach ($module in $modules) {
    $env:PROTOGRAPH_MODULE = "flux/$module"
    & "$PSScriptRoot/run-fluxsdk-script.ps1" -ScriptPath "$PSScriptRoot/compile-protograph-module.fsx"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
