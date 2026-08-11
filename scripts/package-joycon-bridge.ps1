$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$bridge = Join-Path $repository 'bridges\joycon-rumble'
$versions = Get-Content -LiteralPath (Join-Path $repository 'versions.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$version = $versions.bridges.joyconRumble
$stage = Join-Path $repository "build\joycon-rumble-bridge-v$version-windows-x64"
$archive = "$stage.zip"
$separator = [char]0x1f
$env:CARGO_ENCODED_RUSTFLAGS = @(
    "--remap-path-prefix=$repository=."
    "--remap-path-prefix=$HOME=<home>"
) -join $separator

& cargo build --manifest-path (Join-Path $bridge 'Cargo.toml') --release --bins
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse }
New-Item -ItemType Directory -Path $stage | Out-Null

foreach ($name in 'joycon-rumble-bridge.exe', 'joycon-rumble-gui.exe') {
    $source = Join-Path $bridge "target\release\$name"
    $bytes = [System.IO.File]::ReadAllBytes($source)
    $ascii = [System.Text.Encoding]::ASCII.GetString($bytes)
    $unicode = [System.Text.Encoding]::Unicode.GetString($bytes)
    foreach ($privatePath in @($HOME, $repository)) {
        if ($ascii.Contains($privatePath) -or $unicode.Contains($privatePath)) {
            throw "$name contains a private build path"
        }
    }
    Copy-Item -LiteralPath $source -Destination $stage
}
Copy-Item -LiteralPath (Join-Path $bridge 'README.md') -Destination $stage
Copy-Item -LiteralPath (Join-Path $bridge 'joycon-rumble.example.toml') -Destination $stage

if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive
$checksum = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath "$archive.sha256" -Encoding ASCII -Value "$checksum  $(Split-Path -Leaf $archive)"
Write-Output "Created $archive"
