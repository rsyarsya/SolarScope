<#
.SYNOPSIS
  Builds dist\SolarScope-Setup.exe: self-contained app + bundled settings + Inno Setup installer.

.DESCRIPTION
  Settings are taken from the current user's environment variables:
    SOLARSCOPE_DB_CONNECTION      Npgsql connection string (may include Root Certificate=<path>)
    SOLARSCOPE_GEOAPIFY_API_KEY   Geoapify API key
  The root certificate is copied next to the app and referenced by a relative path.
  Everything is written under dist\ (git-ignored). Secrets never enter the repository.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File installer\build.ps1
#>
param([string]$Version = "1.0.0")
$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $root "dist"
$app = Join-Path $dist "app"

function Get-Setting([string]$name) {
    $value = [Environment]::GetEnvironmentVariable($name, "Process")
    if ([string]::IsNullOrWhiteSpace($value)) { $value = [Environment]::GetEnvironmentVariable($name, "User") }
    if ([string]::IsNullOrWhiteSpace($value)) { throw "Environment variable $name belum diisi." }
    return $value.Trim()
}

$connection = Get-Setting "SOLARSCOPE_DB_CONNECTION"
$geoapifyKey = Get-Setting "SOLARSCOPE_GEOAPIFY_API_KEY"

$iscc = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 tidak ditemukan. Pasang dengan: winget install JRSoftware.InnoSetup" }

if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }

Write-Host "Publishing self-contained app..."
dotnet publish (Join-Path $root "src\SolarScope.Desktop\SolarScope.Desktop.csproj") `
    -c Release -r win-x64 --self-contained true -p:Version=$Version -o $app
if ($LASTEXITCODE -ne 0) { throw "dotnet publish gagal." }

# Bundle the root certificate and point the connection string at it relatively.
$certPattern = '(?i)Root\s*Certificate\s*=\s*("([^"]*)"|([^;]*))'
$match = [regex]::Match($connection, $certPattern)
if ($match.Success) {
    $certPath = if ($match.Groups[2].Success) { $match.Groups[2].Value } else { $match.Groups[3].Value.Trim() }
    if (-not (Test-Path $certPath)) { throw "Sertifikat root tidak ditemukan." }
    $certDir = New-Item -ItemType Directory -Force (Join-Path $app "certs")
    Copy-Item $certPath (Join-Path $certDir "supabase-root.crt")
    $connection = [regex]::Replace($connection, $certPattern, 'Root Certificate=certs\supabase-root.crt')
}

$settings = [ordered]@{ DatabaseConnection = $connection; GeoapifyApiKey = $geoapifyKey } | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $app "solarscope.settings.json"), $settings, (New-Object Text.UTF8Encoding $false))

Write-Host "Compiling installer..."
& $iscc "/DAppVersion=$Version" (Join-Path $PSScriptRoot "SolarScope.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup gagal." }

Write-Host "Selesai: $(Join-Path $dist 'SolarScope-Setup.exe')"
