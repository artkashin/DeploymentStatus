// API Configuration
const API_CONFIG = {
    // FORCE PRODUCTION API - Set to false to use local API on localhost
    useProd: true,

    // 🔑 AZURE FUNCTION KEY - Required for production API
    // Get this from Azure Portal: Function App → Functions → Function Keys → "default"
    // ⚠️ Leave empty for local development (no key needed for localhost:7071)
    functionKey: '', // Add your Azure Function master/host key here

    // Production API URL
    productionUrl: 'https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api',

    // Local development API URL
    localUrl: 'http://localhost:7071/api',

    // Active API URL (determined by useProd flag)
    get baseUrl() {
        if (this.useProd) {
            console.log('🌐 Using PRODUCTION API:', this.productionUrl);
            if (!this.functionKey) {
                console.warn('⚠️ WARNING: No function key set! API calls will fail with 401 Unauthorized.');
                console.warn('   Get key from Azure Portal → Function App → Function Keys');
            }
            return this.productionUrl;
        }
        const isLocal = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';
        const url = isLocal ? this.localUrl : this.productionUrl;
        console.log(`🌐 Using ${isLocal ? 'LOCAL' : 'PRODUCTION'} API:`, url);
        return url;
    },

    // Helper to add function key to URL if needed
    addKeyToUrl(url) {
        // Local API doesn't need a key
        if (url.startsWith(this.localUrl)) {
            return url;
        }

        // Production API needs function key
        if (!this.functionKey) {
            console.error('❌ Function key required for production API!');
            return url;
        }

        const separator = url.includes('?') ? '&' : '?';
        return `${url}${separator}code=${this.functionKey}`;
    }
};

