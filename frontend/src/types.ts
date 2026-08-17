export type RunStatus = 'success' | 'partial' | 'failed' | 'cancelled' | 'skipped'
export type DeploymentMode = 'execute' | 'dryRun'
export type OperationOutcome = 'success' | 'failed' | 'alreadyCurrent' | 'newerPresent' | 'excluded' | 'planned' | 'skipped'

export interface Me { name: string; isAdaptive: boolean; customerIds: string[] }
export interface Summary { total: number; succeeded: number; failed: number; skipped: number; planned: number }
export interface CustomerLatest { customerId: string; customerName: string; eventId: string; status: RunStatus; mode: DeploymentMode; completedAt: string; summary: Summary }
export interface CurrentState { customerId: string; tenantId: string; tenantLabel?: string; applicationId: string; applicationName: string; version?: string; lastOutcome: OperationOutcome; verifiedAt: string; eventId: string }
export interface Source { repository?: string; workflow?: string; runId?: number; runAttempt?: number; jobName?: string; runUrl?: string; branch?: string; commitSha?: string; actor?: string; artifactRunId?: number; runnerLabel?: string; serviceName?: string }
export interface Customer { id: string; name: string }
export interface Operation { scope: string; tenantId?: string; tenantLabel?: string; applicationId?: string; applicationName: string; publisher?: string; previousVersion?: string; targetVersion?: string; observedVersion?: string; action: string; outcome: OperationOutcome; durationMs?: number; message?: string; internalError?: string }
export interface Deployment { eventId: string; source: Source; customer: Customer; mode: DeploymentMode; status: RunStatus; startedAt: string; completedAt: string; detailCompleteness: string; summary: Summary; operations?: Operation[] }
export interface DeploymentPage { items: Deployment[]; nextCursor?: string }
export interface CustomerDetail { customer: CustomerLatest; currentState: CurrentState[] }
