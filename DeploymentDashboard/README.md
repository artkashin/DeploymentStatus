# Deployment Dashboard

Azure Static Web App dashboard for visualizing Business Central deployment status.

## Features

- Real-time deployment status overview
- Client-by-client deployment tracking
- CI/CD version monitoring
- Deployment history viewer
- Version comparison and alerts

## Local Development

```bash
# Install dependencies (optional - uses CDN)
# No build step required - pure HTML/CSS/JS

# Run local server
python -m http.server 8080
# Or use Live Server in VS Code
```

## Configuration

Set API endpoint in `js/config.js`:
```javascript
const API_BASE_URL = 'http://localhost:7071/api';
```

## Project Structure

```
DeploymentDashboard/
??? index.html              # Main dashboard
??? css/
?   ??? style.css           # Styling
??? js/
?   ??? config.js           # API configuration
?   ??? api.js              # API client
?   ??? app.js              # Main application logic
??? staticwebapp.config.json # Azure SWA config
```

## Deployment to Azure

```bash
# Using Azure Static Web Apps CLI
swa deploy --app-location DeploymentDashboard --api-location DeploymentAPI
```
