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

    throw 'Could not locate the Warboard Unity project root. Put this installer inside the project folder (or a subfolder) and run it again.'
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

$objectivePath = Join-Path $core 'ObjectiveController.cs'
$missionsPath = Join-Path $core 'GameController.Missions.cs'
$corePath = Join-Path $core 'GameController.Core.cs'
$buildInfoPath = Join-Path $core 'WarboardBuildInfo.cs'
$payload = Join-Path $scriptDir 'V49_PATCH_PAYLOAD'

$required = @($objectivePath, $missionsPath, $corePath, $buildInfoPath)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required current-main file is missing: $path"
    }
}

if (-not (Test-Path -LiteralPath $payload)) {
    throw 'V49_PATCH_PAYLOAD is missing next to the installer.'
}

Write-Host "Warboard root: $root"
Write-Host 'Validating V48 -> V49 terrain-objective patch...'

$objectiveText = [IO.File]::ReadAllText($objectivePath)
$missionsText = [IO.File]::ReadAllText($missionsPath)
$coreText = [IO.File]::ReadAllText($corePath)
$buildText = [IO.File]::ReadAllText($buildInfoPath)

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

if (-not $missionsText.Contains('WARBOARD_V49_BIND_TERRAIN_OBJECTIVES')) {
    $missionReplacement = @'
BindObjectivesToTerrainAreas11(); // WARBOARD_V49_BIND_TERRAIN_OBJECTIVES

        CreateDeploymentZoneOutlines();
'@

    $missionsText = Replace-ExactlyOnce `
        -Text $missionsText `
        -Pattern 'CreateDeploymentZoneOutlines\(\)\s*;' `
        -Replacement $missionReplacement `
        -Label 'mission battlefield terrain-objective binding'
}

$missionsText = $missionsText.Replace(
    '"click a legal objective marker"',
    '"click a legal objective terrain area"'
)

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

if ($buildText -match 'public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*"v49"\s*;') {
    # Already V49; safe re-run.
}
elseif ($buildText -match 'public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*"v48"\s*;') {
    $buildText = [regex]::Replace(
        $buildText,
        '(public\s+const\s+string\s+(?:CurrentVersion|Version)\s*=\s*)"v48"(\s*;)',
        '${1}"v49"${2}',
        1
    )
}
else {
    throw 'WarboardBuildInfo.cs is not v48/v49. This installer targets the current V48 main and refuses to guess.'
}

$payloadFiles = @(
    'TerrainFeature.V49TerrainObjectives.cs',
    'SquadController.V49TerrainObjectives.cs',
    'ObjectiveTerrainLink49.cs',
    'ObjectiveController.V49TerrainObjectives.cs',
    'GameController.V49TerrainObjectives.cs'
)

foreach ($name in $payloadFiles) {
    $source = Join-Path $payload $name
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Missing V49 payload file: $name"
    }
}

# All anchors have now validated. Only now do we touch the project.
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupDir = Join-Path $root ("Library\WarboardBackups\V49\" + $stamp)
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
        'WARBOARD V49 - 11E TERRAIN OBJECTIVES',
        ('Installed: ' + (Get-Date).ToString('s')),
        ('Backup: ' + $backupDir),
        'Normal mission objectives are now bound to terrain areas rather than circular markers.'
    ) | Set-Content -LiteralPath (Join-Path $markerDir 'V49_TERRAIN_OBJECTIVES.txt') -Encoding UTF8
}
catch {
    Write-Host 'Install failed while writing. Restoring the backed-up V48 files...' -ForegroundColor Yellow

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
Write-Host 'SUCCESS: Warboard V49 terrain objectives installed.' -ForegroundColor Green
Write-Host "Backup: $backupDir"
Write-Host 'Open Unity and let it compile. Normal mission objectives now use terrain-area footprints; the old circular objective visuals are hidden.'
