$ErrorActionPreference = 'Stop'

foreach ($module in 'BYOHapticsJoyConOSCOutput', 'BYOHapticsHaptiraOSCOutput', 'BYOHapticsDemoOutput') {
    $env:PROTOGRAPH_MODULE = "flux/$module"
    & "$PSScriptRoot/run-fluxsdk-script.ps1" -ScriptPath "$PSScriptRoot/compile-protograph-module.fsx"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
