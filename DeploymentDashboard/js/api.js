// API Client for Deployment API

class DeploymentAPI {
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }

    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const config = {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        };

        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
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
}

// Create global API instance
const api = new DeploymentAPI(API_CONFIG.baseUrl);
