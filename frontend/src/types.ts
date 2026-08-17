export type RunStatus = 'success' | 'partial' | 'failed' | 'cancelled' | 'skipped'
export type DeploymentMode = 'execute' | 'dryRun'
export type OperationOutcome = 'success' | 'failed' | 'alreadyCurrent' | 'newerPresent' | 'excluded' | 'planned' | 'skipped'

export interface Me { name: string; isAdaptive: boolean; customerIds: string[] }
export interface Summary { total: number; succeeded: number; failed: number; skipped: number; planned: number }
export type DesiredAppHealth = 'current' | 'outdated' | 'failed' | 'unavailable' | 'planned'
export interface TenantLatest { tenantId: string; tenantLabel?: string; installedVersion?: string; installedAt?: string; desiredAppCount: number; currentAppCount: number; attentionAppCount: number; failedAppCount: number; health: 'current' | 'attention' | 'failed' | 'unknown' }
export interface CustomerLatest { customerId: string; customerName: string; eventId: string; status: RunStatus; mode: DeploymentMode; completedAt: string; summary: Summary; bcVersion?: string; packageVersion?: string; desiredAppCount: number; currentAppCount: number; attentionAppCount: number; failedAppCount: number; health: 'current' | 'attention' | 'failed' | 'unknown'; tenants: TenantLatest[] }
export interface DesiredAppState { customerId: string; tenantId: string; tenantLabel?: string; applicationId: string; applicationName: string; publisher?: string; desiredVersion?: string; installedVersion?: string; installedAt?: string; observedAt?: string; state: DesiredAppHealth; lastOutcome?: OperationOutcome; safeMessage?: string; updatedAt: string; eventId: string }
export interface Source { repository?: string; workflow?: string; runId?: number; runAttempt?: number; jobName?: string; runUrl?: string; branch?: string; commitSha?: string; actor?: string; artifactRunId?: number; runnerLabel?: string; serviceName?: string }
export interface ArtifactSourceReference { branch: string; bcVersion: string; runId?: number; runUrl?: string; artifactName?: string; packageVersion?: string; usable: boolean; conclusion?: string; warning?: string }
export interface ArtifactSource extends ArtifactSourceReference { sourceId: string; repository: string; workflow: string; runId: number; runAttempt: number; completedAt: string; artifactAvailable: boolean }
export interface Customer { id: string; name: string }
export interface Operation { scope: string; tenantId?: string; tenantLabel?: string; applicationId?: string; applicationName: string; publisher?: string; previousVersion?: string; targetVersion?: string; observedVersion?: string; action: string; outcome: OperationOutcome; durationMs?: number; message?: string; internalError?: string }
export interface TenantAppState { tenantId: string; tenantLabel?: string; applicationId?: string; applicationName: string; publisher?: string; desiredVersion?: string; installedVersion?: string; installedAt?: string; observedAt?: string; state: DesiredAppHealth; lastOutcome?: OperationOutcome; message?: string }
export interface Deployment { eventId: string; source: Source; customer: Customer; mode: DeploymentMode; status: RunStatus; startedAt: string; completedAt: string; detailCompleteness: string; summary: Summary; artifactSource?: ArtifactSourceReference; tenantAppStates?: TenantAppState[]; operations?: Operation[] }
export interface DeploymentPage { items: Deployment[]; nextCursor?: string }
export interface CustomerDetail { customer: CustomerLatest; desiredAppState: DesiredAppState[] }
