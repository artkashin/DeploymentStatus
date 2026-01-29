# Clean Project - Files That Should Exist

## PowerShell Scripts (10 total)

These are the ONLY scripts that should exist:

1. `START.ps1` - Interactive menu
2. `rebuild-and-start.ps1` - Build and start API
3. `run-api.ps1` - Build helper
4. `diagnose.ps1` - Environment diagnostics
5. `start-functions.ps1` - Start Functions
6. `test-api.ps1` - Test API endpoints
7. `test-version-logic.ps1` - Test version logic
8. `test-storage-persistence.ps1` - Test storage persistence
9. `start-azurite.ps1` - Start Azurite emulator
10. `start-with-tablestorage.ps1` - Start with Table Storage

## Documentation (7 total)

These are the ONLY markdown files that should exist:

1. `README.md` - Main project documentation
2. `QUICK-START.md` - Quick reference
3. `CHEATSHEET.md` - Command cheat sheet
4. `FILE-STRUCTURE.md` - File structure overview
5. `ARCHITECTURE-STORAGE.md` - Storage architecture
6. `STORAGE-SETUP.md` - Storage setup guide
7. `AZURE-STORAGE-INTEGRATION.md` - Table Storage integration

## Files That Should NOT Exist (Close These Tabs!)

If you see these in Visual Studio, close the tabs - they've been deleted:

- ? `rebuild-and-start-en.ps1` - DELETED
- ? `diagnose-en.ps1` - DELETED
- ? `start-functions-en.ps1` - DELETED
- ? `test-api-en.ps1` - DELETED
- ? `start-azurite-en.ps1` - DELETED
- ? `start-with-tablestorage-en.ps1` - DELETED
- ? `test-version-logic-en.ps1` - DELETED
- ? `README-EN.md` - DELETED
- ? `ENGLISH-SCRIPTS.md` - DELETED
- ? `ALL-SCRIPTS-ENGLISH.md` - DELETED
- ? `create-missing-scripts.ps1` - DELETED
- ? `update-scripts-to-english.ps1` - DELETED

## How to Close All Old Tabs in Visual Studio

1. **Close All Documents**: Window ? Close All Documents
2. **Or manually**: Close tabs for files listed above with ?

## Verify Files

Run this command to see what actually exists:

```powershell
# List all scripts
Get-ChildItem -Filter "*.ps1" | Select-Object Name

# List all markdown
Get-ChildItem -Filter "*.md" | Select-Object Name
```

## Summary

- **10 PowerShell scripts** - all in English
- **7 Documentation files** - all in English
- **NO duplicates** - all `-en` files removed
- **NO translation files** - all cleanup scripts removed

**The file system is clean - just close the old tabs in Visual Studio!**
