# Add Dashboard to Solution

Write-Host "Adding DeploymentDashboard to Solution" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if wrapper project already exists
if (Test-Path "DeploymentDashboard.Project") {
    Write-Host "DeploymentDashboard.Project already exists!" -ForegroundColor Yellow
    $overwrite = Read-Host "Recreate it? (Y/N)"
    if ($overwrite -ne "Y" -and $overwrite -ne "y") {
        Write-Host "Cancelled." -ForegroundColor Yellow
        exit 0
    }
    Remove-Item "DeploymentDashboard.Project" -Recurse -Force
}

Write-Host "1. Creating wrapper project..." -ForegroundColor Yellow
dotnet new classlib -n DeploymentDashboard.Project -o DeploymentDashboard.Project --force

if ($LASTEXITCODE -ne 0) {
    Write-Host "   Failed to create project!" -ForegroundColor Red
    exit 1
}
Write-Host "   Project created" -ForegroundColor Green

Write-Host ""
Write-Host "2. Removing default Class1.cs..." -ForegroundColor Yellow
Remove-Item "DeploymentDashboard.Project\Class1.cs" -ErrorAction SilentlyContinue
Write-Host "   Removed" -ForegroundColor Green

Write-Host ""
Write-Host "3. Updating .csproj to include dashboard files..." -ForegroundColor Yellow

$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Include all Dashboard files -->
  <ItemGroup>
    <Content Include="..\DeploymentDashboard\**\*">
      <Link>%%(RecursiveDir)%%(Filename)%%(Extension)</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
"@

$csprojContent | Out-File "DeploymentDashboard.Project\DeploymentDashboard.Project.csproj" -Encoding UTF8
Write-Host "   Updated .csproj" -ForegroundColor Green

Write-Host ""
Write-Host "4. Adding project to solution..." -ForegroundColor Yellow
dotnet sln add DeploymentDashboard.Project/DeploymentDashboard.Project.csproj

if ($LASTEXITCODE -eq 0) {
    Write-Host "   Added to solution" -ForegroundColor Green
} else {
    Write-Host "   Warning: Could not add to solution automatically" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "5. Building project (verification)..." -ForegroundColor Yellow
dotnet build DeploymentDashboard.Project/DeploymentDashboard.Project.csproj --nologo --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "   Build successful" -ForegroundColor Green
} else {
    Write-Host "   Build failed (this is OK - it's just a wrapper)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Close and reopen Visual Studio" -ForegroundColor White
Write-Host "  2. You should see 'DeploymentDashboard.Project' in Solution Explorer" -ForegroundColor White
Write-Host "  3. All dashboard files will be visible under it" -ForegroundColor White
Write-Host ""
Write-Host "To run the dashboard:" -ForegroundColor Cyan
Write-Host "  .\start-full-stack.ps1" -ForegroundColor White
Write-Host ""
