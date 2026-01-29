// API Configuration
const API_CONFIG = {
    // Change this to your Azure Functions URL in production
    baseUrl: 'http://localhost:7071/api',
    
    // For production, use environment-specific URLs
    // baseUrl: window.location.hostname === 'localhost' 
    //     ? 'http://localhost:7071/api'
    //     : 'https://your-function-app.azurewebsites.net/api'
};
