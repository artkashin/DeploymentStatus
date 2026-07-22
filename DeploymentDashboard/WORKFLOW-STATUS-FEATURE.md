# Workflow Status Dashboard Feature

## Overview

The dashboard now displays real-time status from the "Update all customers" GitHub Actions workflow, showing which customers were successfully updated and which failed.

## Features

### Workflow Run Summary
- **Run Information**: Run number, ID, and status
- **Statistics**:
  - Total customers processed
  - Successfully installed count
  - Failed installations count
  - Success rate percentage

### Customer Status Cards
Each customer update displays:
- ✅ **Success** or ❌ **Failure** indicator
- Customer name
- Installation status
- Runner that executed the update
- Duration in seconds
- Start and completion timestamps
- Link to GitHub job details

### Visual Design
- **Green cards** for successful installations
- **Red cards** for failed installations
- Color-coded statistics
- Responsive grid layout

## How It Works

1. **Data Source**: Fetches from `/api/update-all-customers/latest` endpoint
2. **Auto-Load**: Loads automatically on page load
3. **Manual Refresh**: Click "Refresh" button to update data
4. **Sorting**: Displays successful installations first, then failures, alphabetically

## API Integration

### New Endpoints Used
```javascript
// Get latest workflow run status
GET /api/update-all-customers/latest

// Get specific workflow run status
GET /api/workflow-runs/{runId}/customer-status
```

### Response Format
```json
{
  "runId": 29418806053,
  "runNumber": 17,
  "workflowName": "Update all customers",
  "status": "completed",
  "overallSuccess": false,
  "totalCustomers": 8,
  "successfulInstallations": 6,
  "failedInstallations": 2,
  "timestamp": "2026-07-15T14:30:00Z",
  "customers": [
	{
	  "name": "josephs",
	  "installed": true,
	  "status": "success",
	  "runner": "CD-joshephs",
	  "durationSeconds": 70,
	  "startedAt": "2026-07-15T13:21:11Z",
	  "completedAt": "2026-07-15T13:22:21Z",
	  "url": "https://github.com/..."
	}
  ]
}
```

## Files Modified

### HTML (`index.html`)
- Added `workflow-section` for workflow run display
- Added summary panel and customer cards container

### JavaScript (`js/app.js`)
- `loadWorkflowRunStatus()` - Fetches latest workflow data
- `displayWorkflowRunStatus()` - Renders summary statistics
- `createWorkflowCustomerCard()` - Creates individual customer cards
- `formatTime()` - Helper for time formatting

### JavaScript (`js/api.js`)
- `getLatestUpdateCustomersStatus()` - API method for latest run
- `getWorkflowRunCustomerStatus(runId)` - API method for specific run

### CSS (`css/style.css`)
- `.workflow-section` - Main container styles
- `.workflow-summary-grid` - Summary layout
- `.workflow-customer-card` - Customer card styles
- Success/failure color variations
- Responsive breakpoints

## Usage Example

### Basic Implementation
```javascript
// Load on page init
async function init() {
	await loadWorkflowRunStatus();
}

// Manual refresh
document.getElementById('refreshWorkflowBtn').addEventListener('click', async () => {
	await loadWorkflowRunStatus();
});
```

### Custom Filtering
```javascript
// Show only failed customers
const failedCustomers = data.customers.filter(c => !c.installed);

// Show only successful customers
const successfulCustomers = data.customers.filter(c => c.installed);
```

## UI Components

### Status Summary Panel
```
┌─────────────────────────────────────────────────────────┐
│ Update all customers                        Run #17     │
│ Run ID: 29418806053  Status: completed                 │
│                                                         │
│ [8]            [6]           [2]          [75%]        │
│ Total       ✓ Installed    ✗ Failed   Success Rate    │
└─────────────────────────────────────────────────────────┘
```

### Customer Card (Success)
```
┌──────────────────────────────────────┐
│ ✓ josephs                   success  │ <-- Green border
├──────────────────────────────────────┤
│ Runner: CD-joshephs   Duration: 70s │
│ Started: 13:21:11     Completed:    │
│                       13:22:21       │
├──────────────────────────────────────┤
│ View on GitHub →                     │
└──────────────────────────────────────┘
```

### Customer Card (Failed)
```
┌──────────────────────────────────────┐
│ ✗ bergaro                   failure  │ <-- Red border
├──────────────────────────────────────┤
│ Runner: BCAPPDEVOPSVM Duration: 28s  │
│ Started: 13:21:12     Completed:    │
│                       13:21:40       │
├──────────────────────────────────────┤
│ View on GitHub →                     │
└──────────────────────────────────────┘
```

## Responsive Design

- **Desktop**: 3-4 cards per row
- **Tablet**: 2 cards per row
- **Mobile**: 1 card per row (stacked)

Statistics grid adapts:
- **Desktop**: 4 columns (all stats in one row)
- **Mobile**: 2x2 grid

## Future Enhancements

1. **Historical Runs**: Show dropdown to select previous runs
2. **Charts**: Add success rate trend chart over time
3. **Notifications**: Browser notifications for failed installations
4. **Filters**: Filter by runner, status, or duration
5. **Export**: Export customer status to CSV/Excel
6. **Real-time**: WebSocket updates for live status
7. **Comparison**: Compare multiple runs side-by-side

## Testing

### Local Development
1. Start the API:
   ```bash
   cd DeploymentAPI
   func start
   ```

2. Open the dashboard:
   ```bash
   cd DeploymentDashboard
   # Open index.html in browser or use a local server
   python -m http.server 8000
   ```

3. Navigate to `http://localhost:8000`

### Verify the Feature
- Check the "Latest Update All Customers Run" section appears
- Verify summary statistics are correct
- Confirm customer cards load and display properly
- Test the refresh button
- Click "View on GitHub" links to verify they open correctly

## Troubleshooting

### "Failed to load workflow status"
- Ensure DeploymentAPI is running
- Check API URL in `js/config.js`
- Verify GitHub authentication is configured
- Check browser console for errors

### No customer data shown
- Verify at least one "Update all customers" workflow has run
- Check API response in Network tab
- Ensure workflow jobs follow the naming pattern: "Update {customer} / Update {customer}"

### Cards not displaying correctly
- Clear browser cache
- Check CSS is loading properly
- Verify card HTML structure in browser DevTools

## Performance Considerations

- Data is cached on page load
- Manual refresh required for updates
- Consider adding auto-refresh every 30-60 seconds for monitoring dashboards
- Large customer lists (50+) may benefit from pagination

## Browser Compatibility

- ✅ Chrome 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Edge 90+
