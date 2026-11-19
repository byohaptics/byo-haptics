param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('host', 'plugins')]
    [string]$Target
)

$ErrorActionPreference = 'Stop'
$modules = @{
    host = @(
        'flux/BYOHapticsLifecycle',
        'flux/BYOHapticsPositioning',
        'flux/BYOHapticsSampler',
        'flux/BYOHapticsPluginDiscovery',
        'flux/BYOHapticsPluginPackageManager',
        'flux/BYOHapticsOutputBus'
    )
    plugins = @(
        'flux/BYOHapticsJoyConOSCOutput',
        'flux/BYOHapticsHaptiraOSCOutput',
        'flux/BYOHapticsDemoOutput',
        'templates/output-plugin/MinimalOutputPlugin'
    )
}

foreach ($module in $modules[$Target]) {
    $env:PROTOGRAPH_MODULE = $module
    & "$PSScriptRoot/run-fluxsdk-script.ps1" -ScriptPath "$PSScriptRoot/compile-protograph-module.fsx"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
