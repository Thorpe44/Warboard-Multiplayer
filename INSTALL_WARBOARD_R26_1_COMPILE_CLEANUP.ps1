$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
function Find-Root([string]$Start) {
  $p=(Resolve-Path $Start).Path
  for($i=0;$i-lt 12;$i++){
    if((Test-Path (Join-Path $p "Assets\Scripts\Core\GameController.cs")) -and (Test-Path (Join-Path $p "ProjectSettings\ProjectVersion.txt"))){return $p}
    $q=Split-Path -Parent $p; if([string]::IsNullOrWhiteSpace($q)-or$q-eq$p){break}; $p=$q
  }
  return $null
}
$ProjectRoot=Find-Root $ScriptDir
if(-not $ProjectRoot){Write-Host "ERROR: Warboard root not found" -ForegroundColor Red; Read-Host; exit 1}
$Core=Join-Path $ProjectRoot "Assets\Scripts\Core"
$R26=Join-Path $Core "WarboardUiReadabilityR26.cs"
$Core11=Join-Path $Core "CoreRules11Completion.cs"
$Side=Join-Path $Core "WarboardV45PhysicalSideTrays.cs"
$Terrain=Join-Path $Core "GameController.V50TerrainAreaBattlefield.cs"
$V48=Join-Path $Core "GameController.V48CoreAlignment.cs"
foreach($f in @($R26,$Core11,$Side,$Terrain,$V48)){if(-not(Test-Path $f)){Write-Host "ERROR missing $f" -ForegroundColor Red; Read-Host; exit 1}}
$stamp=Get-Date -Format "yyyyMMdd_HHmmss"; $backup=Join-Path $ProjectRoot "Library\WarboardBackups\R26_1_COMPILE_CLEANUP\$stamp"; New-Item -ItemType Directory -Force -Path $backup|Out-Null
foreach($f in @($R26,$Core11,$Side,$Terrain,$V48)){Copy-Item $f (Join-Path $backup (Split-Path -Leaf $f)) -Force}
try{
  Copy-Item (Join-Path $ScriptDir "PATCH_PAYLOAD\Assets\Scripts\Core\WarboardUiReadabilityR26.cs") $R26 -Force
  $t=Get-Content -Raw $Core11
  $t=$t -replace '\.FindObjectsOfType<TerrainFeature>\(\)', '.FindObjectsByType<TerrainFeature>(FindObjectsInactive.Exclude)'
  Set-Content $Core11 $t -Encoding UTF8
  $t=Get-Content -Raw $Side
  $t=[regex]::Replace($t,'(?s)UnityEngine\.Object\s*\.FindObjectsByType<\s*BattlefieldWorldUI\s*>\s*\(\s*FindObjectsInactive\.Include\s*,\s*FindObjectsSortMode\.None\s*\)','UnityEngine.Object.FindObjectsByType<BattlefieldWorldUI>(FindObjectsInactive.Include)',1)
  Set-Content $Side $t -Encoding UTF8
  $t=Get-Content -Raw $Terrain
  $t=[regex]::Replace($t,'(?s)Object\.FindObjectsByType<\s*TerrainFeature\s*>\s*\(\s*FindObjectsSortMode\.None\s*\)','Object.FindObjectsByType<TerrainFeature>()',1)
  Set-Content $Terrain $t -Encoding UTF8
  $t=Get-Content -Raw $V48
  $t=[regex]::Replace($t,'(?m)^\s*private\s+bool\s+v48ChargeCommandRerolled\s*;\s*\r?\n','')
  $t=[regex]::Replace($t,'(?m)^\s*v48ChargeCommandRerolled\s*=\s*(?:true|false)\s*;\s*(?://[^\r\n]*)?\r?\n','')
  Set-Content $V48 $t -Encoding UTF8
}catch{
  foreach($f in @($R26,$Core11,$Side,$Terrain,$V48)){ $b=Join-Path $backup (Split-Path -Leaf $f); if(Test-Path $b){Copy-Item $b $f -Force}}
  Write-Host "FAILED - restored backup" -ForegroundColor Red; Write-Host $_.Exception.Message; Read-Host; exit 1
}
Write-Host "R26.1 installed." -ForegroundColor Green
Write-Host "Fixed CS0104 plus the obsolete Unity API warnings you pasted." -ForegroundColor Green
Write-Host "Backup: $backup" -ForegroundColor DarkGray
Read-Host "Press Enter to close"
