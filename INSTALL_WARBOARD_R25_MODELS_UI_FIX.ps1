$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD R25 - ORK/NID MODELS + AELDARI GHOST-BASE + MISSION UI FIX" -ForegroundColor Cyan
Write-Host "-------------------------------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Fail {
    param([string]$Message)

    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    Write-Host "The installer will not continue." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 12; $i++) {
        if ((Test-Path (Join-Path $candidate "Assets\Scripts\Core\SquadController.cs")) -and
            (Test-Path (Join-Path $candidate "Assets\Scripts\Core\ModelVisualRegistry.cs")) -and
            (Test-Path (Join-Path $candidate "Assets\Scripts\Core\GameController.UI.cs"))) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate

        if ([string]::IsNullOrWhiteSpace($parent) -or
            $parent -eq $candidate) {
            break
        }

        $candidate = $parent
    }

    return $null
}

function Safe-Remove {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return
    }

    try {
        Remove-Item $Path -Recurse -Force -ErrorAction Stop
        Write-Host "Removed obsolete: $(Split-Path -Leaf $Path)" -ForegroundColor DarkGray
    }
    catch {
        Write-Host "Could not remove obsolete file: $Path" -ForegroundColor Yellow
    }
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir

if (-not $ProjectRoot) {
    Fail "Could not locate the Warboard project root."
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Core = Join-Path $ProjectRoot "Assets\Scripts\Core"
$Squad = Join-Path $Core "SquadController.cs"
$Registry = Join-Path $Core "ModelVisualRegistry.cs"
$Ui = Join-Path $Core "GameController.UI.cs"
$Resolver = Join-Path $Core "ExtendedFactionModelPackResolverR25.cs"
$PayloadResolver = Join-Path $ScriptDir "PATCH_PAYLOAD\Assets\Scripts\Core\ExtendedFactionModelPackResolverR25.cs"

foreach ($required in @($Squad, $Registry, $Ui, $PayloadResolver)) {
    if (-not (Test-Path $required)) {
        Fail "Missing required file: $required"
    }
}

$squadText = Get-Content -Raw -Path $Squad
$registryText = Get-Content -Raw -Path $Registry
$uiText = Get-Content -Raw -Path $Ui

if ($registryText -notmatch 'private\s+static\s+ModelVisualDefinition\s+TryResolvePack\s*\(' -or
    $registryText -notmatch 'component\.position') {
    Fail "Aeldari ModelVisualRegistry source was not recognised."
}

if ($uiText -notmatch '"CHAPTER APPROVED 2026 - 27\s+-\s+MISSION SETUP"' -or
    $uiText -notmatch 'previewBattlefield\.DisplayName') {
    Fail "Mission-setup UI source was not recognised."
}

# SquadController can be either:
# - live GitHub: NecronModelPackResolverR22
# - user's later local install: FactionModelPackResolver
# - already R25
if ($squadText -notmatch 'NecronModelPackResolverR22\.TryResolve' -and
    $squadText -notmatch 'FactionModelPackResolver\.TryResolve' -and
    $squadText -notmatch 'ExtendedFactionModelPackResolverR25\.TryResolve') {
    Fail "SquadController model-resolver chain was not recognised."
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$Backup = Join-Path $ProjectRoot "Library\WarboardBackups\R25_MODEL_UI_FIX\$timestamp"
New-Item -ItemType Directory -Force -Path $Backup | Out-Null

$backupFiles = @(
    "Assets\Scripts\Core\SquadController.cs",
    "Assets\Scripts\Core\ModelVisualRegistry.cs",
    "Assets\Scripts\Core\GameController.UI.cs",
    "Assets\Scripts\Core\ExtendedFactionModelPackResolverR25.cs"
)

foreach ($relative in $backupFiles) {
    $source = Join-Path $ProjectRoot $relative

    if (-not (Test-Path $source)) {
        continue
    }

    $dest = Join-Path $Backup $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Copy-Item $source $dest -Force
}

try {
    Copy-Item $PayloadResolver $Resolver -Force

    # -----------------------------------------------------------------
    # 1. SQUADCONTROLLER:
    #    Use the new resolver for Necrons + Orks + Tyranids.
    #    Aeldari/Custodes still fall through to their existing proven code.
    # -----------------------------------------------------------------
    $squadText = $squadText.Replace(
        "NecronModelPackResolverR22.TryResolve(",
        "ExtendedFactionModelPackResolverR25.TryResolve("
    )

    $squadText = $squadText.Replace(
        "FactionModelPackResolver.TryResolve(",
        "ExtendedFactionModelPackResolverR25.TryResolve("
    )

    # Remove the old R2.3 post-spawn origin hack if it is still present.
    $originRepairPattern =
        '(?s)if\s*\(\s*visual\s*!=\s*null\s*\)\s*\{\s*bool\s+visualAttached\s*=\s*token\.AttachVisual\(\s*visual,\s*baseColor\s*\);\s*if\s*\(\s*visualAttached\s*\)\s*\{\s*NecronVisualOriginRepairR23\.Reanchor\(\s*FactionId,\s*token\s*\);\s*\}\s*\}'

    if ([regex]::IsMatch($squadText, $originRepairPattern)) {
        $plainAttach = @'
if (visual != null)
            {
                token.AttachVisual(
                    visual,
                    baseColor
                );
            }
'@

        $squadText =
            [regex]::Replace(
                $squadText,
                $originRepairPattern,
                $plainAttach,
                1
            )
    }

    Set-Content -Path $Squad -Value $squadText -Encoding UTF8

    # -----------------------------------------------------------------
    # 2. AELDARI:
    #    Keep the proven Aeldari resolver, but sanitize TTS wrapper/root
    #    transforms BEFORE ModelToken.AttachVisual.
    # -----------------------------------------------------------------
    $registryText = Get-Content -Raw -Path $Registry

    if ($registryText -notmatch 'AeldariPackSkipTtsGhostComponent\s*\(') {
        $nullCheck = @'
            if (component == null ||
                string.IsNullOrWhiteSpace(
                    component.meshResource))
            {
                continue;
            }

            components.Add(
'@

        $nullCheckReplacement = @'
            if (component == null ||
                string.IsNullOrWhiteSpace(
                    component.meshResource))
            {
                continue;
            }

            // R25: raw TTS root/base wrappers must not become detached
            // "ghost bases" beside the actual miniature.
            if (AeldariPackSkipTtsGhostComponent(
                    selected,
                    component))
            {
                continue;
            }

            components.Add(
'@

        if (-not $registryText.Contains($nullCheck)) {
            throw "Could not locate the Aeldari pack component loop."
        }

        $registryText =
            $registryText.Replace(
                $nullCheck,
                $nullCheckReplacement
            )

        $oldPosition = @'
                    V(
                        component.position,
                        Vector3.zero
                    ),
'@

        $newPosition = @'
                    AeldariPackSafeLocalPosition(
                        selected,
                        component
                    ),
'@

        # Limit this replacement to the first occurrence after TryResolvePack's
        # component loop by replacing once.
        $positionIndex =
            $registryText.IndexOf(
                $oldPosition,
                $registryText.IndexOf(
                    "private static ModelVisualDefinition" +
                    "`r`n        TryResolvePack"
                )
            )

        if ($positionIndex -lt 0) {
            # Handle LF-only formatting.
            $positionIndex =
                $registryText.IndexOf(
                    $oldPosition,
                    $registryText.IndexOf(
                        "TryResolvePack("
                    )
                )
        }

        if ($positionIndex -lt 0) {
            throw "Could not locate the Aeldari pack component-position expression."
        }

        $registryText =
            $registryText.Substring(
                0,
                $positionIndex
            ) +
            $newPosition +
            $registryText.Substring(
                $positionIndex +
                $oldPosition.Length
            )

        $helperAnchor = @'
    private static string N(string value)
'@

        if (-not $registryText.Contains($helperAnchor)) {
            throw "Could not locate ModelVisualRegistry helper insertion point."
        }

        $helpers = @'
    // WARBOARD_R25_AELDARI_TTS_ROOT_SANITISATION
    private static bool AeldariPackHasChildComponents(
        ModelPackUnitData selected)
    {
        if (selected == null ||
            selected.components == null)
        {
            return false;
        }

        foreach (ModelPackComponentData candidate
            in selected.components)
        {
            if (candidate != null &&
                !string.IsNullOrWhiteSpace(
                    candidate.childPath))
            {
                return true;
            }
        }

        return false;
    }

    private static Vector3 AeldariPackFirstChildPosition(
        ModelPackUnitData selected)
    {
        if (selected == null ||
            selected.components == null)
        {
            return Vector3.zero;
        }

        foreach (ModelPackComponentData candidate
            in selected.components)
        {
            if (candidate != null &&
                !string.IsNullOrWhiteSpace(
                    candidate.childPath))
            {
                return
                    V(
                        candidate.position,
                        Vector3.zero
                    );
            }
        }

        return Vector3.zero;
    }

    private static bool AeldariPackSkipTtsGhostComponent(
        ModelPackUnitData selected,
        ModelPackComponentData component)
    {
        if (selected == null ||
            component == null)
        {
            return false;
        }

        bool hasChildren =
            AeldariPackHasChildComponents(
                selected
            );

        bool root =
            string.IsNullOrWhiteSpace(
                component.childPath
            );

        Vector3 raw =
            V(
                component.position,
                Vector3.zero
            );

        if (hasChildren &&
            root)
        {
            Vector2 horizontal =
                new Vector2(
                    raw.x,
                    raw.z
                );

            // Wraithknight-style TTS entries contain an untextured/world-
            // positioned parent wrapper plus correctly-local child meshes.
            if (string.IsNullOrWhiteSpace(
                    component.diffuseResource) ||
                horizontal.magnitude >
                    4.0f)
            {
                Debug.Log(
                    "Warboard R25 Aeldari: skipped TTS root/wrapper '" +
                    component.nickname +
                    "' from '" +
                    selected.name +
                    "'."
                );

                return true;
            }
        }

        if (hasChildren &&
            !root)
        {
            Vector3 reference =
                AeldariPackFirstChildPosition(
                    selected
                );

            Vector2 delta =
                new Vector2(
                    raw.x - reference.x,
                    raw.z - reference.z
                );

            if (delta.magnitude >
                8.0f)
            {
                Debug.LogWarning(
                    "Warboard R25 Aeldari: skipped detached child component '" +
                    component.nickname +
                    "' from '" +
                    selected.name +
                    "' (offset " +
                    delta.magnitude.ToString("F2") +
                    ")."
                );

                return true;
            }
        }

        return false;
    }

    private static Vector3 AeldariPackSafeLocalPosition(
        ModelPackUnitData selected,
        ModelPackComponentData component)
    {
        Vector3 raw =
            V(
                component != null
                ? component.position
                : null,
                Vector3.zero
            );

        if (component == null)
            return raw;

        bool root =
            string.IsNullOrWhiteSpace(
                component.childPath
            );

        bool hasChildren =
            AeldariPackHasChildComponents(
                selected
            );

        if (root &&
            !hasChildren)
        {
            Vector2 horizontal =
                new Vector2(
                    raw.x,
                    raw.z
                );

            // Yvraine-style single-component TTS objects contain their source
            // tabletop X/Z. The mesh itself is valid; only its placement is
            // not. Keep the normal local Y and anchor X/Z to the model token.
            if (horizontal.magnitude >
                4.0f)
            {
                return
                    new Vector3(
                        0f,
                        raw.y,
                        0f
                    );
            }
        }

        return raw;
    }

'@

        $registryText =
            $registryText.Replace(
                $helperAnchor,
                $helpers +
                $helperAnchor
            )

        Set-Content -Path $Registry -Value $registryText -Encoding UTF8
    }

    # -----------------------------------------------------------------
    # 3. MISSION SETUP UI:
    #    The 210px Layout button clips "LAYOUT A | SWEEPING ENGAGEMENT".
    #    Give it real width and move attacker control alongside it.
    # -----------------------------------------------------------------
    $uiText = Get-Content -Raw -Path $Ui

    $oldLayoutBlock = @'
        if (GUI.Button(
            new Rect(
                settings.x + 14f,
                settings.y + 48f,
                210f,
                34f
            ),
            "LAYOUT " +
            previewBattlefield.LayoutLabel +
            "  |  " +
            previewBattlefield.DisplayName
                .ToUpper()))
'@

    $newLayoutBlock = @'
        if (GUI.Button(
            new Rect(
                settings.x + 14f,
                settings.y + 48f,
                300f,
                34f
            ),
            "LAYOUT " +
            previewBattlefield.LayoutLabel +
            "  |  " +
            previewBattlefield.DisplayName
                .ToUpper()))
'@

    if ($uiText.Contains($oldLayoutBlock)) {
        $uiText =
            $uiText.Replace(
                $oldLayoutBlock,
                $newLayoutBlock
            )
    }
    elseif ($uiText -notmatch 'settings\.x\s*\+\s*14f[\s\S]{0,120}300f,[\s\S]{0,180}previewBattlefield\.DisplayName') {
        throw "Could not locate the mission Layout button for UI repair."
    }

    $oldAttackerRect = @'
                settings.x + 238f,
                settings.y + 48f,
                290f,
                34f
'@

    $newAttackerRect = @'
                settings.x + 328f,
                settings.y + 48f,
                250f,
                34f
'@

    if ($uiText.Contains($oldAttackerRect)) {
        $uiText =
            $uiText.Replace(
                $oldAttackerRect,
                $newAttackerRect
            )
    }

    Set-Content -Path $Ui -Value $uiText -Encoding UTF8

    # -----------------------------------------------------------------
    # VERIFY BEFORE REMOVING OLD RESOLVERS.
    # -----------------------------------------------------------------
    $verifySquad = Get-Content -Raw -Path $Squad
    $verifyRegistry = Get-Content -Raw -Path $Registry
    $verifyUi = Get-Content -Raw -Path $Ui
    $verifyResolver = Get-Content -Raw -Path $Resolver

    $resolverCalls =
        ([regex]::Matches(
            $verifySquad,
            'ExtendedFactionModelPackResolverR25\.TryResolve'
        )).Count

    if ($resolverCalls -ne 2) {
        throw "Expected two R25 extended resolver calls in SquadController; found $resolverCalls."
    }

    if ($verifySquad -match 'NecronVisualOriginRepairR23\.Reanchor') {
        throw "Old post-spawn Necron recenter hack still remains."
    }

    if ($verifyRegistry -notmatch 'WARBOARD_R25_AELDARI_TTS_ROOT_SANITISATION' -or
        $verifyRegistry -notmatch 'AeldariPackSafeLocalPosition') {
        throw "Aeldari ghost-base sanitisation verification failed."
    }

    if ($verifyUi -notmatch 'settings\.x\s*\+\s*14f,[\s\S]{0,80}300f,[\s\S]{0,180}previewBattlefield\.DisplayName') {
        throw "Mission setup Layout-button width verification failed."
    }

    if ($verifyResolver -notmatch 'WARBOARD_EXTENDED_FACTION_MODEL_RESOLVER_R25' -or
        $verifyResolver -notmatch 'new PackSpec\("Orks"' -or
        $verifyResolver -notmatch 'new PackSpec\("Tyranids"') {
        throw "R25 resolver payload verification failed."
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Restoring backup..." -ForegroundColor Red

    foreach ($relative in $backupFiles) {
        $saved = Join-Path $Backup $relative
        $dest = Join-Path $ProjectRoot $relative

        if (Test-Path $saved) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
            Copy-Item $saved $dest -Force
        }
    }

    if (-not (Test-Path (Join-Path $Backup "Assets\Scripts\Core\ExtendedFactionModelPackResolverR25.cs")) -and
        (Test-Path $Resolver)) {
        Remove-Item $Resolver -Force -ErrorAction SilentlyContinue
    }

    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

# Runtime verification passed; old experimental resolvers can now go.
$obsolete = @(
    "NecronModelPackResolverR22.cs",
    "NecronModelPackResolverR22.cs.meta",
    "NecronVisualOriginRepairR23.cs",
    "NecronVisualOriginRepairR23.cs.meta",
    "FactionModelPackResolver.cs",
    "FactionModelPackResolver.cs.meta"
)

foreach ($name in $obsolete) {
    Safe-Remove (Join-Path $Core $name)
}

Write-Host ""
Write-Host "R25 installed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Fixed:" -ForegroundColor Cyan
Write-Host "  - Orks use the same pre-spawn TTS anchoring principle that made Necrons work"
Write-Host "  - Tyranids are wired through the same path now as well"
Write-Host "  - Necrons remain on that same local-transform path"
Write-Host "  - Wraithknight-style TTS parent wrappers are no longer spawned as ghost pieces"
Write-Host "  - Yvraine-style single TTS roots have world X/Z removed before spawning"
Write-Host "  - absurd detached Aeldari child components are skipped"
Write-Host "  - mission Layout button widened so SWEEPING ENGAGEMENT is not chopped off"
Write-Host ""
Write-Host "Aeldari/Custodes matching logic itself was NOT replaced." -ForegroundColor Green
Write-Host "Backup: $Backup" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity and let it compile. Then START A FRESH BATTLE." -ForegroundColor Yellow
Write-Host ""
Read-Host "Press Enter to close"
