# Генератор 100 уровней для Match3
$levelsDir = "C:\Users\anatoliy26\source\Unity\Match3\Assets\Game\ScriptableObjects\Levels"

# Фон-спрайты (guid, fileID)
$bgSprites = @(
    @{guid="b1b5df2416c31df4ebc68703523df9cf"; fileID="-8212735365783306809"},  # Anima_00537_
    @{guid="e02ff33c904d6644c98ff3803cb174c1"; fileID="-5239265548855951963"},  # Anima_00540_
    @{guid="b6282fa3d3a54e64595329e3da83f471"; fileID="944179808672642539"},    # Anima_00004_
    @{guid="d4c49d210df40664c9eec4ce297eac84"; fileID="3287579595892397182"},    # Anima_00040_
    @{guid="3400857689f132a429c7d83d46a526d7"; fileID="-1418062074237323280"},   # 1781446396
    @{guid="f2fd6eca5611ce647b5ec80000a69dfe"; fileID="8750230940053459384"}     # monitor222
)

# Конвертирует список int-значений enum в hex-строку (little-endian)
function EncodeEnumList {
    param([int[]]$values)
    $sb = [System.Text.StringBuilder]::new()
    foreach ($v in $values) {
        $bytes = [BitConverter]::GetBytes($v)
        foreach ($b in $bytes) {
            $sb.Append($b.ToString("x2")) | Out-Null
        }
    }
    return $sb.ToString()
}

# Генерирует случайный GUID в Unity-формате (32 hex без дефисов)
function New-UnityGuid {
    return [System.Guid]::NewGuid().ToString("N").ToLower()
}

# Создаёт содержимое .meta файла
function New-MetaContent {
    param([string]$guid)
    return @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 

"@
}

# Генерирует YAML для RopeGoal
function New-RopeGoalYaml {
    param(
        [int]$kindType,      # 0=Regular, 1=Bonus
        [int]$regularType,   # 0-5
        [int]$bonusType,     # 0-2
        [int]$count,
        [int]$targetColor = -1  # -1 значит null
    )
    $sb = [System.Text.StringBuilder]::new()
    $sb.AppendLine("  - Kind:") | Out-Null
    $sb.AppendLine("      KindType: $kindType") | Out-Null
    $sb.AppendLine("      RegularType: $regularType") | Out-Null
    $sb.AppendLine("      BonusType: $bonusType") | Out-Null
    if ($targetColor -ge 0) {
        $sb.AppendLine("      TargetColor:") | Out-Null
        $sb.AppendLine("        RegularType: $targetColor") | Out-Null
    }
    $sb.AppendLine("    count: $count") | Out-Null
    return $sb.ToString().TrimEnd()
}

# Генерирует .asset файл
function New-LevelAsset {
    param(
        [int]$levelNumber,
        [int]$rows,
        [int]$cols,
        [int]$steps,
        [int[]]$regularTypes,
        [int[]]$bonusTypes,
        [string]$bgGuid,
        [string]$bgFileId,
        [string[]]$goalYamlLines
    )

    $regularHex = EncodeEnumList $regularTypes
    $bonusHex = EncodeEnumList $bonusTypes

    $goalsYaml = ""
    if ($goalYamlLines.Count -gt 0) {
        $goalsYaml = "  ropesGoalsList:" + [Environment]::NewLine
        foreach ($g in $goalYamlLines) {
            $goalsYaml += $g + [Environment]::NewLine
        }
    } else {
        $goalsYaml = "  ropesGoalsList: []"
    }

    return @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 22155ef6179a3294785284ce33da5b00, type: 3}
  m_Name: Level$levelNumber
  m_EditorClassIdentifier: Assembly-CSharp::LevelSettings
  levelNumber: $levelNumber
  Rows: $rows
  Columns: $cols
  Steps: $steps
  RegiularTilesList: $regularHex
  BonusTilesList: $bonusHex
  backgroundSprite: {fileID: $bgFileId, guid: $bgGuid, type: 3}
$goalsYaml
"@
}

# ============================================================
# ОПРЕДЕЛЕНИЯ ВСЕХ 100 УРОВНЕЙ
# ============================================================

$levelDefs = @()

# Регулярные цвета: 0=Red, 1=Green, 2=Blue, 3=Yellow, 4=Orange, 5=Purple
# Бонусы: 0=Bomb, 1=VerticalBomb, 2=HorizontalBomb

for ($i = 1; $i -le 100; $i++) {
    $levelNum = $i

    # === Базовые параметры (прогрессия) ===
    if ($i -le 10) {
        # Уровни 1-10: Введение
        $rows = 6 + [Math]::Floor(($i - 1) / 5)
        $cols = 6 + [Math]::Floor(($i - 1) / 5)
        $steps = 30 - ($i - 1)  # 30..21
        $numColors = 3
        $numBonuses = 0
        $numGoals = 1
        $goalCountBase = 8
    } elseif ($i -le 20) {
        # Уровни 11-20
        $rows = 7 + [Math]::Floor(($i - 11) / 5)
        $cols = 7 + [Math]::Floor(($i - 11) / 5)
        $steps = 25 - ($i - 11)  # 25..16
        $numColors = 3 + [Math]::Floor(($i - 11) / 5)
        $numBonuses = 0
        $numGoals = 1 + [Math]::Floor(($i - 11) / 5)
        $goalCountBase = 10
    } elseif ($i -le 30) {
        # Уровни 21-30
        $rows = 7 + [Math]::Floor(($i - 21) / 5)
        $cols = 8 + [Math]::Floor(($i - 21) / 5)
        $steps = 22 - ($i - 21)  # 22..13
        $numColors = 4
        $numBonuses = 1
        $numGoals = 2
        $goalCountBase = 12
    } elseif ($i -le 40) {
        # Уровни 31-40
        $rows = 8 + [Math]::Floor(($i - 31) / 5)
        $cols = 8 + [Math]::Floor(($i - 31) / 5)
        $steps = 20 - ($i - 31)  # 20..11
        $numColors = 4 + [Math]::Floor(($i - 31) / 5)
        $numBonuses = 1
        $numGoals = 2
        $goalCountBase = 15
    } elseif ($i -le 50) {
        # Уровни 41-50
        $rows = 8 + [Math]::Floor(($i - 41) / 5)
        $cols = 9 + [Math]::Floor(($i - 41) / 5)
        $steps = 18 - ($i - 41)  # 18..9
        $numColors = 5
        $numBonuses = 2
        $numGoals = 2 + [Math]::Floor(($i - 41) / 10)
        $goalCountBase = 18
    } elseif ($i -le 60) {
        # Уровни 51-60
        $rows = 9 + [Math]::Floor(($i - 51) / 5)
        $cols = 9 + [Math]::Floor(($i - 51) / 5)
        $steps = 15 - ($i - 51)  # 15..6
        $numColors = 5 + [Math]::Floor(($i - 51) / 10)
        $numBonuses = 3
        $numGoals = 2 + [Math]::Floor(($i - 51) / 10)
        $goalCountBase = 20
    } elseif ($i -le 70) {
        # Уровни 61-70
        $rows = 9 + [Math]::Floor(($i - 61) / 5)
        $cols = 10 + [Math]::Floor(($i - 61) / 5)
        $steps = 15 - ($i - 61)  # 15..6
        $numColors = 6
        $numBonuses = 3
        $numGoals = 3
        $goalCountBase = 22
    } elseif ($i -le 80) {
        # Уровни 71-80
        $rows = 10 + [Math]::Floor(($i - 71) / 5)
        $cols = 10 + [Math]::Floor(($i - 71) / 5)
        $steps = 12 - ($i - 71)  # 12..3
        $numColors = 6
        $numBonuses = 3
        $numGoals = 3
        $goalCountBase = 25
    } elseif ($i -le 90) {
        # Уровни 81-90
        $rows = 10 + [Math]::Floor(($i - 81) / 5)
        $cols = 11 + [Math]::Floor(($i - 81) / 5)
        $steps = 12 - ($i - 81)  # 12..3
        $numColors = 6
        $numBonuses = 3
        $numGoals = 3
        $goalCountBase = 28
    } else {
        # Уровни 91-100
        $rows = 11 + [Math]::Floor(($i - 91) / 5)
        $cols = 11 + [Math]::Floor(($i - 91) / 5)
        $steps = 10 - ($i - 91)  # 10..1
        $numColors = 6
        $numBonuses = 3
        $numGoals = 3
        $goalCountBase = 30
    }

    # Защита: steps не менее 3
    if ($steps -lt 3) { $steps = 3 }

    # === Выбор цветов ===
    # Всегда выбираем первые numColors цветов из 6
    $usedColors = @()
    for ($c = 0; $c -lt $numColors; $c++) {
        $usedColors += $c
    }

    # === Выбор бонусов ===
    $usedBonuses = @()
    for ($b = 0; $b -lt $numBonuses; $b++) {
        $usedBonuses += $b
    }

    # === Выбор фона (циклически) ===
    $bgIdx = ($i - 1) % $bgSprites.Count
    $bg = $bgSprites[$bgIdx]

    # === Цели ===
    $goals = @()
    # Определяем какие цвета будут в целях
    $goalColorCount = [Math]::Min($numGoals, $numColors)
    # Для первых 10 уровней — цель на один конкретный цвет
    if ($i -le 10) {
        $goalColor = $i % $numColors
        $gc = $goalCountBase + ($i % 3) * 2
        $goals += (New-RopeGoalYaml 0 $goalColor 0 $gc)
    } elseif ($i -le 20) {
        for ($gi = 0; $gi -lt $goalColorCount; $gi++) {
            $gc = $goalCountBase + ($i % 5) * 2 + $gi * 3
            $goals += (New-RopeGoalYaml 0 $gi 0 $gc)
        }
    } else {
        # Для остальных уровней выбираем цвета для целей со смещением
        $offset = $i % $numColors
        for ($gi = 0; $gi -lt $goalColorCount; $gi++) {
            $colorIdx = ($offset + $gi) % $numColors
            $gc = $goalCountBase + ($i % 7) * 3 + $gi * 2
            $goals += (New-RopeGoalYaml 0 $colorIdx 0 $gc)
        }
    }

    $levelDefs += @{
        number = $i
        rows = $rows
        cols = $cols
        steps = $steps
        regularTypes = $usedColors
        bonusTypes = $usedBonuses
        bgGuid = $bg.guid
        bgFileId = $bg.fileID
        goals = $goals
    }
}

# ============================================================
# ГЕНЕРАЦИЯ ФАЙЛОВ
# ============================================================

Write-Host "Generating 100 level files..."

# Сначала удаляем старые Level*.asset (кроме Level1, Level2, Levels, InfiniteLevel)
Get-ChildItem -Path $levelsDir -Filter "Level*.asset" | Where-Object {
    $_.Name -notmatch '^(Levels\.asset|Level1\.asset|Level2\.asset|InfiniteLevel\.asset)$'
} | Remove-Item -Force

# Также удаляем соответствующие .meta
Get-ChildItem -Path $levelsDir -Filter "Level*.asset.meta" | Where-Object {
    $_.Name -notmatch '^(Levels\.asset\.meta|Level1\.asset\.meta|Level2\.asset\.meta|InfiniteLevel\.asset\.meta)$'
} | Remove-Item -Force

$allGuids = @{}
# Сохраняем guids для Level1, Level2 и InfiniteLevel (читаем из их .meta)
$existingMetas = @{
    1 = (Get-Content "$levelsDir\Level1.asset.meta" | Select-String -Pattern "guid: (.+)" | ForEach-Object { $_.Matches.Groups[1].Value })
    2 = (Get-Content "$levelsDir\Level2.asset.meta" | Select-String -Pattern "guid: (.+)" | ForEach-Object { $_.Matches.Groups[1].Value })
}
$allGuids[1] = $existingMetas[1]
$allGuids[2] = $existingMetas[2]

# Генерируем уровни 3-100 (1 и 2 уже существуют)
foreach ($def in $levelDefs) {
    $i = $def.number
    if ($i -le 2) {
        # Пропускаем Level1 и Level2 — они уже существуют
        $allGuids[$i] = $existingMetas[$i]
        continue
    }

    # Генерируем уникальный guid для .meta
    $guid = New-UnityGuid
    $allGuids[$i] = $guid

    # Создаём .asset контент
    $assetContent = New-LevelAsset `
        -levelNumber $i `
        -rows $def.rows `
        -cols $def.cols `
        -steps $def.steps `
        -regularTypes $def.regularTypes `
        -bonusTypes $def.bonusTypes `
        -bgGuid $def.bgGuid `
        -bgFileId $def.bgFileId `
        -goalYamlLines $def.goals

    $assetPath = "$levelsDir\Level$i.asset"
    $metaPath = "$levelsDir\Level$i.asset.meta"

    Set-Content -Path $assetPath -Value $assetContent -NoNewline
    Set-Content -Path $metaPath -Value (New-MetaContent $guid) -NoNewline

    Write-Host ("  Created Level" + $i + ".asset (" + $rows + "x" + $cols + ", " + $steps + " steps, " + $def.regularTypes.Count + " colors, " + $def.goals.Count + " goals)")
}

# ============================================================
# ОБНОВЛЕНИЕ Levels.asset
# ============================================================

Write-Host "`nUpdating Levels.asset..."

$levelsAssetPath = "$levelsDir\Levels.asset"

# Получаем guid InfiniteLevel
$infiniteGuid = Get-Content "$levelsDir\InfiniteLevel.asset.meta" | Select-String -Pattern "guid: (.+)" | ForEach-Object { $_.Matches.Groups[1].Value }

# Строим levelsMap: Level1, Level2, Level3...Level100, InfiniteLevel
$levelRefs = @()
for ($i = 1; $i -le 100; $i++) {
    $g = $allGuids[$i]
    $levelRefs += "- {fileID: 11400000, guid: $g, type: 2}"
}
# Добавляем InfiniteLevel
$levelRefs += "- {fileID: 11400000, guid: $infiniteGuid, type: 2}"

$levelsYaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 66936ea3d818815439a11747331ab522, type: 3}
  m_Name: Levels
  m_EditorClassIdentifier: Assembly-CSharp::Levels
  levelsMap:
  $($levelRefs -join "`n  ")
"@

Set-Content -Path $levelsAssetPath -Value $levelsYaml -NoNewline

Write-Host "  Updated Levels.asset with 100 levels + InfiniteLevel"
Write-Host "`nDone! Generated 100 levels successfully."
