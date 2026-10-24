param([string]$Version = '1.0.0')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$output = Join-Path $taskRoot "artifacts\Liss-TextLab-$Version-win-x64"
if (Test-Path -LiteralPath $output) { throw "Output already exists. Preserve existing release or select a new version: $output" }
dotnet publish (Join-Path $taskRoot 'src\TextLab.App\TextLab.App.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:Version=$Version -o $output --nologo
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
Copy-Item -LiteralPath (Join-Path $taskRoot 'README.md') -Destination $output
Copy-Item -LiteralPath (Join-Path $taskRoot 'THIRD-PARTY-NOTICES.md') -Destination $output
Copy-Item -LiteralPath (Join-Path $taskRoot 'licenses') -Destination $output -Recurse
$zip = "$output.zip"
Compress-Archive -Path (Join-Path $output '*') -DestinationPath $zip
[pscustomobject]@{ version = $Version; exe = (Join-Path $output 'Liss.TextLab.exe'); zip = $zip; bytes = (Get-Item -LiteralPath $zip).Length; sha256 = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant() } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'artifacts\release-manifest.json') -Encoding utf8
