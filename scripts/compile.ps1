param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('host', 'plugins')]
    [string]$Target
)

$ErrorActionPreference = 'Stop'
$modules = @{
    host = @(
        'Flux/BYOHapticsLifecycle',
        'Flux/BYOHapticsPositioning',
        'Flux/BYOHapticsSampler',
        'Flux/BYOHapticsPluginDiscovery',
        'Flux/BYOHapticsPluginPackageManager',
        'Flux/BYOHapticsOutputBus'
    )
    plugins = @(
        'Flux/BYOHapticsJoyConOSCOutput',
        'Flux/BYOHapticsHaptiraOSCOutput',
        'Flux/BYOHapticsDemoOutput',
        'templates/output-plugin/MinimalOutputPlugin'
    )
}

foreach ($module in $modules[$Target]) {
    $env:PROTOGRAPH_MODULE = $module
    & "$PSScriptRoot/run-fluxsdk-script.ps1" -ScriptPath "$PSScriptRoot/compile-protograph-module.fsx"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
