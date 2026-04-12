$content = Get-Content 'c:\Users\atw61\OneDrive\Desktop\new\MagicalDeckbuilder.Shared\Game\CardFactory.cs' -Raw
$matches = [regex]::Matches($content, 'ID: (1[0-9]{3})')
$usedIds = @()
foreach ($m in $matches) {
    $usedIds += [int]$m.Groups[1].Value
}
$usedIds = $usedIds | Sort-Object -Unique

$allIds = 1000..1300
$availableIds = $allIds | Where-Object { $_ -notin $usedIds }
Write-Host "Available count: $($availableIds.Count)"
Write-Host "Available (first 35): $($availableIds[0..34] -join ', ')"