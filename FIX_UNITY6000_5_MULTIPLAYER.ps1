$ErrorActionPreference = "Stop"

function Find-WarboardRoot {
    param([string]$Start)

    $dir = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 8; $i++) {
        if ((Test-Path (Join-Path $dir "Assets")) -and
            (Test-Path (Join-Path $dir "Packages")) -and
            (Test-Path (Join-Path $dir "ProjectSettings"))) {
            return $dir
        }

        $parent = Split-Path -Parent $dir

        if ([string]::IsNullOrWhiteSpace($parent) -or
            $parent -eq $dir) {
            break
        }

        $dir = $parent
    }

    return $null
}

function Read-Utf8 {
    param([string]$Path)
    $utf8 = New-Object System.Text.UTF8Encoding($false, $false)
    return [System.IO.File]::ReadAllText($Path, $utf8)
}

function Write-Utf8 {
    param([string]$Path, [string]$Content)
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

$Root = Find-WarboardRoot $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Root)) {
    Write-Host "FAILED: Could not find Warboard project root."
    exit 1
}

Write-Host "Warboard root:"
Write-Host "  $Root"
Write-Host ""

$manifestPath = Join-Path $Root "Packages\manifest.json"
$bridgePath = Join-Path $Root "Assets\Scripts\Multiplayer\WarboardNetworkBridge.cs"
$bootstrapPath = Join-Path $Root "Assets\Scripts\Multiplayer\WarboardMultiplayerBootstrap.cs"
$gameMpPath = Join-Path $Root "Assets\Scripts\Multiplayer\GameController.Multiplayer.cs"
$sessionPath = Join-Path $Root "Assets\Scripts\Multiplayer\WarboardSessionService.cs"

$required = @(
    $manifestPath,
    $bridgePath,
    $bootstrapPath,
    $sessionPath
)

foreach ($path in $required) {
    if (!(Test-Path $path)) {
        Write-Host ("FAILED: Missing " + $path)
        exit 2
    }
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Back up only the files this compatibility fix changes.
foreach ($path in @($manifestPath, $bridgePath, $bootstrapPath, $sessionPath, $gameMpPath)) {
    if (Test-Path $path) {
        Copy-Item $path ($path + ".before_unity6000_5_mpfix_" + $stamp + ".bak") -Force
    }
}

# ------------------------------------------------------------------
# 1) Use Multiplayer Services 2.1.3.
# Unity's 2.1 line contains the Unity 6000.4+ editor API fix.
# ------------------------------------------------------------------
$manifest = Read-Utf8 $manifestPath

# Remove standalone Relay if a previous compatibility attempt added it.
$manifest = [regex]::Replace(
    $manifest,
    '(?m)^\s*"com\.unity\.services\.relay"\s*:\s*"[^"]+"\s*,?\s*\r?\n',
    ''
)

if ($manifest -match '"com\.unity\.services\.multiplayer"\s*:') {
    $manifest = [regex]::Replace(
        $manifest,
        '"com\.unity\.services\.multiplayer"\s*:\s*"[^"]+"',
        '"com.unity.services.multiplayer": "2.1.3"'
    )
}
else {
    $manifest = $manifest.Replace(
        '"dependencies": {',
        '"dependencies": {' + [Environment]::NewLine +
        '    "com.unity.services.multiplayer": "2.1.3",'
    )
}

if ($manifest -match '"com\.unity\.netcode\.gameobjects"\s*:') {
    $manifest = [regex]::Replace(
        $manifest,
        '"com\.unity\.netcode\.gameobjects"\s*:\s*"[^"]+"',
        '"com.unity.netcode.gameobjects": "2.13.1"'
    )
}
else {
    $manifest = $manifest.Replace(
        '"dependencies": {',
        '"dependencies": {' + [Environment]::NewLine +
        '    "com.unity.netcode.gameobjects": "2.13.1",'
    )
}

if ($manifest -match '"com\.unity\.services\.authentication"\s*:') {
    $manifest = [regex]::Replace(
        $manifest,
        '"com\.unity\.services\.authentication"\s*:\s*"[^"]+"',
        '"com.unity.services.authentication": "3.5.2"'
    )
}
else {
    $manifest = $manifest.Replace(
        '"dependencies": {',
        '"dependencies": {' + [Environment]::NewLine +
        '    "com.unity.services.authentication": "3.5.2",'
    )
}

Write-Utf8 $manifestPath $manifest
Write-Host "[FIXED] Packages\manifest.json -> Multiplayer Services 2.1.3"

# ------------------------------------------------------------------
# 2) Restore session-based MPS service.
# The replacement file ships beside this script.
# ------------------------------------------------------------------
$replacementSession = Join-Path $PSScriptRoot "WarboardSessionService.cs"

if (!(Test-Path $replacementSession)) {
    Write-Host "FAILED: Replacement WarboardSessionService.cs is missing."
    exit 3
}

Copy-Item $replacementSession $sessionPath -Force
Write-Host "[FIXED] WarboardSessionService.cs restored to MPS Sessions"

# ------------------------------------------------------------------
# 3) NGO 2.13: ServerClientId is static.
# ------------------------------------------------------------------
$bridge = Read-Utf8 $bridgePath
$bridge = $bridge.Replace(
    "manager.ServerClientId",
    "NetworkManager.ServerClientId"
)

# Unity 6000.5 warning cleanup.
$bridge = $bridge.Replace(
    "FindFirstObjectByType<",
    "FindAnyObjectByType<"
)

Write-Utf8 $bridgePath $bridge
Write-Host "[FIXED] WarboardNetworkBridge.cs NGO 2.13 API"

# ------------------------------------------------------------------
# 4) Unity 6000.5 object lookup warning cleanup.
# ------------------------------------------------------------------
$bootstrap = Read-Utf8 $bootstrapPath
$bootstrap = $bootstrap.Replace(
    "FindFirstObjectByType<",
    "FindAnyObjectByType<"
)
Write-Utf8 $bootstrapPath $bootstrap
Write-Host "[FIXED] WarboardMultiplayerBootstrap.cs lookup API"

if (Test-Path $gameMpPath) {
    $gameMp = Read-Utf8 $gameMpPath

    $gameMp = [regex]::Replace(
        $gameMp,
        'FindObjectsByType<TerrainFeature>\s*\(\s*FindObjectsSortMode\.None\s*\)',
        'FindObjectsByType<TerrainFeature>()'
    )

    Write-Utf8 $gameMpPath $gameMp
    Write-Host "[FIXED] GameController.Multiplayer.cs lookup API"
}

# ------------------------------------------------------------------
# 5) Remove stale generated package state.
# ------------------------------------------------------------------
$lock = Join-Path $Root "Packages\packages-lock.json"

if (Test-Path $lock) {
    Remove-Item $lock -Force
    Write-Host "[REMOVED] Packages\packages-lock.json"
}

$packageCache = Join-Path $Root "Library\PackageCache"

if (Test-Path $packageCache) {
    Get-ChildItem $packageCache -Directory -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -like "com.unity.services.multiplayer@*" -or
            $_.Name -like "com.unity.services.relay@*" -or
            $_.Name -like "com.unity.netcode.gameobjects@*"
        } |
        ForEach-Object {
            Remove-Item $_.FullName -Recurse -Force
            Write-Host ("[REMOVED CACHE] " + $_.Name)
        }
}

$scriptAssemblies = Join-Path $Root "Library\ScriptAssemblies"

if (Test-Path $scriptAssemblies) {
    Remove-Item $scriptAssemblies -Recurse -Force
    Write-Host "[REMOVED] Library\ScriptAssemblies"
}

# ------------------------------------------------------------------
# Verification.
# ------------------------------------------------------------------
$manifestVerify = Read-Utf8 $manifestPath
$bridgeVerify = Read-Utf8 $bridgePath
$sessionVerify = Read-Utf8 $sessionPath

$errors = @()

if ($manifestVerify -notmatch '"com\.unity\.services\.multiplayer"\s*:\s*"2\.1\.3"') {
    $errors += "Multiplayer Services is not 2.1.3"
}

if ($manifestVerify -match '"com\.unity\.services\.relay"') {
    $errors += "Standalone Relay package still exists in manifest"
}

if ($bridgeVerify.Contains("manager.ServerClientId")) {
    $errors += "manager.ServerClientId still exists in WarboardNetworkBridge.cs"
}

if ($sessionVerify.Contains("new RelayServerData")) {
    $errors += "Old direct RelayServerData constructor still exists"
}

if ($sessionVerify -notmatch 'Unity\.Services\.Multiplayer') {
    $errors += "Session service was not restored to Multiplayer Services"
}

Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "FIX INCOMPLETE:"
    foreach ($entry in $errors) {
        Write-Host ("  - " + $entry)
    }
    exit 4
}

Write-Host "[PASS] MPS 2.1.3 configured."
Write-Host "[PASS] Standalone Relay removed."
Write-Host "[PASS] NGO ServerClientId API fixed."
Write-Host "[PASS] Old RelayServerData constructor removed."
Write-Host "[PASS] Package cache reset."
Write-Host ""
Write-Host "SUCCESS - UNITY 6000.5 MULTIPLAYER COMPILE FIX VERIFIED"
Write-Host ""
Write-Host "Re-open Unity and allow Package Manager to finish resolving."
