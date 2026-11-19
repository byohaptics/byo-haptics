param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('joycon', 'haptira', 'demo')]
    [string]$Plugin,
    [switch]$Recreate
)

$ErrorActionPreference = 'Stop'
$runner = "$PSScriptRoot/run-fluxsdk-script.ps1"
$settings = @{
    joycon = @('specs/joycon-osc-plugin.resoslots.json', 'Flux/BYOHapticsJoyConOSCOutput', 'build/joycon-osc-plugin.generated-ids.json')
    haptira = @('specs/haptira-osc-plugin.resoslots.json', 'Flux/BYOHapticsHaptiraOSCOutput', 'build/haptira-osc-plugin.generated-ids.json')
    demo = @('specs/demo-output-plugin.resoslots.json', 'Flux/BYOHapticsDemoOutput', 'build/demo-output-plugin.generated-ids.json')
}[$Plugin]

$env:RESONITE_SLOTSPEC_PATH = $settings[0]
$env:RESONITE_SLOTSPEC_OUTPUT = $settings[2]
$env:RESONITE_SLOTSPEC_RECREATE = if ($Recreate) { '1' } else { '0' }
& node "$PSScriptRoot/validate-slotspec.mjs" $env:RESONITE_SLOTSPEC_PATH
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$env:PROTOGRAPH_MODULE = $settings[1]
& $runner -ScriptPath "$PSScriptRoot/compile-protograph-module.fsx"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $runner -ScriptPath "$PSScriptRoot/build-from-resonite-slotspec.fsx"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$env:OUTPUT_PLUGIN_MODULE = $settings[1]
$env:OUTPUT_PLUGIN_IDS = "$($settings[2]).pending.json"
$env:OUTPUT_PLUGIN_DEPLOY = '1'
& $runner -ScriptPath "$PSScriptRoot/deploy-output-plugin.fsx"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $runner -ScriptPath "$PSScriptRoot/finalize-slotspec-build.fsx"
exit $LASTEXITCODE
