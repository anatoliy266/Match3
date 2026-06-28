# Rebalance all 100 levels using game-design formula with challenge factor

$levelsDir = "C:\Users\anatoliy26\source\Unity\Match3\Assets\Game\ScriptableObjects\Levels"

# Difficulty pattern for all 100 levels (grouped for progression)
# E=Easy(0), M=Medium(1), H=Hard(2)
$difficultyPattern = @(
    # Block 1: Levels 1-6 (tutorial opening)
    'E','E','E','E','M','H',
    # Block 2: Levels 7-12
    'E','E','M','H','M','E',
    # Block 3: Levels 13-18
    'E','M','H','M','E','M',
    # Block 4: Levels 19-24
    'M','H','M','E','M','H',
    # Block 5: Levels 25-30
    'E','M','H','M','H','M',
    # Block 6: Levels 31-36
    'M','H','E','M','H','M',
    # Block 7: Levels 37-42
    'H','M','M','H','E','M',
    # Block 8: Levels 43-48
    'M','H','M','H','M','H',
    # Block 9: Levels 49-54
    'E','H','M','M','H','M',
    # Block 10: Levels 55-60
    'M','H','H','M','H','M',
    # Block 11: Levels 61-66
    'M','H','H','M','H','M',
    # Block 12: Levels 67-72
    'H','M','H','H','M','H',
    # Block 13: Levels 73-78
    'M','H','H','M','H','H',
    # Block 14: Levels 79-84
    'H','M','H','H','M','H',
    # Block 15: Levels 85-90
    'H','H','M','H','H','M',
    # Block 16: Levels 91-96
    'M','H','H','H','H','M',
    # Block 17: Levels 97-100
    'H','H','M','H'
)

# CF values per difficulty
$cfMap = @{ 'E' = 1.4; 'M' = 1.0; 'H' = 0.7 }
$diffEnumMap = @{ 'E' = 0; 'M' = 1; 'H' = 2 }

function Get-KChaos {
    param([double]$freeRatio)
    if ($freeRatio -gt 0.8) { return 0.6 }
    if ($freeRatio -gt 0.6) { return 0.7 }
    return 0.9
}

$results = @()

for ($i = 1; $i -le 100; $i++) {
    $path = Join-Path $levelsDir "Level$i.asset"
    if (-not (Test-Path $path)) {
        Write-Warning "Level$i.asset not found!"
        continue
    }

    $lines = Get-Content $path
    $content = [string]::Join("`n", $lines)

    # Parse Rows
    $rowsLine = $lines | Select-String "^\s+Rows:\s+(\d+)$"
    $rows = [int]$rowsLine.Matches.Groups[1].Value

    # Parse Columns
    $colsLine = $lines | Select-String "^\s+Columns:\s+(\d+)$"
    $cols = [int]$colsLine.Matches.Groups[1].Value

    # Parse Steps
    $stepsLine = $lines | Select-String "^\s+Steps:\s+(\d+)$"
    $currentSteps = [int]$stepsLine.Matches.Groups[1].Value

    # Count total blockers in Blockers section
    $blockerCount = 0
    $inBlockers = $false
    foreach ($line in $lines) {
        if ($line -match "^\s+Blockers:") { $inBlockers = $true; continue }
        if ($inBlockers) {
            if ($line -match "^\s+- Type:") { $blockerCount++ }
            elseif ($line -notmatch "^\s+") { $inBlockers = $false }
        }
    }

    # Parse ropesGoalsList: sum regular goals (KindType: 0) and box blocker goals (KindType: 2, BlockerType: 0)
    $regularGoalSum = 0
    $boxGoalSum = 0
    $inGoals = $false
    $currentKindType = -1
    $currentBlockerType = -1
    for ($li = 0; $li -lt $lines.Count; $li++) {
        $line = $lines[$li]
        if ($line -match "^\s+ropesGoalsList:") { $inGoals = $true; continue }
        if ($inGoals) {
            if ($line -match "^\s+KindType:\s+(\d+)") {
                $currentKindType = [int]$Matches[1]
            }
            elseif ($line -match "^\s+BlockerType:\s+(\d+)") {
                $currentBlockerType = [int]$Matches[1]
            }
            elseif ($line -match "^\s+count:\s+(\d+)") {
                $count = [int]$Matches[1]
                if ($currentKindType -eq 0) {
                    $regularGoalSum += $count
                }
                elseif ($currentKindType -eq 2 -and $currentBlockerType -eq 0) {
                    $boxGoalSum += $count
                }
                $currentKindType = -1
                $currentBlockerType = -1
            }
            # Stop when we hit a non-indented line (next top-level field)
            if ($line -match "^\S" -and $line -notmatch "^\s") { $inGoals = $false }
        }
    }

    # Calculate formula
    $totalCells = $rows * $cols
    $freeCells = $totalCells - $blockerCount
    $freeRatio = $freeCells / $totalCells
    $kChaos = Get-KChaos $freeRatio
    $tTotal = $regularGoalSum + $boxGoalSum

    $difficulty = $difficultyPattern[$i - 1]
    $cf = $cfMap[$difficulty]
    $diffEnum = $diffEnumMap[$difficulty]

    $recSteps = [Math]::Max(3, [Math]::Round(($tTotal / 1.2) * $kChaos * $cf))

    # Modify Steps line and add Difficulty after it
    $newLines = @()
    foreach ($line in $lines) {
        if ($line -match "^\s+Steps:\s+(\d+)$") {
            $newLines += "  Steps: $recSteps"
            $newLines += "  Difficulty: $diffEnum"
        } else {
            $newLines += $line
        }
    }

    Set-Content -Path $path -Value ($newLines -join "`n") -NoNewline

    $results += [PSCustomObject]@{
        Level = $i
        Rows = $rows
        Cols = $cols
        Blockers = $blockerCount
        FreeCells = $freeCells
        FreeRatio = [Math]::Round($freeRatio, 3)
        KChaos = $kChaos
        RegGoals = $regularGoalSum
        BoxGoals = $boxGoalSum
        TTotal = $tTotal
        Difficulty = $difficulty
        CF = $cf
        OldSteps = $currentSteps
        NewSteps = $recSteps
    }
}

Write-Host "`n=== Rebalance Complete ==="
Write-Host ""

# Display results as table
$results | Format-Table -Property @{L='Lv';E={$_.Level}},
    @{L='Br';E={$_.Rows}}, @{L='Bc';E={$_.Cols}},
    @{L='Blk';E={$_.Blockers}},
    @{L='Free';E={$_.FreeCells}},
    @{L='K';E={$_.KChaos}},
    @{L='RegG';E={$_.RegGoals}},
    @{L='BoxG';E={$_.BoxGoals}},
    @{L='T';E={$_.TTotal}},
    @{L='Diff';E={$_.Difficulty}},
    @{L='CF';E={$_.CF}},
    @{L='OldS';E={$_.OldSteps}},
    @{L='NewS';E={$_.NewSteps}} -AutoSize

Write-Host "`n=== Summary ==="
$easy = ($results | Where-Object { $_.Difficulty -eq 'E' }).Count
$med = ($results | Where-Object { $_.Difficulty -eq 'M' }).Count
$hard = ($results | Where-Object { $_.Difficulty -eq 'H' }).Count
Write-Host "Easy: $easy  Medium: $med  Hard: $hard"

$avgOld = [Math]::Round(($results | Measure-Object OldSteps -Average).Average, 1)
$avgNew = [Math]::Round(($results | Measure-Object NewSteps -Average).Average, 1)
Write-Host "Average steps: $avgOld -> $avgNew"

# Check which levels had biggest changes
$sorted = $results | Sort-Object { $_.NewSteps / $_.OldSteps }
Write-Host "`n=== Biggest reductions (new steps / old steps ratio) ==="
$sorted | Select-Object -First 10 | Format-Table Level, OldSteps, NewSteps, @{L='Ratio';E={[Math]::Round($_.NewSteps/$_.OldSteps, 2)}}

Write-Host "`n=== Biggest increases (new steps / old steps ratio) ==="
$sorted | Select-Object -Last 10 | Sort-Object Level | Format-Table Level, OldSteps, NewSteps, @{L='Ratio';E={[Math]::Round($_.NewSteps/$_.OldSteps, 2)}}

Write-Host "`nDone! All 100 levels rebalanced."
