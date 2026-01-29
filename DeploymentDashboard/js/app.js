// Main Application Logic

let currentData = null;

// Initialize app
document.addEventListener('DOMContentLoaded', () => {
    initializeApp();
});

function initializeApp() {
    // Bind event listeners
    document.getElementById('refreshBtn').addEventListener('click', loadAllData);
    document.getElementById('updateCiCdBtn').addEventListener('click', openUpdateCiCdModal);
    document.querySelector('.modal-close').addEventListener('click', closeUpdateCiCdModal);
    document.querySelector('.modal-cancel').addEventListener('click', closeUpdateCiCdModal);
    document.getElementById('updateCiCdForm').addEventListener('submit', handleUpdateCiCd);
    document.getElementById('clientFilter').addEventListener('change', handleClientFilterChange);
    
    // Close modal on outside click
    document.getElementById('updateCiCdModal').addEventListener('click', (e) => {
        if (e.target.classList.contains('modal')) {
            closeUpdateCiCdModal();
        }
    });

    // Initial load
    loadAllData();
}

async function loadAllData() {
    showLoading();
    try {
        await Promise.all([
            loadCiCdVersion(),
            loadClientsStatus()
        ]);
        updateLastUpdated();
    } catch (error) {
        showError('Failed to load data: ' + error.message);
    }
}

async function loadCiCdVersion() {
    try {
        const data = await api.getCiCdVersion();
        displayCiCdVersion(data);
    } catch (error) {
        console.error('Failed to load CI/CD version:', error);
        document.getElementById('cicdVersion').textContent = 'Not Set';
    }
}

async function loadClientsStatus() {
    try {
        const data = await api.getAllClientsStatus();
        currentData = data;
        displayClientsStatus(data);
        updateStats(data);
        populateClientFilter(data.clients);
    } catch (error) {
        console.error('Failed to load clients status:', error);
        document.getElementById('clientsList').innerHTML = '<div class="error">Failed to load clients</div>';
    }
}

function displayCiCdVersion(data) {
    document.getElementById('cicdVersion').textContent = data.version || 'Not Set';
    
    if (data.updatedAt) {
        const date = new Date(data.updatedAt);
        document.getElementById('cicdUpdatedAt').textContent = `Updated: ${formatDate(date)}`;
    }
    
    if (data.updatedBy) {
        document.getElementById('cicdUpdatedBy').textContent = `By: ${data.updatedBy}`;
    }
}

function displayClientsStatus(data) {
    const container = document.getElementById('clientsList');
    
    if (!data.clients || data.clients.length === 0) {
        container.innerHTML = '<div class="empty-state">No clients found</div>';
        return;
    }

    const clientsHtml = data.clients.map(client => createClientCard(client)).join('');
    container.innerHTML = clientsHtml;
    
    // Add click handlers for history
    data.clients.forEach(client => {
        const card = document.querySelector(`[data-client-id="${client.clientId}"]`);
        if (card) {
            card.addEventListener('click', () => loadClientHistory(client.clientId));
        }
    });
}

function createClientCard(client) {
    const isUpToDate = client.minVersion === client.ciCdVersion;
    const hasVersionMismatch = client.minVersion !== client.maxVersion;
    const statusClass = isUpToDate ? 'status-success' : 'status-warning';
    
    return `
        <div class="client-card ${statusClass}" data-client-id="${client.clientId}">
            <div class="client-header">
                <h3>${client.clientName}</h3>
                <span class="client-id">${client.clientId}</span>
            </div>
            <div class="client-versions">
                <div class="version-info">
                    <span class="label">Min Version:</span>
                    <span class="version">${client.minVersion || 'N/A'}</span>
                </div>
                <div class="version-info">
                    <span class="label">Max Version:</span>
                    <span class="version">${client.maxVersion || 'N/A'}</span>
                </div>
                <div class="version-info">
                    <span class="label">CI/CD:</span>
                    <span class="version">${client.ciCdVersion || 'N/A'}</span>
                </div>
            </div>
            ${hasVersionMismatch ? '<div class="warning-badge">Version Mismatch</div>' : ''}
            ${!isUpToDate ? '<div class="warning-badge">Behind CI/CD</div>' : '<div class="success-badge">Up-to-Date</div>'}
            <div class="client-apps">
                <strong>${client.applications.length}</strong> application${client.applications.length !== 1 ? 's' : ''}
            </div>
        </div>
    `;
}

function updateStats(data) {
    const upToDateCount = data.clients.filter(c => c.minVersion === c.ciCdVersion).length;
    const outdatedCount = data.clients.length - upToDateCount;
    const totalApps = data.clients.reduce((sum, c) => sum + c.applications.length, 0);
    
    document.getElementById('totalClients').textContent = data.totalClients;
    document.getElementById('upToDateClients').textContent = upToDateCount;
    document.getElementById('outdatedClients').textContent = outdatedCount;
    document.getElementById('totalApps').textContent = totalApps;
}

async function loadClientHistory(clientId) {
    const container = document.getElementById('historyList');
    container.innerHTML = '<div class="loading">Loading history...</div>';
    
    try {
        const data = await api.getDeploymentHistory(clientId);
        displayHistory(data);
    } catch (error) {
        container.innerHTML = '<div class="error">Failed to load history</div>';
    }
}

function displayHistory(data) {
    const container = document.getElementById('historyList');
    
    if (!data.deployments || data.deployments.length === 0) {
        container.innerHTML = '<div class="empty-state">No deployment history</div>';
        return;
    }

    const historyHtml = data.deployments.map(dep => createHistoryItem(dep)).join('');
    container.innerHTML = historyHtml;
}

function createHistoryItem(deployment) {
    const statusClass = deployment.status === 0 ? 'success' : deployment.status === 1 ? 'failed' : 'in-progress';
    const statusText = deployment.status === 0 ? 'Success' : deployment.status === 1 ? 'Failed' : 'In Progress';
    const date = new Date(deployment.deploymentTime);
    
    return `
        <div class="history-item">
            <div class="history-status status-${statusClass}">${statusText}</div>
            <div class="history-info">
                <div class="history-client">${deployment.clientName}</div>
                <div class="history-app">${deployment.applicationName}</div>
                <div class="history-version">v${deployment.version}</div>
                <div class="history-time">${formatDate(date)}</div>
            </div>
        </div>
    `;
}

function populateClientFilter(clients) {
    const select = document.getElementById('clientFilter');
    const options = clients.map(c => 
        `<option value="${c.clientId}">${c.clientName}</option>`
    ).join('');
    select.innerHTML = '<option value="">All Clients</option>' + options;
}

async function handleClientFilterChange(e) {
    const clientId = e.target.value;
    
    if (!clientId) {
        document.getElementById('historyList').innerHTML = 
            '<div class="empty-state">Select a client to view history</div>';
        return;
    }
    
    await loadClientHistory(clientId);
}

// Modal functions
function openUpdateCiCdModal() {
    document.getElementById('updateCiCdModal').classList.add('show');
}

function closeUpdateCiCdModal() {
    document.getElementById('updateCiCdModal').classList.remove('show');
    document.getElementById('updateCiCdForm').reset();
}

async function handleUpdateCiCd(e) {
    e.preventDefault();
    
    const formData = new FormData(e.target);
    const version = formData.get('version');
    const updatedBy = formData.get('updatedBy');
    const notes = formData.get('notes');
    
    try {
        await api.updateCiCdVersion(version, updatedBy, notes);
        closeUpdateCiCdModal();
        await loadCiCdVersion();
        await loadClientsStatus();
        showSuccess('CI/CD version updated successfully');
    } catch (error) {
        showError('Failed to update CI/CD version: ' + error.message);
    }
}

// Helper functions
function formatDate(date) {
    return date.toLocaleString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function updateLastUpdated() {
    const now = new Date();
    document.getElementById('lastUpdated').textContent = formatDate(now);
}

function showLoading() {
    // Could add a loading overlay here
}

function showError(message) {
    alert('Error: ' + message);
}

function showSuccess(message) {
    alert('Success: ' + message);
}
