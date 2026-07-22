# Check what error Functions are actually returning
Write-Host "Checking detailed error..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:7071/api/github/actions" -ErrorAction Stop
    $content = $response.Content | ConvertFrom-Json
    Write-Host "Success!" -ForegroundColor Green
    Write-Host "Got $($content.Count) runs" -ForegroundColor Cyan
} catch {
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        Write-Host "Response: $responseBody" -ForegroundColor Yellow
    } else {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}
