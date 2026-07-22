# GitHub Actions Integration - Quick Reference

## ?? Quick Setup (3 Steps)

1. **Create GitHub Token** ? https://github.com/settings/tokens
   - Select scope: `repo` (Full control of private repositories)
   
2. **Add to local.settings.json**:
   ```json
   "GitHub:Token": "ghp_your_token_here",
   "GitHub:Owner": "AdaptiveBS",
   "GitHub:Repository": "CIApp"
   ```

3. **Test**:
   ```powershell
   ./start-functions.ps1
   ./test-github-integration.ps1
   ```

## ?? New API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/github/actions` | Get all workflow runs |
| `GET /api/github/actions?client=ABC` | Filter runs by client |
| `GET /api/github/workflows` | List all workflows |
| `GET /api/github/repository` | Get repo information |
| `GET /api/clients/{id}/status-with-github` | Client status + GitHub data |

## ?? Usage Examples

### Get all workflow runs
```bash
curl http://localhost:7071/api/github/actions
```

### Filter by client name
```bash
curl "http://localhost:7071/api/github/actions?client=ClientABC"
```

### Get workflows list
```bash
curl http://localhost:7071/api/github/workflows
```

### Combined status (local + GitHub)
```bash
curl http://localhost:7071/api/clients/client-001/status-with-github
```

## ?? Response Data

### Workflow Run Status Values
- `queued` - Waiting to start
- `in_progress` - Currently running
- `completed` - Finished

### Workflow Run Conclusion Values
- `success` - ? Completed successfully
- `failure` - ? Failed
- `cancelled` - ?? Cancelled by user
- `skipped` - ?? Skipped

## ?? Security

- ? Token stored in `local.settings.json` (already in .gitignore)
- ? Use Azure Key Vault for production
- ? Rotate tokens every 90 days
- ?? **Never commit tokens to git**

## ?? Troubleshooting

| Error | Solution |
|-------|----------|
| 401 Unauthorized | Check token validity and `repo` scope |
| 404 Not Found | Verify Owner/Repository names are correct |
| 403 Forbidden | Check API rate limits or token permissions |

## ?? Documentation

- Full setup guide: `GITHUB-SETUP-GUIDE.md`
- API details: `DeploymentAPI/GITHUB-INTEGRATION.md`
- Test script: `test-github-integration.ps1`

## ?? Next Steps

- [ ] Set up token in Azure Key Vault (production)
- [ ] Configure Application Settings in Azure Portal
- [ ] Test with real GitHub Actions runs
- [ ] Monitor API rate limits (5000/hour)
- [ ] Consider implementing caching for frequently accessed data
