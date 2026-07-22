// API Client for Deployment API

class DeploymentAPI {
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }

    async request(endpoint, options = {}) {
        // Build the full URL and add function key if needed
        const baseUrl = `${this.baseUrl}${endpoint}`;
        const url = API_CONFIG.addKeyToUrl(baseUrl);

        const config = {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        };

        try {
            console.log(`🔄 API Request: ${endpoint}`);
            const response = await fetch(url, config);

            if (!response.ok) {
                if (response.status === 401) {
                    console.error('❌ 401 Unauthorized - Function key missing or invalid!');
                    console.error('   Add your Azure Function key to config.js');
                }
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('API request failed:', error);
            throw error;
        }
    }

    // CI/CD Version endpoints
    async getCiCdVersion() {
        return await this.request('/cicd/version');
    }

    async updateCiCdVersion(version, updatedBy, notes = '') {
        return await this.request('/cicd/version', {
            method: 'POST',
            body: JSON.stringify({ version, updatedBy, notes })
        });
    }

    // Client Status endpoints
    async getAllClientsStatus() {
        return await this.request('/clients/status');
    }

    async getClientStatus(clientId) {
        return await this.request(`/clients/${clientId}/status`);
    }

    // Deployment History endpoints
    async getDeploymentHistory(clientId, applicationId = null, limit = 50) {
        let endpoint = `/clients/${clientId}/history?limit=${limit}`;
        if (applicationId) {
            endpoint += `&applicationId=${applicationId}`;
        }
        return await this.request(endpoint);
    }

    // Register Deployment endpoint
    async registerDeployment(deployment) {
        return await this.request('/deployments', {
            method: 'POST',
            body: JSON.stringify(deployment)
        });
    }

    // Workflow Run endpoints (DEPRECATED - use sync instead)
    async getLatestUpdateCustomersStatus() {
        return await this.request('/update-all-customers/latest');
    }

    async getWorkflowRunCustomerStatus(runId) {
        return await this.request(`/workflow-runs/${runId}/customer-status`);
    }

    // Workflow Sync endpoints
    async syncWorkflowData() {
        return await this.request('/sync/workflow-data', {
            method: 'POST'
        });
    }

    async syncSpecificWorkflowRun(runId) {
        return await this.request(`/sync/workflow-data/${runId}`, {
            method: 'POST'
        });
    }
}

// Create global API instance
const api = new DeploymentAPI(API_CONFIG.baseUrl);
