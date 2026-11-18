$ErrorActionPreference = 'Stop'

foreach ($module in 'flux/BYOHapticsJoyConOSCOutput', 'flux/BYOHapticsHaptiraOSCOutput', 'flux/BYOHapticsDemoOutput', 'templates/output-plugin/MinimalOutputPlugin') {
    $env:PROTOGRAPH_MODULE = $module
    & "$PSScriptRoot/run-fluxsdk-script.ps1" -ScriptPath "$PSScriptRoot/compile-protograph-module.fsx"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
