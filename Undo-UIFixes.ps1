param(
    [Parameter(Mandatory=$true)]
    [string]$RepoRoot
)

$ErrorActionPreference = "Stop"
if ($null -eq $RepoRoot) {
    throw "No repository path was supplied."
}
$RepoRoot = $RepoRoot.Trim()
$resolvedRepo =
    Resolve-Path -LiteralPath $RepoRoot -ErrorAction Stop
$RepoRoot = $resolvedRepo.Path
$metadataPath = Join-Path $RepoRoot "_warboard_ui_fix_backup_latest.json"

if (-not (Test-Path -LiteralPath $metadataPath)) {
    throw "No _warboard_ui_fix_backup_latest.json found in $RepoRoot"
}

$meta = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
$backupRoot = [string]$meta.BackupRoot

if (-not (Test-Path -LiteralPath $backupRoot)) {
    throw "Backup folder is missing: $backupRoot"
}

foreach ($relative in $meta.BackedUpFiles) {
    $src = Join-Path $backupRoot ($relative -replace "/", "\")
    $dst = Join-Path $RepoRoot ($relative -replace "/", "\")
    if (-not (Test-Path -LiteralPath $src)) {
        throw "Backup file missing: $src"
    }
    $dstDir = Split-Path -Parent $dst
    New-Item -ItemType Directory -Path $dstDir -Force | Out-Null
    Copy-Item -LiteralPath $src -Destination $dst -Force
    Write-Host "[restored] $relative"
}

foreach ($relative in $meta.CreatedFiles) {
    $dst = Join-Path $RepoRoot ($relative -replace "/", "\")
    if (Test-Path -LiteralPath $dst) {
        Remove-Item -LiteralPath $dst -Force
        Write-Host "[removed] $relative"
    }

    $metaFile = $dst + ".meta"
    if (Test-Path -LiteralPath $metaFile) {
        Remove-Item -LiteralPath $metaFile -Force
        Write-Host "[removed] $relative.meta"
    }
}

Remove-Item -LiteralPath $metadataPath -Force

Write-Host ""
Write-Host "Restored from:"
Write-Host "  $backupRoot"
Write-Host ""
Write-Host "The timestamped backup folder itself was kept."
exit 0
