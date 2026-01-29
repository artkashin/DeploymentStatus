# Deployment Status API - File Structure

## Clean Setup Complete!

All Russian/translation files removed. Only English scripts and documentation remain.

---

## Scripts (All English)

### Main Scripts
- `START.ps1` - Interactive menu
- `rebuild-and-start.ps1` - Build and start API
- `run-api.ps1` - Build helper
- `diagnose.ps1` - Environment diagnostics
- `start-functions.ps1` - Start Functions only

### Test Scripts
- `test-api.ps1` - API endpoint tests
- `test-version-logic.ps1` - Version logic tests
- `test-storage-persistence.ps1` - Storage persistence tests

### Storage Scripts
- `start-azurite.ps1` - Start Azurite emulator
- `start-with-tablestorage.ps1` - Start with Table Storage

---

## Documentation

- `README.md` - Main documentation
- `QUICK-START.md` - Quick reference
- `CHEATSHEET.md` - Command reference
- `ARCHITECTURE-STORAGE.md` - Storage architecture
- `STORAGE-SETUP.md` - Storage setup guide
- `AZURE-STORAGE-INTEGRATION.md` - Table Storage integration

---

## Project Structure

```
DeplomentStatus/
??? DeploymentAPI/               # Azure Functions project
?   ??? Functions/               # API endpoints
?   ??? Models/                  # Data models
?   ??? Repositories/            # Data access
?   ?   ??? Entities/            # Table Storage entities
?   ??? Program.cs               # Entry point
?   ??? host.json                # Functions config
?   ??? local.settings.json      # Local settings
??? DeplomentStatus.AppHost/     # Aspire host (optional)
??? DeplomentStatus.ServiceDefaults/ # Shared config
??? *.ps1                        # PowerShell scripts
??? *.md                         # Documentation
```

---

## Quick Commands

```powershell
# Start development
.\START.ps1

# Or direct start
.\rebuild-and-start.ps1

# Run tests
.\test-api.ps1

# Check environment
.\diagnose.ps1
```

---

## What Was Cleaned Up

### Removed Files
? All `*-en.ps1` backup scripts  
? `create-missing-scripts.ps1`  
? `update-scripts-to-english.ps1`  
? `ENGLISH-SCRIPTS.md`  
? `ALL-SCRIPTS-ENGLISH.md`  
? `README-EN.md`  

### Kept Files
? All main `.ps1` scripts (now in English)  
? Essential documentation  
? Project source code  
? Configuration files  

---

## Ready to Use!

All files are clean and in English. No encoding issues, no duplicate files.

**Start with:** `.\START.ps1`
