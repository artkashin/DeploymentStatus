# Cheat Sheet - English Scripts

## Start Development

```powershell
# Option 1: Interactive menu
.\START.ps1

# Option 2: Direct start
.\rebuild-and-start-en.ps1
```

---

## Run Tests

```powershell
# Terminal 1: Start API
.\rebuild-and-start-en.ps1

# Terminal 2: Run tests
.\test-api-en.ps1
```

---

## Use Table Storage

```powershell
# Terminal 1: Start Azurite
.\start-azurite-en.ps1

# Terminal 2: Start API
.\start-with-tablestorage-en.ps1

# Terminal 3: Test
.\test-api-en.ps1
```

---

## Troubleshooting

```powershell
# Check environment
.\diagnose-en.ps1

# Clean rebuild
cd DeploymentAPI
dotnet clean
dotnet build
cd ..
```

---

## Quick API Test

```powershell
$base = "http://localhost:7071/api"

# Set version
$v = @{version="1.3.0";updatedBy="Test"} | ConvertTo-Json
Invoke-RestMethod -Uri "$base/cicd/version" -Method Post -Body $v -ContentType "application/json"

# Get version
Invoke-RestMethod -Uri "$base/cicd/version"

# Register deployment
$d = @{clientId="c1";clientName="Co";applicationId="a1";applicationName="App";version="1.3.0";status=0} | ConvertTo-Json
Invoke-RestMethod -Uri "$base/deployments" -Method Post -Body $d -ContentType "application/json"

# Get status
Invoke-RestMethod -Uri "$base/clients/c1/status"
```

---

## All Scripts

| Script | Purpose |
|--------|---------|
| START.ps1 | Interactive menu |
| rebuild-and-start-en.ps1 | Build & start |
| diagnose-en.ps1 | Diagnostics |
| test-api-en.ps1 | API tests |
| test-version-logic-en.ps1 | Version tests |
| start-azurite-en.ps1 | Start Azurite |
| start-with-tablestorage-en.ps1 | Start with storage |

---

## Storage Configuration

**In-Memory (default):**
```json
{
  "StorageType": "InMemory"
}
```

**Table Storage:**
```json
{
  "StorageType": "TableStorage",
  "AzureWebJobsStorage": "UseDevelopmentStorage=true"
}
```

---

## Prerequisites

```powershell
# .NET 8
dotnet --version

# Azure Functions Core Tools
func --version

# Azurite (optional)
azurite --version
```

Install if missing:
```powershell
npm install -g azure-functions-core-tools@4 --unsafe-perm true
npm install -g azurite
```

---

**No more encoding issues - use `-en` scripts!**
