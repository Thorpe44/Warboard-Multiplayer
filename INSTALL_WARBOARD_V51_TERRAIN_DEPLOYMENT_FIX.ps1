$ErrorActionPreference = 'Stop'

function Find-WarboardRoot {
    param([string[]]$Starts)

    foreach ($start in $Starts) {
        if ([string]::IsNullOrWhiteSpace($start)) { continue }

        try { $candidate = (Resolve-Path -LiteralPath $start).Path }
        catch { continue }

        while ($candidate) {
            if ((Test-Path -LiteralPath (Join-Path $candidate 'Assets')) -and
                (Test-Path -LiteralPath (Join-Path $candidate 'ProjectSettings')) -and
                (Test-Path -LiteralPath (Join-Path $candidate 'Assets\Scripts\Core\WarboardBuildInfo.cs'))) {
                return $candidate
            }

            $parent = Split-Path -Parent $candidate
            if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) { break }
            $candidate = $parent
        }
    }

    throw 'Could not locate the Warboard Unity project root. Extract this ZIP into the project folder and run the BAT again.'
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Find-WarboardRoot @($PWD.Path, $scriptDir)
$core = Join-Path $root 'Assets\Scripts\Core'
$payload = Join-Path $scriptDir 'V51_PATCH_PAYLOAD'

$footprintPath = Join-Path $core 'TerrainAreaFootprint50.cs'
$buildInfoPath = Join-Path $core 'WarboardBuildInfo.cs'
$payloadFootprint = Join-Path $payload 'TerrainAreaFootprint50.cs'

foreach ($path in @($footprintPath, $buildInfoPath, $payloadFootprint)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required V50/V51 file is missing: $path"
    }
}

Write-Host "Warboard root: $root"
Write-Host 'Validating V50 -> V51 terrain-area deployment fix...'

$currentFootprint = [IO.File]::ReadAllText($footprintPath)
$replacementFootprint = [IO.File]::ReadAllText($payloadFootprint)
$buildText = [IO.File]::ReadAllText($buildInfoPath)

if (-not $currentFootprint.Contains('public sealed class TerrainAreaFootprint50')) {
    throw 'TerrainAreaFootprint50.cs is not the expected V50 terrain-area implementation.'
}

if (-not $replacementFootprint.Contains('V51 DEPLOYMENT FIX')) {
    throw 'V51 replacement payload failed self-validation.'
}

# Accept a clean V50 install or a safe V51 rerun.
if ($buildText -match 'public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*"v51"\s*;') {
    # already v51; keep identity
}
elseif ($buildText -match 'public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*"v50"\s*;') {
    $buildText = [regex]::Replace(
        $buildText,
        '(public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*)"v50"(\s*;)',
        '${1}"v51"${2}',
        1
    )
}
else {
    throw 'WarboardBuildInfo.cs is not v50/v51. This fix is specifically for the V50 terrain-area build.'
}

$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupDir = Join-Path $root ("Library\WarboardBackups\V51\" + $stamp)
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

Copy-Item -LiteralPath $footprintPath -Destination (Join-Path $backupDir 'TerrainAreaFootprint50.cs') -Force
Copy-Item -LiteralPath $buildInfoPath -Destination (Join-Path $backupDir 'WarboardBuildInfo.cs') -Force

$utf8NoBom = New-Object System.Text.UTF8Encoding -ArgumentList $false

try {
    [IO.File]::WriteAllText($footprintPath, $replacementFootprint, $utf8NoBom)
    [IO.File]::WriteAllText($buildInfoPath, $buildText, $utf8NoBom)

    $markerDir = Join-Path $root 'Library\WarboardInstallerMarkers'
    New-Item -ItemType Directory -Force -Path $markerDir | Out-Null

    @(
        'WARBOARD V51 - TERRAIN AREA DEPLOYMENT FIX',
        ('Installed: ' + (Get-Date).ToString('s')),
        ('Backup: ' + $backupDir),
        'Terrain Area click colliders now live on a non-TerrainFeature child, so the footprint itself does not block deployment/movement. Decorative scenery remains physical.'
    ) | Set-Content -LiteralPath (Join-Path $markerDir 'V51_TERRAIN_DEPLOYMENT_FIX.txt') -Encoding UTF8
}
catch {
    Write-Host 'Install failed while writing. Restoring backup...' -ForegroundColor Yellow
    Copy-Item -LiteralPath (Join-Path $backupDir 'TerrainAreaFootprint50.cs') -Destination $footprintPath -Force
    Copy-Item -LiteralPath (Join-Path $backupDir 'WarboardBuildInfo.cs') -Destination $buildInfoPath -Force
    throw
}

Write-Host ''
Write-Host 'V51 terrain-area deployment fix installed successfully.' -ForegroundColor Green
Write-Host ('Backup: ' + $backupDir)
Write-Host 'Return to Unity, let it compile, then start a FRESH battle so the V51 terrain-area colliders are regenerated.'
