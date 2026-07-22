# Get actual workflow data
$runs = Invoke-RestMethod -Uri "http://localhost:7071/api/github/actions" -UseBasicParsing
Write-Host "Type: $($runs.GetType().Name)" -ForegroundColor Cyan
Write-Host "Count: $($runs.Count)" -ForegroundColor Cyan
if ($runs.error) {
    Write-Host "ERROR: $($runs.error)" -ForegroundColor Red
    Write-Host "Message: $($runs.message)" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "=== SUCCESS! ===" -ForegroundColor Green
    Write-Host "Retrieved $($runs.Count) workflow runs" -ForegroundColor Cyan
    Write-Host ""
    $runs | Select-Object -First 15 | ForEach-Object {
        $icon = if ($_.conclusion -eq "success") { "?" } elseif ($_.conclusion -eq "failure") { "?" } else { "?" }
        $date = [DateTime]::Parse($_.createdAt).ToString("yyyy-MM-dd HH:mm")
        Write-Host "$icon $date | $($_.name)" -ForegroundColor White
        Write-Host "   Branch: $($_.headBranch) | Status: $($_.status)" -ForegroundColor Gray
        Write-Host "   ?? https://github.com/AdaptiveBS/CIApp/actions/runs/$($_.id)" -ForegroundColor DarkGray
        Write-Host ""
    }
    $s = ($runs | Where-Object {$_.conclusion -eq "success"}).Count
    $f = ($runs | Where-Object {$_.conclusion -eq "failure"}).Count
    Write-Host "=== SUMMARY ===" -ForegroundColor Yellow
    Write-Host "Total: $($runs.Count)" -ForegroundColor Cyan
    Write-Host "? Success: $s" -ForegroundColor Green  
    Write-Host "? Failed: $f" -ForegroundColor Red
}
