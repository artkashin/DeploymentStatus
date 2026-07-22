# Workflow Run #17 - Customer Installation Status

**Run ID:** 29418806053  
**Workflow:** Update all customers  
**Date:** July 15, 2026 at 13:21 UTC  
**Status:** Completed (with failures)  
**Duration:** ~57 seconds

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Customers** | 8 |
| **✓ Successfully Installed** | 6 |
| **✗ Failed Installations** | 2 |
| **Overall Success** | ❌ No (2 failures) |

---

## Successful Installations ✓

| # | Customer | Runner | Duration | Started | Completed |
|---|----------|--------|----------|---------|-----------|
| 1 | **josephs** | CD-joshephs | 70s | 13:21:11Z | 13:22:21Z |
| 2 | **baileybox** | CD-baileys | 45s | 13:21:11Z | 13:21:56Z |
| 3 | **jrdunn** | CD-jrdunn | 50s | 13:21:11Z | 13:22:01Z |
| 4 | **eiseman** | CD-eiseman | 46s | 13:21:11Z | 13:21:57Z |
| 5 | **dw** | CD-dw | 52s | 13:21:11Z | 13:22:03Z |
| 6 | **lbgreen** | CD-lbgreen | 61s | 13:21:11Z | 13:22:12Z |

**Average Duration:** 54 seconds

---

## Failed Installations ✗

| # | Customer | Runner | Duration | Started | Completed | Status |
|---|----------|--------|----------|---------|-----------|--------|
| 1 | **bergaro** | BCAPPDEVOPSVM | 28s | 13:21:12Z | 13:21:40Z | failure |
| 2 | **orrs** | CD-orrs | 80s | 13:21:13Z | 13:22:33Z | failure |

---

## Detailed Customer Status

### ✓ josephs
- **Status:** ✅ Installed Successfully
- **Runner:** CD-joshephs
- **Duration:** 70 seconds
- **Started:** 2026-07-15T13:21:11Z
- **Completed:** 2026-07-15T13:22:21Z
- **URL:** [View Job](https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363703645)

### ✓ baileybox
- **Status:** ✅ Installed Successfully
- **Runner:** CD-baileys
- **Duration:** 45 seconds
- **Started:** 2026-07-15T13:21:11Z
- **Completed:** 2026-07-15T13:21:56Z
- **URL:** [View Job](https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363704073)

### ✓ jrdunn
- **Status:** ✅ Installed Successfully
- **Runner:** CD-jrdunn
- **Duration:** 50 seconds
- **Started:** 2026-07-15T13:21:11Z
- **Completed:** 2026-07-15T13:22:01Z
- **URL:** [View Job](https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363704508)

### ✓ eiseman
- **Status:** ✅ Installed Successfully
- **Runner:** CD-eiseman
- **Duration:** 46 seconds
- **Started:** 2026-07-15T13:21:11Z
- **Completed:** 2026-07-15T13:21:57Z
- **URL:** [View Job](https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363705215)

### ✗ bergaro
- **Status:** ❌ Installation Failed
- **Runner:** BCAPPDEVOPSVM
- **Duration:** 28 seconds
- **Started:** 2026-07-15T13:21:12Z
- **Completed:** 2026-07-15T13:21:40Z
- **URL:** [View Job](https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363707790)
- **Note:** Failure occurred during "Execute update" step

### ✓ dw
- **Status:** ✅ Installed Successfully
- **Runner:** CD-dw
- **Duration:** 52 seconds
- **Started:** 2026-07-15T13:21:11Z
- **Completed:** 2026-07-15T13:22:03Z
- **URL:** [View Job](https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363709343)

### ✓ lbgreen
- **Status:** ✅ Installed Successfully
- **Runner:** CD-lbgreen
- **Duration:** 61 seconds
- **Started:** 2026-07-15T13:21:11Z
- **Completed:** 2026-07-15T13:22:12Z
- **URL:** [View Job](https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363710736)

### ✗ orrs
- **Status:** ❌ Installation Failed
- **Runner:** CD-orrs
- **Duration:** 80 seconds
- **Started:** 2026-07-15T13:21:13Z
- **Completed:** 2026-07-15T13:22:33Z
- **URL:** [View Job](https://github.com/AdaptiveBS/CIApp/actions/runs/29418806053/job/87363710823)
- **Note:** Failure occurred during "Execute update" step (longest running job)

---

## Analysis

### Success Rate
- **75%** of customers (6 out of 8) were successfully updated

### Performance Insights
- **Fastest installation:** baileybox (45 seconds)
- **Slowest installation:** josephs (70 seconds) - still successful
- **Longest failed job:** orrs (80 seconds) - indicates it ran through most of the process before failing

### Failure Patterns
1. **bergaro** - Failed quickly (28s), suggesting an early-stage failure
2. **orrs** - Failed after 80s, suggesting a late-stage failure during execution

### Recommendations
1. Investigate **bergaro** installation environment (runner: BCAPPDEVOPSVM)
2. Review **orrs** execution logs to identify the specific failure point
3. Both failures occurred in the "Execute update" step - check update scripts/commands
4. Consider retry mechanisms for failed installations

---

## API Access

You can retrieve this data programmatically using:

```bash
# Get this specific run's status
curl http://localhost:7071/api/workflow-runs/29418806053/customer-status

# Or get the latest "Update all customers" run
curl http://localhost:7071/api/update-all-customers/latest
```

See [WORKFLOW-CUSTOMER-STATUS-API.md](./WORKFLOW-CUSTOMER-STATUS-API.md) for complete API documentation.

---

## Next Steps

1. ✅ Review failure logs for bergaro and orrs
2. ⏳ Retry failed installations
3. ⏳ Monitor next workflow run for similar patterns
4. ⏳ Document common failure scenarios and resolutions
