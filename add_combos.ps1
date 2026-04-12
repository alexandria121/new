$lines = Get-Content 'c:\Users\atw61\OneDrive\Desktop\new\MagicalDeckbuilder.Shared\Game\CardFactory.cs'
$insertLines = Get-Content 'c:\Users\atw61\OneDrive\Desktop\new\NewComboCards.cs'
$output = @()
$found = $false

foreach ($line in $lines) {
    if ($line -eq '        return deck;' -and -not $found) {
        foreach ($il in $insertLines) {
            $output += $il
        }
        $found = $true
    }
    $output += $line
}

Set-Content 'c:\Users\atw61\OneDrive\Desktop\new\MagicalDeckbuilder.Shared\Game\CardFactory.cs' $output
Write-Host "Done! Added 25 combo cards."