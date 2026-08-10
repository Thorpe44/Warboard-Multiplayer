$ErrorActionPreference = 'Stop'

function Find-WarboardRoot {
    param([string[]]$Starts)

    foreach ($start in $Starts) {
        if ([string]::IsNullOrWhiteSpace($start)) { continue }

        try {
            $candidate = (Resolve-Path -LiteralPath $start).Path
        }
        catch {
            continue
        }

        while ($candidate) {
            $assets = Join-Path $candidate 'Assets'
            $projectSettings = Join-Path $candidate 'ProjectSettings'
            $objective = Join-Path $candidate 'Assets\Scripts\Core\ObjectiveController.cs'

            if ((Test-Path -LiteralPath $assets) -and
                (Test-Path -LiteralPath $projectSettings) -and
                (Test-Path -LiteralPath $objective)) {
                return $candidate
            }

            $parent = Split-Path -Parent $candidate
            if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) { break }
            $candidate = $parent
        }
    }

    throw 'Could not locate the Warboard Unity project root. Extract this ZIP into the project folder and run the BAT again.'
}

function Replace-ExactlyOnce {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Replacement,
        [string]$Label
    )

    $regex = New-Object System.Text.RegularExpressions.Regex(
        $Pattern,
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    $matches = $regex.Matches($Text)

    if ($matches.Count -ne 1) {
        throw "Patch validation failed for $Label (expected exactly 1 anchor, found $($matches.Count)). Nothing has been written."
    }

    return $regex.Replace($Text, $Replacement, 1)
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Find-WarboardRoot @($PWD.Path, $scriptDir)
$core = Join-Path $root 'Assets\Scripts\Core'
$payload = Join-Path $scriptDir 'V50_PATCH_PAYLOAD'

$objectivePath = Join-Path $core 'ObjectiveController.cs'
$missionsPath = Join-Path $core 'GameController.Missions.cs'
$corePath = Join-Path $core 'GameController.Core.cs'
$buildInfoPath = Join-Path $core 'WarboardBuildInfo.cs'

$required = @(
    $objectivePath,
    $missionsPath,
    $corePath,
    $buildInfoPath
)

foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required current-main file is missing: $path"
    }
}

if (-not (Test-Path -LiteralPath $payload)) {
    throw 'V50_PATCH_PAYLOAD is missing next to the installer.'
}

$payloadFiles = @(
    'TerrainAreaFootprint50.cs',
    'TerrainFeature.V49TerrainObjectives.cs',
    'ObjectiveController.V49TerrainObjectives.cs',
    'SquadController.V49TerrainObjectives.cs',
    'ObjectiveTerrainLink49.cs',
    'GameController.V49TerrainObjectives.cs',
    'GameController.V50TerrainAreaBattlefield.cs'
)

foreach ($name in $payloadFiles) {
    $source = Join-Path $payload $name
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Missing V50 payload file: $name"
    }
}

Write-Host "Warboard root: $root"
Write-Host 'Validating V48/V49 -> V50 terrain-area battlefield repair...'

$objectiveText = [IO.File]::ReadAllText($objectivePath)
$missionsText = [IO.File]::ReadAllText($missionsPath)
$coreText = [IO.File]::ReadAllText($corePath)
$buildText = [IO.File]::ReadAllText($buildInfoPath)

# Ensure the V49 terrain-aware OC hook exists. V50 is self-contained even if
# the user's V49 install partially failed or they are still on V48.
if (-not $objectiveText.Contains('WARBOARD_V49_TERRAIN_OBJECTIVE_OC')) {
    $ocReplacement = @'
int oc =
                terrainObjectiveArea != null
                ? squad.TotalObjectiveControlWithinTerrain(
                    terrainObjectiveArea
                )
                : squad.TotalObjectiveControlWithin(
                    transform.position,
                    ControlRadius
                ); // WARBOARD_V49_TERRAIN_OBJECTIVE_OC
'@

    $objectiveText = Replace-ExactlyOnce `
        -Text $objectiveText `
        -Pattern 'int\s+oc\s*=\s*squad\.TotalObjectiveControlWithin\(\s*transform\.position\s*,\s*ControlRadius\s*\)\s*;' `
        -Replacement $ocReplacement `
        -Label 'ObjectiveController objective-control geometry'
}

if (-not $objectiveText.Contains('WARBOARD_V49_TERRAIN_OBJECTIVE_RANGE')) {
    $rangeReplacement = @'
if (terrainObjectiveArea != null)
        {
            return squad
                .JoinedLivingModelTokens()
                .Any(
                    model =>
                        terrainObjectiveArea
                            .ModelTouchesObjectiveArea(
                                model
                            )
                );
        } // WARBOARD_V49_TERRAIN_OBJECTIVE_RANGE

        return squad
            .JoinedLivingModelTokens()
'@

    $objectiveText = Replace-ExactlyOnce `
        -Text $objectiveText `
        -Pattern 'return\s+squad\s*\.JoinedLivingModelTokens\(\)' `
        -Replacement $rangeReplacement `
        -Label 'ObjectiveController objective-range geometry'
}

# Replace V49's nearest-scatter binding with the V50 standard terrain-area
# rebuild. If the project is V48, insert the V50 call directly.
if (-not $missionsText.Contains('WARBOARD_V50_STANDARD_TERRAIN_AREAS')) {
    if ($missionsText -match 'BindObjectivesToTerrainAreas11\(\)\s*;') {
        $missionsText = [regex]::Replace(
            $missionsText,
            'BindObjectivesToTerrainAreas11\(\)\s*;\s*(?://\s*WARBOARD_V49_BIND_TERRAIN_OBJECTIVES)?',
            'BuildAndBindStandardTerrainAreas50(); // WARBOARD_V50_STANDARD_TERRAIN_AREAS',
            1
        )
    }
    else {
        $missionReplacement = @'
BuildAndBindStandardTerrainAreas50(); // WARBOARD_V50_STANDARD_TERRAIN_AREAS

        CreateDeploymentZoneOutlines();
'@

        $missionsText = Replace-ExactlyOnce `
            -Text $missionsText `
            -Pattern 'CreateDeploymentZoneOutlines\(\)\s*;' `
            -Replacement $missionReplacement `
            -Label 'mission battlefield V50 terrain-area build hook'
    }
}

$missionsText = $missionsText.Replace(
    '"click a legal objective marker"',
    '"click a legal objective terrain area"'
)

# Ensure clicking a scenery feature sitting on an objective terrain area can
# resolve back to its ObjectiveController.
if (-not $coreText.Contains('WARBOARD_V49_OBJECTIVE_TERRAIN_CLICK_LINK')) {
    $clickPattern = 'TerrainFeature\s+clickedTerrain\s*=\s*hit\.collider\s*\.GetComponentInParent<\s*TerrainFeature\s*>\s*\(\s*\)\s*;'
    $clickRegex = New-Object System.Text.RegularExpressions.Regex(
        $clickPattern,
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )
    $clickMatches = $clickRegex.Matches($coreText)

    if ($clickMatches.Count -ne 1) {
        throw "Patch validation failed for mission-action terrain click link (expected exactly 1 anchor, found $($clickMatches.Count)). Nothing has been written."
    }

    $match = $clickMatches[0]
    $clickInsert = @'


        // WARBOARD_V49_OBJECTIVE_TERRAIN_CLICK_LINK
        if (clickedObjective == null &&
            clickedTerrain != null)
        {
            ObjectiveTerrainLink49 objectiveLink =
                clickedTerrain.GetComponent<
                    ObjectiveTerrainLink49
                >();

            if (objectiveLink != null)
                clickedObjective = objectiveLink.Objective;
        }
'@

    $coreText =
        $coreText.Substring(0, $match.Index) +
        $match.Value +
        $clickInsert +
        $coreText.Substring($match.Index + $match.Length)
}

# Build identity. Accept V48 because this installer can repair a skipped or
# failed V49 install; accept V49 as the normal route; allow safe V50 reruns.
if ($buildText -match 'public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*"v50"\s*;') {
    # already V50
}
elseif ($buildText -match 'public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*"v49"\s*;') {
    $buildText = [regex]::Replace(
        $buildText,
        '(public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*)"v49"(\s*;)',
        '${1}"v50"${2}',
        1
    )
}
elseif ($buildText -match 'public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*"v48"\s*;') {
    $buildText = [regex]::Replace(
        $buildText,
        '(public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*)"v48"(\s*;)',
        '${1}"v50"${2}',
        1
    )
}
else {
    throw 'WarboardBuildInfo.cs is not v48/v49/v50. This installer refuses to guess against an unknown main.'
}

# All anchors and payloads validated. Back up before touching the project.
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupDir = Join-Path $root ("Library\WarboardBackups\V50\" + $stamp)
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

$backupMap = @{}
foreach ($path in $required) {
    $dest = Join-Path $backupDir ([IO.Path]::GetFileName($path))
    Copy-Item -LiteralPath $path -Destination $dest -Force
    $backupMap[$path] = $dest
}

$payloadDestBackups = @{}
foreach ($name in $payloadFiles) {
    $dest = Join-Path $core $name
    if (Test-Path -LiteralPath $dest) {
        $backup = Join-Path $backupDir $name
        Copy-Item -LiteralPath $dest -Destination $backup -Force
        $payloadDestBackups[$dest] = $backup
    }
}

$utf8NoBom = New-Object System.Text.UTF8Encoding -ArgumentList $false

try {
    [IO.File]::WriteAllText($objectivePath, $objectiveText, $utf8NoBom)
    [IO.File]::WriteAllText($missionsPath, $missionsText, $utf8NoBom)
    [IO.File]::WriteAllText($corePath, $coreText, $utf8NoBom)
    [IO.File]::WriteAllText($buildInfoPath, $buildText, $utf8NoBom)

    foreach ($name in $payloadFiles) {
        Copy-Item -LiteralPath (Join-Path $payload $name) -Destination (Join-Path $core $name) -Force
    }

    $markerDir = Join-Path $root 'Library\WarboardInstallerMarkers'
    New-Item -ItemType Directory -Force -Path $markerDir | Out-Null

    @(
        'WARBOARD V50 - 11E TERRAIN AREA BATTLEFIELD',
        ('Installed: ' + (Get-Date).ToString('s')),
        ('Backup: ' + $backupDir),
        'Creates the 16 standard 11e terrain-area footprint set and binds objectives directly to designated terrain areas.'
    ) | Set-Content -LiteralPath (Join-Path $markerDir 'V50_TERRAIN_AREAS.txt') -Encoding UTF8
}
catch {
    Write-Host 'Install failed while writing. Restoring backed-up files...' -ForegroundColor Yellow

    foreach ($path in $required) {
        if ($backupMap.ContainsKey($path)) {
            Copy-Item -LiteralPath $backupMap[$path] -Destination $path -Force
        }
    }

    foreach ($name in $payloadFiles) {
        $dest = Join-Path $core $name

        if ($payloadDestBackups.ContainsKey($dest)) {
            Copy-Item -LiteralPath $payloadDestBackups[$dest] -Destination $dest -Force
        }
        elseif (Test-Path -LiteralPath $dest) {
            Remove-Item -LiteralPath $dest -Force
        }
    }

    throw
}

Write-Host ''
Write-Host 'V50 terrain-area battlefield repair installed successfully.' -ForegroundColor Green
Write-Host ('Backup: ' + $backupDir)
Write-Host 'Return to Unity, let it compile, then start a FRESH battle so the battlefield regenerates.'
