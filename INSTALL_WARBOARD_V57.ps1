$ErrorActionPreference = "Stop"

# WARBOARD_V57_TERRAIN_GHOST_INSTALLER
$Root = (Resolve-Path $PSScriptRoot).Path
$Stamp = Get-Date -Format "yyyyMMdd_HHmmss"

$StageRoot =
    Join-Path $Root (
        "Library\WarboardV57Staging_" +
        $Stamp
    )

$BackupRoot =
    Join-Path $Root (
        "Library\WarboardBackups\V57_" +
        $Stamp
    )

$PayloadRoot =
    Join-Path $Root "V57_PAYLOAD"

$changed =
    New-Object System.Collections.Generic.List[string]

$commitStarted = $false

function Normalize-Newlines(
    [string]$Text)
{
    if ($null -eq $Text)
    {
        return ""
    }

    return $Text.Replace(
        "`r`n",
        "`n"
    ).Replace(
        "`r",
        "`n"
    )
}

function Read-Text(
    [string]$Path)
{
    if (-not (Test-Path $Path))
    {
        throw (
            "Expected file is missing: " +
            $Path
        )
    }

    return Normalize-Newlines(
        [System.IO.File]::ReadAllText(
            $Path
        )
    )
}

function Write-Utf8(
    [string]$Path,
    [string]$Text)
{
    $parent =
        Split-Path -Parent $Path

    if (-not (Test-Path $parent))
    {
        New-Item `
            -ItemType Directory `
            -Path $parent `
            -Force |
            Out-Null
    }

    $encoding =
        New-Object System.Text.UTF8Encoding(
            $false
        )

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Text),
        $encoding
    )
}

function Backup-File(
    [string]$Relative)
{
    $source =
        Join-Path $Root $Relative

    if (-not (Test-Path $source))
    {
        throw (
            "Expected project file is missing: " +
            $Relative
        )
    }

    $backup =
        Join-Path $BackupRoot $Relative

    $parent =
        Split-Path -Parent $backup

    if (-not (Test-Path $parent))
    {
        New-Item `
            -ItemType Directory `
            -Path $parent `
            -Force |
            Out-Null
    }

    Copy-Item `
        -LiteralPath $source `
        -Destination $backup `
        -Force

    if (-not $changed.Contains(
            $Relative))
    {
        $changed.Add(
            $Relative
        ) | Out-Null
    }
}

function Stage-Replacement(
    [string]$Relative,
    [string]$RequiredOldMarker,
    [string]$RequiredNewMarker)
{
    $target =
        Join-Path $Root $Relative

    $payload =
        Join-Path $PayloadRoot $Relative

    if (-not (Test-Path $target))
    {
        throw (
            "Expected current v54/v55 source missing: " +
            $Relative
        )
    }

    if (-not (Test-Path $payload))
    {
        throw (
            "Missing v57 payload: " +
            $Relative
        )
    }

    $current =
        Read-Text $target

    if (-not $current.Contains(
            $RequiredOldMarker))
    {
        throw (
            "Unexpected local source for " +
            $Relative +
            ". Required marker missing: " +
            $RequiredOldMarker
        )
    }

    Backup-File $Relative

    $replacement =
        Read-Text $payload

    if (-not $replacement.Contains(
            $RequiredNewMarker))
    {
        throw (
            "v57 payload validation failed for " +
            $Relative
        )
    }

    $stage =
        Join-Path $StageRoot $Relative

    Write-Utf8 `
        $stage `
        $replacement

    Write-Host (
        "[FIXED] " +
        $Relative
    ) -ForegroundColor Green
}

function Stage-BuildVersion()
{
    $relative =
        "Assets\Scripts\Core\WarboardBuildInfo.cs"

    Backup-File $relative

    $source =
        Read-Text (
            Join-Path $Root $relative
        )

    $regex =
        New-Object System.Text.RegularExpressions.Regex(
            'public const string CurrentVersion\s*=\s*"v[^"]+";'
        )

    $matches =
        $regex.Matches(
            $source
        )

    if ($matches.Count -ne 1)
    {
        throw (
            "Could not uniquely locate Warboard build version; found " +
            $matches.Count
        )
    }

    $patched =
        $regex.Replace(
            $source,
            'public const string CurrentVersion = "v57";',
            1
        )

    $stage =
        Join-Path $StageRoot $relative

    Write-Utf8 `
        $stage `
        $patched

    Write-Host (
        "[FIXED] build identity v57"
    ) -ForegroundColor Green
}

function Require-StagedMarker(
    [string]$Relative,
    [string]$Marker)
{
    $path =
        Join-Path $StageRoot $Relative

    $text =
        Read-Text $path

    if (-not $text.Contains(
            $Marker))
    {
        throw (
            "Staged validation marker missing in " +
            $Relative +
            ": " +
            $Marker
        )
    }
}

function Restore-Project()
{
    foreach ($relative
        in $changed)
    {
        $backup =
            Join-Path $BackupRoot $relative

        $target =
            Join-Path $Root $relative

        if (Test-Path $backup)
        {
            Copy-Item `
                -LiteralPath $backup `
                -Destination $target `
                -Force
        }
    }
}

try
{
    Write-Host (
        "WARBOARD v57 TERRAIN + DEPLOYMENT GHOSTS"
    ) -ForegroundColor Cyan

    Write-Host (
        "Target: Thorpe44/Warboard-Multiplayer"
    ) -ForegroundColor Cyan

    Write-Host ""

    $required = @(
        "Assets\Scripts\Core\GameController.V54PlacementGhost.cs",
        "Assets\Scripts\Core\GameController.V55CleanTerrain.cs",
        "Assets\Scripts\Core\WarboardBuildInfo.cs"
    )

    foreach ($relative
        in $required)
    {
        if (-not (
            Test-Path (
                Join-Path $Root $relative
            )))
        {
            throw (
                "Install v54/v55 first or extract this into the correct Warboard-Multiplayer root. Missing: " +
                $relative
            )
        }
    }

    New-Item `
        -ItemType Directory `
        -Path $StageRoot `
        -Force |
        Out-Null

    New-Item `
        -ItemType Directory `
        -Path $BackupRoot `
        -Force |
        Out-Null

    Stage-Replacement `
        "Assets\Scripts\Core\GameController.V54PlacementGhost.cs" `
        "WARBOARD_V54_PLACEMENT_GHOST_SYSTEM" `
        "WARBOARD_V57_DEPLOYMENT_GHOSTS"

    Stage-Replacement `
        "Assets\Scripts\Core\GameController.V55CleanTerrain.cs" `
        "WARBOARD_V55_CLEAN_TERRAIN_KIT" `
        "WARBOARD_V57_RUIN_TERRAIN_KIT"

    Stage-BuildVersion

    Require-StagedMarker `
        "Assets\Scripts\Core\GameController.V54PlacementGhost.cs" `
        "WARBOARD_V57_DEPLOYMENT_GHOSTS"

    Require-StagedMarker `
        "Assets\Scripts\Core\GameController.V54PlacementGhost.cs" `
        "WARBOARD_V57_FORCE_GHOST_RENDERERS"

    Require-StagedMarker `
        "Assets\Scripts\Core\GameController.V55CleanTerrain.cs" `
        "WARBOARD_V57_RUIN_TERRAIN_KIT"

    Require-StagedMarker `
        "Assets\Scripts\Core\WarboardBuildInfo.cs" `
        'CurrentVersion = "v57";'

    Write-Host ""
    Write-Host (
        "All v57 files validated in staging. Committing..."
    ) -ForegroundColor Cyan

    $commitStarted = $true

    foreach ($relative
        in $changed)
    {
        $stage =
            Join-Path $StageRoot $relative

        if (-not (Test-Path $stage))
        {
            throw (
                "Staged file missing: " +
                $relative
            )
        }

        $target =
            Join-Path $Root $relative

        Copy-Item `
            -LiteralPath $stage `
            -Destination $target `
            -Force
    }

    $commitStarted = $false

    Remove-Item `
        -LiteralPath $StageRoot `
        -Recurse `
        -Force

    Write-Host ""
    Write-Host (
        "WARBOARD v57 INSTALL COMPLETE"
    ) -ForegroundColor Green

    Write-Host (
        "Backup: " +
        $BackupRoot
    ) -ForegroundColor DarkGray

    exit 0
}
catch
{
    Write-Host ""
    Write-Host (
        "WARBOARD v57 INSTALL FAILED"
    ) -ForegroundColor Red

    Write-Host (
        "---------------------------"
    ) -ForegroundColor Red

    Write-Host (
        $_.Exception.Message
    ) -ForegroundColor Red

    Write-Host ""

    if ($commitStarted)
    {
        Write-Host (
            "Rolling project files back from v57 backup..."
        ) -ForegroundColor Yellow

        try
        {
            Restore-Project

            Write-Host (
                "Rollback complete."
            ) -ForegroundColor Yellow
        }
        catch
        {
            Write-Host (
                "ROLLBACK ERROR: " +
                $_.Exception.Message
            ) -ForegroundColor Red
        }
    }
    else
    {
        Write-Host (
            "Project source was not committed; failure occurred during staging/validation."
        ) -ForegroundColor Yellow
    }

    if (Test-Path $StageRoot)
    {
        Remove-Item `
            -LiteralPath $StageRoot `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue
    }

    exit 1
}
