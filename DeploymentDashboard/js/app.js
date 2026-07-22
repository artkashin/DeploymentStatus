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

    // Sync workflow data button (if it exists)
    const syncBtn = document.getElementById('syncWorkflowBtn');
    if (syncBtn) {
        syncBtn.addEventListener('click', syncWorkflowData);
    }

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
    const statusClass = client.status === 'Active' ? 'status-active' : 'status-inactive';

    // Count applications by status
    const upToDateApps = client.applications.filter(app => app.isUpToDate).length;
    const behindApps = client.applications.filter(app => app.isBehind).length;
    const failedApps = client.applications.filter(app => app.status === 'Failed').length;

    return `
        <div class="client-card ${statusClass}" data-client-id="${client.clientId}">
            <div class="client-header">
                <h3>${client.clientName}</h3>
                <span class="client-id">${client.clientId}</span>
                ${client.status !== 'Active' ? '<span class="inactive-badge">Inactive</span>' : ''}
            </div>
            <div class="client-meta">
                <span>Created: ${client.createdAt ? formatDate(new Date(client.createdAt)) : 'Unknown'}</span>
                <span class="app-count">${client.applications.length} application${client.applications.length !== 1 ? 's' : ''}</span>
            </div>
            <div class="apps-list">
                ${client.applications.map(app => createApplicationCard(app, client.ciCdVersion)).join('')}
            </div>
            <div class="client-summary">
                ${upToDateApps > 0 ? `<span class="badge badge-success">${upToDateApps} up-to-date</span>` : ''}
                ${behindApps > 0 ? `<span class="badge badge-warning">${behindApps} behind</span>` : ''}
                ${failedApps > 0 ? `<span class="badge badge-error">${failedApps} failed</span>` : ''}
            </div>
        </div>
    `;
}

function createApplicationCard(app, ciCdVersion) {
    const statusClass = app.status === 'Success' ? 'app-success' : 'app-failed';

    return `
        <div class="app-item ${statusClass}">
            <div class="app-header">
                <span class="app-name">${app.applicationName}</span>
                <span class="app-status ${app.status.toLowerCase()}">${app.status}</span>
            </div>
            <div class="version-info">
                <div class="version-row">
                    <span class="version-label">Installed:</span>
                    <span class="version-value">${app.installedVersion || 'Not installed'}</span>
                    ${app.installedAt ? `<span class="version-date">${formatDate(new Date(app.installedAt))}</span>` : ''}
                </div>
                <div class="version-row">
                    <span class="version-label">Latest:</span>
                    <span class="version-value">${app.latestVersion || 'N/A'}</span>
                </div>
                <div class="version-row">
                    <span class="version-label">Target:</span>
                    <span class="version-value">${app.ciCdTargetVersion || ciCdVersion || 'N/A'}</span>
                </div>
            </div>
            <div class="status-badges">
                ${app.isUpToDate ? '<span class="badge badge-success">✓ Up-to-date</span>' : ''}
                ${app.isBehind ? '<span class="badge badge-warning">⚠ Behind</span>' : ''}
                ${app.status === 'Failed' ? '<span class="badge badge-error">✗ Failed</span>' : ''}
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

// Workflow Sync Functions
async function syncWorkflowData() {
    const syncBtn = document.getElementById('syncWorkflowBtn');
    const originalText = syncBtn ? syncBtn.textContent : '';

    try {
        if (syncBtn) {
            syncBtn.disabled = true;
            syncBtn.textContent = 'Syncing...';
        }

        showLoading();

        const result = await api.syncWorkflowData();

        if (result.success) {
            showSuccess(`Synced ${result.customersProcessed} customers and ${result.deploymentsRecorded} deployments from Run #${result.runNumber}`);
            // Reload all data to show synced results
            await loadAllData();
        } else {
            const errorMessage = Array.isArray(result.errors) && result.errors.length > 0
                ? result.errors.join(', ')
                : 'Unknown error';
            showError(`Sync completed with errors: ${errorMessage}`);
        }
    } catch (error) {
        console.error('Failed to sync workflow data:', error);
        showError('Failed to sync workflow data: ' + error.message);
    } finally {
        if (syncBtn) {
            syncBtn.disabled = false;
            syncBtn.textContent = originalText;
        }
    }
}

// Workflow Run Status Functions (DEPRECATED - kept for reference)
async function loadWorkflowRunStatus() {
    try {
        const data = await api.getLatestUpdateCustomersStatus();
        displayWorkflowRunStatus(data);
    } catch (error) {
        console.error('Failed to load workflow run status:', error);
        document.getElementById('workflowRunSummary').innerHTML = '<div class="error">Failed to load workflow status</div>';
    }
}

function displayWorkflowRunStatus(data) {
    // Display summary
    const summaryHtml = `
        <div class="workflow-summary-grid">
            <div class="workflow-info">
                <div class="workflow-title">
                    <h3>${data.workflowName}</h3>
                    <span class="run-badge">Run #${data.runNumber}</span>
                </div>
                <div class="workflow-meta">
                    <span>Run ID: ${data.runId}</span>
                    <span>Status: <span class="status-badge status-${data.status}">${data.status}</span></span>
                    <span>Retrieved: ${formatDate(new Date(data.timestamp))}</span>
                </div>
            </div>
            <div class="workflow-stats-grid">
                <div class="workflow-stat">
                    <div class="stat-value">${data.totalCustomers}</div>
                    <div class="stat-label">Total Customers</div>
                </div>
                <div class="workflow-stat stat-success">
                    <div class="stat-value">${data.successfulInstallations}</div>
                    <div class="stat-label">✓ Installed</div>
                </div>
                <div class="workflow-stat stat-danger">
                    <div class="stat-value">${data.failedInstallations}</div>
                    <div class="stat-label">✗ Failed</div>
                </div>
                <div class="workflow-stat ${data.overallSuccess ? 'stat-success' : 'stat-warning'}">
                    <div class="stat-value">${Math.round((data.successfulInstallations / data.totalCustomers) * 100)}%</div>
                    <div class="stat-label">Success Rate</div>
                </div>
            </div>
        </div>
    `;
    document.getElementById('workflowRunSummary').innerHTML = summaryHtml;

    // Display customer status cards
    if (!data.customers || data.customers.length === 0) {
        document.getElementById('workflowCustomersList').innerHTML = '<div class="empty-state">No customer data found</div>';
        return;
    }

    // Sort: successful first, then by name
    const sortedCustomers = [...data.customers].sort((a, b) => {
        if (a.installed !== b.installed) {
            return b.installed - a.installed; // installed (true) first
        }
        return a.name.localeCompare(b.name);
    });

    const customersHtml = sortedCustomers.map(customer => createWorkflowCustomerCard(customer)).join('');
    document.getElementById('workflowCustomersList').innerHTML = customersHtml;
}

function createWorkflowCustomerCard(customer) {
    const statusClass = customer.installed ? 'success' : 'failed';
    const statusIcon = customer.installed ? '✓' : '✗';
    const statusText = customer.installed ? 'Installed' : customer.status || 'Failed';

    return `
        <div class="workflow-customer-card ${statusClass}">
            <div class="customer-header">
                <div class="customer-name">
                    <span class="status-icon">${statusIcon}</span>
                    <strong>${customer.name}</strong>
                </div>
                <span class="status-badge status-${customer.status}">${statusText}</span>
            </div>
            <div class="customer-details">
                <div class="detail-item">
                    <span class="detail-label">Runner:</span>
                    <span class="detail-value">${customer.runner || 'N/A'}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Duration:</span>
                    <span class="detail-value">${customer.durationSeconds}s</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Started:</span>
                    <span class="detail-value">${customer.startedAt ? formatTime(new Date(customer.startedAt)) : 'N/A'}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Completed:</span>
                    <span class="detail-value">${customer.completedAt ? formatTime(new Date(customer.completedAt)) : 'N/A'}</span>
                </div>
            </div>
            ${customer.url ? `
                <div class="customer-actions">
                    <a href="${customer.url}" target="_blank" class="btn-link">View on GitHub →</a>
                </div>
            ` : ''}
        </div>
    `;
}

function formatTime(date) {
    return date.toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });
}
