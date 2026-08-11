param([switch]$Recreate)

$ErrorActionPreference = 'Stop'
$runner = "$PSScriptRoot/run-fluxsdk-script.ps1"

$env:RESONITE_SLOTSPEC_PATH = 'specs/byo-haptics.resoslots.json'
$env:RESONITE_SLOTSPEC_OUTPUT = 'build/byo-haptics.generated-ids.json'
$env:RESONITE_SLOTSPEC_RECREATE = if ($Recreate) { '1' } else { '0' }
& $runner -ScriptPath "$PSScriptRoot/build-from-resonite-slotspec.fsx"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$env:BYO_HAPTICS_DEPLOY = '1'
& $runner -ScriptPath "$PSScriptRoot/deploy-byo-haptics.fsx"
exit $LASTEXITCODE
