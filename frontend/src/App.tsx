import { useMemo, useState, type ReactNode } from 'react'
import { useIsAuthenticated, useMsal } from '@azure/msal-react'
import { useQuery } from '@tanstack/react-query'
import { Badge, Button, Card, Input, Select, Spinner, Tooltip } from '@fluentui/react-components'
import { ArrowClockwiseRegular, OpenRegular, SignOutRegular } from '@fluentui/react-icons'
import { Link, Route, Routes, useParams } from 'react-router-dom'
import { ApiClient, duration, formatDate } from './api'
import { apiScope, authDisabled } from './auth'
import type { CustomerLatest, Deployment, Operation, RunStatus } from './types'

export function App() {
  const authenticated = useIsAuthenticated()
  const { instance } = useMsal()
  if (!authDisabled && !authenticated) {
    return <main className="sign-in"><div className="brand-mark">A</div><h1>Adaptive Deployment Status</h1><p>Secure deployment visibility for Adaptive and customer teams.</p><Button appearance="primary" size="large" onClick={() => void instance.loginRedirect({ scopes: [apiScope] })}>Sign in with Microsoft Entra ID</Button></main>
  }
  return <Shell />
}

function Shell() {
  const { instance, accounts } = useMsal()
  const api = useMemo(() => new ApiClient(instance, accounts[0]), [instance, accounts])
  const me = useQuery({ queryKey: ['me'], queryFn: api.me })
  if (me.isLoading) return <Centered><Spinner label="Checking access…" /></Centered>
  if (me.error) return <ErrorState error={me.error} onSignIn={() => void instance.loginRedirect({ scopes: [apiScope], prompt: 'select_account' })} />
  return <div className="app-shell">
    <header><Link to="/" className="brand"><span className="brand-mark small">A</span><span>Deployment Status</span></Link><div className="identity"><span>{me.data?.name}</span>{!authDisabled && <Button appearance="subtle" icon={<SignOutRegular />} onClick={() => void instance.logoutRedirect()}>Sign out</Button>}</div></header>
    <Routes>
      <Route path="/" element={me.data!.isAdaptive ? <AdaptiveDashboard api={api} /> : <CustomerLanding api={api} customerIds={me.data!.customerIds} />} />
      <Route path="/customers/:customerId" element={<CustomerPage api={api} />} />
      <Route path="/deployments/:eventId" element={<DeploymentPage api={api} isAdaptive={me.data!.isAdaptive} />} />
    </Routes>
  </div>
}

function AdaptiveDashboard({ api }: { api: ApiClient }) {
  const [filters, setFilters] = useState({ customerId: '', status: '', mode: '', workflow: '', branch: '', from: '', to: '' })
  const customers = useQuery({ queryKey: ['customers'], queryFn: api.customers })
  const search = useMemo(() => {
    const value = new URLSearchParams({ pageSize: '50' })
    Object.entries(filters).forEach(([key, item]) => { if (item) value.set(key, key === 'from' || key === 'to' ? new Date(item).toISOString() : item) })
    return value
  }, [filters])
  const deployments = useQuery({ queryKey: ['deployments', search.toString()], queryFn: () => api.deployments(search) })
  if (customers.error) return <ErrorState error={customers.error} />
  const latest = customers.data?.items ?? []
  const totals = latest.reduce((result, customer) => ({ ...result, [customer.status]: (result[customer.status] ?? 0) + 1 }), {} as Record<string, number>)
  return <main>
    <section className="hero"><div><p className="eyebrow">Adaptive operations</p><h1>Deployment health</h1><p>Authoritative results reported directly by DeployCD.</p></div><Button icon={<ArrowClockwiseRegular />} onClick={() => { void customers.refetch(); void deployments.refetch() }}>Refresh</Button></section>
    <section className="metrics" aria-label="Deployment summary">
      <Metric label="Healthy" value={totals.success ?? 0} tone="success" />
      <Metric label="Partial" value={totals.partial ?? 0} tone="warning" />
      <Metric label="Failed" value={totals.failed ?? 0} tone="danger" />
      <Metric label="Last refresh" value={customers.data ? <Time value={customers.data.generatedAt} /> : '—'} compact />
    </section>
    <section className="panel"><div className="section-title"><div><h2>Latest by customer</h2><p>Most recent execute or dry-run attempt.</p></div></div>
      {customers.isLoading ? <Spinner label="Loading customers…" /> : latest.length === 0 ? <Empty text="No authoritative deployment events have been received yet." /> : <div className="customer-grid">{latest.map(customer => <CustomerCard key={customer.customerId} customer={customer} />)}</div>}
    </section>
    <section className="panel"><div className="section-title"><div><h2>Deployment activity</h2><p>Automatically refreshes every 30 seconds.</p></div></div>
      <div className="filters">
        <Select aria-label="Customer" value={filters.customerId} onChange={(_, data) => setFilters({ ...filters, customerId: data.value })}><option value="">All customers</option>{latest.map(item => <option key={item.customerId} value={item.customerId}>{item.customerName}</option>)}</Select>
        <Select aria-label="Status" value={filters.status} onChange={(_, data) => setFilters({ ...filters, status: data.value })}><option value="">All statuses</option>{['success', 'partial', 'failed', 'cancelled', 'skipped'].map(value => <option key={value}>{value}</option>)}</Select>
        <Select aria-label="Mode" value={filters.mode} onChange={(_, data) => setFilters({ ...filters, mode: data.value })}><option value="">All modes</option><option value="execute">execute</option><option value="dryRun">dry run</option></Select>
        <Input aria-label="Workflow" placeholder="Workflow" value={filters.workflow} onChange={(_, data) => setFilters({ ...filters, workflow: data.value })} />
        <Input aria-label="Branch" placeholder="Branch" value={filters.branch} onChange={(_, data) => setFilters({ ...filters, branch: data.value })} />
        <Input aria-label="From date" type="datetime-local" value={filters.from} onChange={(_, data) => setFilters({ ...filters, from: data.value })} />
        <Input aria-label="To date" type="datetime-local" value={filters.to} onChange={(_, data) => setFilters({ ...filters, to: data.value })} />
      </div>
      {deployments.isLoading ? <Spinner label="Loading activity…" /> : deployments.error ? <ErrorState error={deployments.error} /> : <DeploymentTable items={deployments.data?.items ?? []} />}
    </section>
  </main>
}

function CustomerLanding({ api, customerIds }: { api: ApiClient; customerIds: string[] }) {
  const customers = useQuery({ queryKey: ['customers'], queryFn: api.customers })
  const history = useQuery({ queryKey: ['customer-history-all'], queryFn: () => api.deployments(new URLSearchParams({ pageSize: '50' })) })
  const singleId = customerIds.length === 1 ? customerIds[0] : ''
  const hasLatest = Boolean(singleId && customers.data?.items.some(customer => customer.customerId === singleId))
  const detail = useQuery({ queryKey: ['customer', singleId], queryFn: () => api.customer(singleId), enabled: hasLatest })
  if (customers.isLoading || (singleId && detail.isLoading)) return <Centered><Spinner label="Loading customer deployment status…" /></Centered>
  if (customers.error || detail.error) return <ErrorState error={customers.error || detail.error} />
  const latest = customers.data?.items ?? []
  const current = detail.data?.currentState ?? []
  const grouped = groupBy(current, state => state.tenantLabel || state.tenantId)
  return <main>
    <section className="hero"><div><p className="eyebrow">Customer workspace</p><h1>{detail.data?.customer.customerName || 'Deployment status'}</h1><p>Current verified application versions and your authorized deployment history.</p></div><Button icon={<ArrowClockwiseRegular />} onClick={() => { void customers.refetch(); void history.refetch(); if (hasLatest) void detail.refetch() }}>Refresh</Button></section>
    {customerIds.length > 1 && <section className="panel"><h2>Latest authorized customers</h2>{latest.length === 0 ? <Empty text="No authoritative deployment events have been received yet." /> : <div className="customer-grid">{latest.map(customer => <CustomerCard key={customer.customerId} customer={customer} />)}</div>}</section>}
    {singleId && <section className="panel"><div className="section-title"><div><h2>Current verified state</h2><p>Last successful verification from a real execute deployment.</p></div>{detail.data && <Status status={detail.data.customer.status} />}</div>{Object.keys(grouped).length === 0 ? <Empty text="No verified execute operation has been reported." /> : Object.entries(grouped).map(([tenant, states]) => <div className="tenant" key={tenant}><h3>{tenant}</h3><div className="state-grid">{states!.map(state => <div className="state" key={`${state.tenantId}-${state.applicationId}`}><strong>{state.applicationName}</strong><span>{state.version || 'Unknown version'}</span><small><Time value={state.verifiedAt} /></small></div>)}</div></div>)}</section>}
    <section className="panel"><h2>Deployment history</h2>{history.isLoading ? <Spinner label="Loading history…" /> : history.error ? <ErrorState error={history.error} /> : <DeploymentTable items={history.data?.items ?? []} />}</section>
  </main>
}

function CustomerPage({ api }: { api: ApiClient }) {
  const { customerId = '' } = useParams()
  const detail = useQuery({ queryKey: ['customer', customerId], queryFn: () => api.customer(customerId) })
  const history = useQuery({ queryKey: ['customer-history', customerId], queryFn: () => api.deployments(new URLSearchParams({ customerId, pageSize: '50' })) })
  if (detail.isLoading) return <Centered><Spinner label="Loading customer…" /></Centered>
  if (detail.error) return <ErrorState error={detail.error} />
  const grouped = groupBy(detail.data?.currentState ?? [], state => state.tenantLabel || state.tenantId)
  return <main><Link to="/" className="back-link">← Dashboard</Link><section className="hero compact"><div><p className="eyebrow">Customer status</p><h1>{detail.data!.customer.customerName}</h1><p>Last verified application versions by tenant.</p></div><Status status={detail.data!.customer.status} /></section>
    <section className="panel"><h2>Current verified state</h2>{Object.keys(grouped).length === 0 ? <Empty text="No verified execute operation has been reported." /> : Object.entries(grouped).map(([tenant, states]) => <div className="tenant" key={tenant}><h3>{tenant}</h3><div className="state-grid">{states!.map(state => <div className="state" key={`${state.tenantId}-${state.applicationId}`}><strong>{state.applicationName}</strong><span>{state.version || 'Unknown version'}</span><small><Time value={state.verifiedAt} /></small></div>)}</div></div>)}</section>
    <section className="panel"><h2>History</h2>{history.isLoading ? <Spinner /> : <DeploymentTable items={history.data?.items ?? []} />}</section>
  </main>
}

function DeploymentPage({ api, isAdaptive }: { api: ApiClient; isAdaptive: boolean }) {
  const { eventId = '' } = useParams()
  const detail = useQuery({ queryKey: ['deployment', eventId], queryFn: () => api.deployment(eventId) })
  if (detail.isLoading) return <Centered><Spinner label="Loading deployment…" /></Centered>
  if (detail.error) return <ErrorState error={detail.error} />
  const item = detail.data!
  const grouped = groupBy(item.operations ?? [], operation => operation.tenantLabel || operation.tenantId || 'Service operations')
  return <main><Link to="/" className="back-link">← Dashboard</Link><section className="hero compact"><div><p className="eyebrow">Deployment detail</p><h1>{item.customer.name}</h1><p>{item.mode === 'dryRun' ? 'Dry run' : 'Execute'} · {duration(item.startedAt, item.completedAt)} · <Time value={item.completedAt} /></p></div><Status status={item.status} /></section>
    {item.detailCompleteness === 'summary' && <section className="notice" role="status"><strong>Partial deployment data</strong><span>The runner did not produce a detailed report. This result was reconstructed from GitHub job conclusions.</span></section>}
    <section className="metrics"><Metric label="Operations" value={item.summary.total} /><Metric label="Succeeded" value={item.summary.succeeded} tone="success" /><Metric label="Failed" value={item.summary.failed} tone="danger" /><Metric label="Skipped / planned" value={item.summary.skipped + item.summary.planned} /></section>
    {isAdaptive && <section className="panel metadata"><h2>Source</h2><dl><dt>Workflow</dt><dd>{item.source.workflow || '—'}</dd><dt>Branch</dt><dd>{item.source.branch || '—'}</dd><dt>Commit</dt><dd>{item.source.commitSha || '—'}</dd><dt>Run</dt><dd>{item.source.runUrl ? <a href={item.source.runUrl} target="_blank" rel="noreferrer">#{item.source.runId} <OpenRegular /></a> : `#${item.source.runId}`}</dd></dl></section>}
    <section className="panel"><h2>App and tenant results</h2>{Object.keys(grouped).length === 0 ? <Empty text="Detailed operation results are unavailable for this deployment." /> : Object.entries(grouped).map(([tenant, operations]) => <div className="tenant" key={tenant}><h3>{tenant}</h3><OperationTable operations={operations!} adaptive={isAdaptive} /></div>)}</section>
  </main>
}

function Metric({ label, value, tone, compact }: { label: string; value: ReactNode; tone?: string; compact?: boolean }) { return <Card className={`metric ${tone || ''}`}><span>{label}</span><strong className={compact ? 'compact-value' : ''}>{value}</strong></Card> }
function CustomerCard({ customer }: { customer: CustomerLatest }) { return <Link to={`/customers/${customer.customerId}`} className="card-link"><Card className="customer-card"><div><h3>{customer.customerName}</h3><span>{customer.mode === 'dryRun' ? 'Dry run' : 'Execute'} · <Time value={customer.completedAt} /></span></div><Status status={customer.status} /><div className="mini-summary"><span>{customer.summary.succeeded} succeeded</span><span>{customer.summary.failed} failed</span></div></Card></Link> }
function Status({ status }: { status: RunStatus }) { const color = status === 'success' ? 'success' : status === 'failed' ? 'danger' : status === 'partial' ? 'warning' : 'informative'; return <Badge appearance="filled" color={color}>{status}</Badge> }
function DeploymentTable({ items }: { items: Deployment[] }) { if (!items.length) return <Empty text="No deployments match these filters." />; return <div className="table-wrap"><table><thead><tr><th>Status</th><th>Customer</th><th>Mode</th><th>Completed</th><th>Result</th><th></th></tr></thead><tbody>{items.map(item => <tr key={item.eventId}><td><Status status={item.status} /></td><td>{item.customer.name}</td><td>{item.mode === 'dryRun' ? 'Dry run' : 'Execute'}</td><td><Time value={item.completedAt} /></td><td>{item.summary.succeeded} ok · {item.summary.failed} failed</td><td><Link to={`/deployments/${encodeURIComponent(item.eventId)}`}>Details →</Link></td></tr>)}</tbody></table></div> }
function Time({ value }: { value: string }) { return <Tooltip content={new Date(value).toISOString()} relationship="label"><span>{formatDate(value)}</span></Tooltip> }
function OperationTable({ operations, adaptive }: { operations: Operation[]; adaptive: boolean }) { return <div className="table-wrap"><table><thead><tr><th>Application</th><th>Action</th><th>Version</th><th>Outcome</th><th>Message</th></tr></thead><tbody>{operations.map((item, index) => <tr key={`${item.applicationId}-${item.action}-${index}`}><td><strong>{item.applicationName}</strong>{item.publisher && <small>{item.publisher}</small>}</td><td>{item.action}</td><td>{item.previousVersion ? `${item.previousVersion} → ${item.observedVersion || item.targetVersion || 'unknown'}` : item.observedVersion || item.targetVersion || '—'}</td><td>{item.outcome}</td><td>{item.message || '—'}{adaptive && item.internalError && <details><summary>Internal error</summary><pre>{item.internalError}</pre></details>}</td></tr>)}</tbody></table></div> }
function Centered({ children }: { children: ReactNode }) { return <main className="centered">{children}</main> }
function Empty({ text }: { text: string }) { return <div className="empty"><strong>Nothing to show</strong><p>{text}</p></div> }
function ErrorState({ error, onSignIn }: { error: unknown; onSignIn?: () => void }) { const message = error instanceof Error ? error.message : 'Unexpected error.'; const expired = message.includes('session expired'); const title = expired ? 'Session expired' : message.includes('not authorized') ? 'Access not authorized' : 'Could not load deployment data'; return <main className="centered"><div className="error-state"><strong>{title}</strong><p>{message}</p>{expired && onSignIn && <Button appearance="primary" onClick={onSignIn}>Sign in again</Button>}</div></main> }
function groupBy<T>(items: T[], key: (item: T) => string): Record<string, T[]> { return items.reduce((result, item) => { const value = key(item); (result[value] ||= []).push(item); return result }, {} as Record<string, T[]>) }
