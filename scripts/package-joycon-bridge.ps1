$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$bridge = Join-Path $repository 'bridges\joycon-rumble'
$build = Join-Path $repository 'build'
$versions = Get-Content -LiteralPath (Join-Path $repository 'versions.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$version = $versions.bridges.joyconRumble
$package = Join-Path $build "BYO-Haptics-Joy-Con-Bridge-v$version.exe"
$userProfile = [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)
$separator = [char]0x1f
$env:CARGO_ENCODED_RUSTFLAGS = @(
    "--remap-path-prefix=$repository=."
    "--remap-path-prefix=$userProfile=<home>"
) -join $separator

& cargo build --manifest-path (Join-Path $bridge 'Cargo.toml') --release --bin joycon-rumble-gui
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$source = Join-Path $bridge 'target\release\joycon-rumble-gui.exe'
$bytes = [System.IO.File]::ReadAllBytes($source)
$ascii = [System.Text.Encoding]::ASCII.GetString($bytes)
$unicode = [System.Text.Encoding]::Unicode.GetString($bytes)
foreach ($privatePath in @($userProfile, $repository)) {
    if ($ascii.Contains($privatePath) -or $unicode.Contains($privatePath)) {
        throw "Joy-Con Bridge contains a private build path"
    }
}

if (-not (Test-Path -LiteralPath $build)) { New-Item -ItemType Directory -Path $build | Out-Null }
if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package }
Copy-Item -LiteralPath $source -Destination $package

$checksum = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$package.sha256" -Encoding ASCII -Value "$checksum  $(Split-Path -Leaf $package)"
Write-Output "Created $package"
