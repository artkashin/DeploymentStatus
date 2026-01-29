# Quick Reference

## Start Full Stack

```powershell
# API + Dashboard together
.\start-full-stack.ps1
```

Opens:
- API: http://localhost:7071
- Dashboard: http://localhost:8080

---

## Start Components Separately

```powershell
# Option 1: Interactive menu
.\START.ps1

# Option 2: API only
.\rebuild-and-start.ps1

# Option 3: Dashboard only
.\start-dashboard.ps1
```

## Test API

```powershell
# Terminal 1: Start API
.\rebuild-and-start.ps1

# Terminal 2: Test
.\test-api.ps1
```

## Use Table Storage

```powershell
# Terminal 1: Start Azurite
.\start-azurite.ps1

# Terminal 2: Start API with storage
.\start-with-tablestorage.ps1

# Terminal 3: Test
.\test-version-logic.ps1
```

## Troubleshoot

```powershell
# Check environment
.\diagnose.ps1

# Clean build
cd DeploymentAPI
dotnet clean
dotnet build
cd ..
```

## All Scripts (English)

- `START.ps1` - Menu
- `rebuild-and-start.ps1` - Build & start
- `run-api.ps1` - Build only
- `diagnose.ps1` - Diagnostics
- `start-functions.ps1` - Start Functions
- `test-api.ps1` - Test API
- `test-version-logic.ps1` - Test logic
- `test-storage-persistence.ps1` - Test storage
- `start-azurite.ps1` - Start Azurite
- `start-with-tablestorage.ps1` - Start with storage

## No More Issues!

? All scripts in English  
? No encoding problems  
? No "???" characters  
? Everything works!

**Start with: `.\START.ps1`**
