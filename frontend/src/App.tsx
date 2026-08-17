import { useMemo, type ReactNode } from 'react'
import { useIsAuthenticated, useMsal } from '@azure/msal-react'
import { useQuery } from '@tanstack/react-query'
import { Badge, Button, Card, Input, Spinner, Tooltip } from '@fluentui/react-components'
import { AlertRegular, ArrowClockwiseRegular, ArrowDownloadRegular, CheckmarkCircleRegular, OpenRegular, SearchRegular, SignOutRegular, WarningRegular } from '@fluentui/react-icons'
import { Link, Route, Routes, useParams } from 'react-router-dom'
import { ApiClient, duration, formatDate } from './api'
import { apiScope, authDisabled } from './auth'
import type { ArtifactSource, CustomerLatest, Deployment, Operation, RunStatus } from './types'

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
  const recoverSession = () => {
    instance.clearCache()
    void instance.loginRedirect({ scopes: [apiScope], prompt: 'select_account' })
  }
  if (me.isLoading) return <Centered><Spinner label="Checking access…" /></Centered>
  if (me.error) return <ErrorState error={me.error} onSignIn={recoverSession} />
  return <div className="app-shell">
    <header className="system-bar"><Link to="/" className="system-title"><h1>System Info Dashboard</h1></Link><div className="system-actions"><Input className="system-search" contentBefore={<SearchRegular />} placeholder="Search systems..." aria-label="Search systems" /><Button appearance="subtle" icon={<AlertRegular />} aria-label="Notifications" /><span className="profile-mark" title={me.data?.name}>{me.data?.name?.slice(0, 1).toUpperCase() || 'A'}</span>{!authDisabled && <Button appearance="subtle" icon={<SignOutRegular />} onClick={() => void instance.logoutRedirect()} aria-label="Sign out" />}</div></header>
    <Routes>
      <Route path="/" element={me.data!.isAdaptive ? <AdaptiveDashboard api={api} /> : <CustomerLanding api={api} customerIds={me.data!.customerIds} />} />
      <Route path="/customers/:customerId" element={<CustomerPage api={api} />} />
      <Route path="/deployments/:eventId" element={<DeploymentPage api={api} isAdaptive={me.data!.isAdaptive} />} />
    </Routes>
  </div>
}

function AdaptiveDashboard({ api }: { api: ApiClient }) {
  const customers = useQuery({ queryKey: ['customers'], queryFn: api.customers, refetchInterval: 30_000 })
  const artifactSources = useQuery({ queryKey: ['artifact-sources'], queryFn: api.artifactSources, refetchInterval: 30_000 })
  if (customers.error) return <ErrorState error={customers.error} />
  const latest = customers.data?.items ?? []
  return <main className="dashboard-main">
    <section className="baseline-strip" aria-label="AJEApps artifact baselines">
      <div className="baseline-title"><AlertRegular /><strong>Artifact baselines</strong></div>{artifactSources.isLoading ? <Spinner size="tiny" /> : artifactSources.error ? <span>Sources unavailable</span> : (artifactSources.data?.items ?? []).map(item => <Baseline key={item.sourceId} item={item} />)}
    </section>
    <section className="fleet panel"><div className="fleet-toolbar"><strong>Fleet Status Overview</strong><div><Tooltip content="Download a shareable PNG snapshot" relationship="label"><Button appearance="subtle" icon={<ArrowDownloadRegular />} aria-label="Download fleet snapshot as PNG" disabled={customers.isLoading} onClick={() => void downloadFleetSnapshot(latest, artifactSources.data?.items ?? [])} /></Tooltip></div></div>
      {customers.isLoading ? <Spinner label="Loading fleet…" /> : <FleetTable items={latest} sources={artifactSources.data?.items ?? []} />}
    </section>
    <footer className="dashboard-footer"><span>Showing {latest.length} customer{latest.length === 1 ? '' : 's'}</span><span>Last refreshed: {customers.data ? <Time value={customers.data.generatedAt} /> : '—'} <Button appearance="subtle" size="small" icon={<ArrowClockwiseRegular />} aria-label="Refresh dashboard" onClick={() => { void customers.refetch(); void artifactSources.refetch() }} /></span></footer>
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
  const grouped = groupBy(detail.data?.desiredAppState ?? [], state => state.tenantLabel || state.tenantId)
  return <main>
    <section className="hero"><div><p className="eyebrow">Customer workspace</p><h1>{detail.data?.customer.customerName || 'Deployment status'}</h1><p>Desired applications and observed installed versions by tenant.</p></div><Button icon={<ArrowClockwiseRegular />} onClick={() => { void customers.refetch(); void history.refetch(); if (hasLatest) void detail.refetch() }}>Refresh</Button></section>
    {customerIds.length > 1 && <section className="panel"><h2>Latest authorized customers</h2>{latest.length === 0 ? <Empty text="No authoritative deployment events have been received yet." /> : <div className="customer-grid">{latest.map(customer => <CustomerCard key={customer.customerId} customer={customer} />)}</div>}</section>}
    {singleId && <section className="panel"><div className="section-title"><div><h2>Desired apps</h2><p>Current desired version compared with the last observed installed version.</p></div>{detail.data && <Health status={detail.data.customer.health} />}</div><DesiredAppTable grouped={grouped} /></section>}
    <section className="panel"><h2>Deployment history</h2>{history.isLoading ? <Spinner label="Loading history…" /> : history.error ? <ErrorState error={history.error} /> : <DeploymentTable items={history.data?.items ?? []} />}</section>
  </main>
}

function CustomerPage({ api }: { api: ApiClient }) {
  const { customerId = '' } = useParams()
  const detail = useQuery({ queryKey: ['customer', customerId], queryFn: () => api.customer(customerId) })
  const history = useQuery({ queryKey: ['customer-history', customerId], queryFn: () => api.deployments(new URLSearchParams({ customerId, pageSize: '50' })) })
  if (detail.isLoading) return <Centered><Spinner label="Loading customer…" /></Centered>
  if (detail.error) return <ErrorState error={detail.error} />
  const grouped = groupBy(detail.data?.desiredAppState ?? [], state => state.tenantLabel || state.tenantId)
  return <main><Link to="/" className="back-link">← Dashboard</Link><section className="hero compact"><div><p className="eyebrow">Customer status</p><h1>{detail.data!.customer.customerName}</h1><p>Desired applications and observed installed versions by tenant.</p></div><Health status={detail.data!.customer.health} /></section>
    <section className="panel"><h2>Desired apps</h2><DesiredAppTable grouped={grouped} /></section>
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
    {item.artifactSource && <section className="panel metadata"><h2>AJEApps artifact</h2><dl><dt>Business Central</dt><dd>BC{item.artifactSource.bcVersion}</dd><dt>Package version</dt><dd>{item.artifactSource.packageVersion || 'Not reported'}</dd><dt>Availability</dt><dd>{item.artifactSource.usable ? 'Usable' : 'Unavailable'}{item.artifactSource.warning ? ` — ${item.artifactSource.warning}` : ''}</dd>{isAdaptive && <><dt>Artifact</dt><dd>{item.artifactSource.artifactName || '—'}</dd><dt>Run</dt><dd>{item.artifactSource.runUrl ? <a href={item.artifactSource.runUrl} target="_blank" rel="noreferrer">#{item.artifactSource.runId} <OpenRegular /></a> : `#${item.artifactSource.runId || '—'}`}</dd><dt>Conclusion</dt><dd>{item.artifactSource.conclusion || '—'}</dd></>}</dl></section>}
    {isAdaptive && <section className="panel metadata"><h2>Deployment source</h2><dl><dt>Workflow</dt><dd>{item.source.workflow || '—'}</dd><dt>Branch</dt><dd>{item.source.branch || '—'}</dd><dt>Commit</dt><dd>{item.source.commitSha || '—'}</dd><dt>Run</dt><dd>{item.source.runUrl ? <a href={item.source.runUrl} target="_blank" rel="noreferrer">#{item.source.runId} <OpenRegular /></a> : `#${item.source.runId}`}</dd></dl></section>}
    <section className="panel"><h2>App and tenant results</h2>{Object.keys(grouped).length === 0 ? <Empty text="Detailed operation results are unavailable for this deployment." /> : Object.entries(grouped).map(([tenant, operations]) => <div className="tenant" key={tenant}><h3>{tenant}</h3><OperationTable operations={operations!} adaptive={isAdaptive} /></div>)}</section>
  </main>
}

function Metric({ label, value, tone, compact }: { label: string; value: ReactNode; tone?: string; compact?: boolean }) { return <Card className={`metric ${tone || ''}`}><span>{label}</span><strong className={compact ? 'compact-value' : ''}>{value}</strong></Card> }
function CustomerCard({ customer }: { customer: CustomerLatest }) { return <Link to={`/customers/${customer.customerId}`} className="card-link"><Card className="customer-card"><div><h3>{customer.customerName}</h3><span>{customer.mode === 'dryRun' ? 'Dry run' : 'Execute'} · <Time value={customer.completedAt} /></span></div><Status status={customer.status} /><div className="mini-summary"><span>{customer.summary.succeeded} succeeded</span><span>{customer.summary.failed} failed</span></div></Card></Link> }
function Status({ status }: { status: RunStatus }) { const color = status === 'success' ? 'success' : status === 'failed' ? 'danger' : status === 'partial' ? 'warning' : 'informative'; return <Badge appearance="filled" color={color}>{status}</Badge> }
function Health({ status }: { status: CustomerLatest['health'] }) { const color = status === 'current' ? 'success' : status === 'failed' ? 'danger' : status === 'attention' ? 'warning' : 'informative'; const label = status === 'current' ? 'All desired apps current' : status === 'failed' ? 'Update failed' : status === 'attention' ? 'Needs attention' : 'Not reported'; return <Badge appearance="filled" color={color}>{label}</Badge> }
function Baseline({ item }: { item: ArtifactSource }) { const label = item.usable ? item.warning ? 'Usable with warning' : 'Usable' : 'Unavailable'; return <div className="baseline"><span>BC{item.bcVersion} · {item.branch}</span><strong>{item.packageVersion || 'No package'}</strong><Badge appearance="filled" color={item.usable && !item.warning ? 'success' : item.usable ? 'warning' : 'danger'}>{label}</Badge>{item.runUrl && <a href={item.runUrl} target="_blank" rel="noreferrer">View build <OpenRegular /></a>}</div> }
function downloadFleetSnapshot(items: CustomerLatest[], sources: ArtifactSource[]) {
  const rows = items.flatMap(customer => [
    { kind: 'customer' as const, customer: customer.customerName, bc: customer.bcVersion ? `BC${customer.bcVersion}` : 'Not reported', version: '', date: '', status: '' },
    ...customer.tenants.map(tenant => ({ kind: 'tenant' as const, customer: `↳  ${tenant.tenantLabel || tenant.tenantId}`, bc: '', version: tenant.installedVersion || 'Unavailable', date: tenant.installedAt ? formatDate(tenant.installedAt) : '—', status: tenant.health }))
  ])
  const width = 1440
  const rowHeight = 42
  const height = 226 + rows.length * rowHeight + 48
  const status = (health: CustomerLatest['health']) => health === 'current' ? ['Current', '#067647', '#dcfaee'] : health === 'failed' ? ['Update failed', '#b42318', '#ffe4e8'] : health === 'attention' ? ['Needs attention', '#9a6700', '#fff3cd'] : ['Not reported', '#475467', '#f2f4f7']
  const canvas = document.createElement('canvas')
  canvas.width = width * 2
  canvas.height = height * 2
  const context = canvas.getContext('2d')!
  context.scale(2, 2)
  const text = (value: string, x: number, y: number, font = '14px "Segoe UI", Arial, sans-serif', color = '#101828') => { context.font = font; context.fillStyle = color; context.fillText(value, x, y) }
  context.fillStyle = '#f9fafb'; context.fillRect(0, 0, width, height)
  text('System Info Dashboard', 28, 44, '700 25px "Segoe UI", Arial, sans-serif')
  text(`Fleet Status Overview · exported ${new Date().toLocaleString()}`, 28, 70, '14px "Segoe UI", Arial, sans-serif', '#475467')
  context.fillStyle = '#fff'; context.strokeStyle = '#d0d5dd'; context.fillRect(28, 88, 1384, 76); context.strokeRect(28, 88, 1384, 76)
  text('Artifact baselines', 48, 121, '700 17px "Segoe UI", Arial, sans-serif')
  sources.forEach((source, index) => {
    const x = 390 + index * 510
    const label = source.usable ? source.warning ? 'Usable with warning' : 'Usable' : 'Unavailable'
    const [color, fill] = source.usable && !source.warning ? ['#067647', '#dcfaee'] : source.usable ? ['#9a6700', '#fff3cd'] : ['#b42318', '#ffe4e8']
    text(`BC${source.bcVersion} · ${source.branch}`, x, 109, '13px "Segoe UI", Arial, sans-serif', '#475467')
    text(source.packageVersion || 'No package', x, 139, '700 14px "Segoe UI", Arial, sans-serif')
    context.fillStyle = fill; context.beginPath(); context.roundRect(x + 100, 120, label.length * 8 + 26, 26, 13); context.fill()
    text(label, x + 113, 138, '700 12px "Segoe UI", Arial, sans-serif', color)
  })
  context.fillStyle = '#fff'; context.strokeStyle = '#d0d5dd'; context.fillRect(28, 180, 1384, height - 228); context.strokeRect(28, 180, 1384, height - 228)
  ;[['CUSTOMER NAME', 48], ['BC VERSION', 405], ['INSTALLED VERSION', 755], ['INSTALLED DATE', 1040], ['STATUS', 1290]].forEach(([label, x]) => text(String(label), Number(x), 211, '700 12px "Segoe UI", Arial, sans-serif', '#475467'))
  rows.forEach((row, index) => {
    const y = 226 + index * rowHeight
    if (row.kind === 'customer') {
      context.fillStyle = '#f2f4f7'; context.fillRect(28, y, 1384, rowHeight)
      text(row.customer, 48, y + 27, '700 14px "Segoe UI", Arial, sans-serif'); text(row.bc, 405, y + 27)
      return
    }
    const [label, color, fill] = status(row.status)
    context.strokeStyle = '#e4e7ec'; context.beginPath(); context.moveTo(28, y + rowHeight); context.lineTo(1412, y + rowHeight); context.stroke()
    text(row.customer, 62, y + 27); text(row.version, 755, y + 27, '700 14px "Segoe UI", Arial, sans-serif'); text(row.date, 1040, y + 27)
    context.fillStyle = fill; context.strokeStyle = color; context.beginPath(); context.arc(1336, y + 21, 10, 0, Math.PI * 2); context.fill(); context.stroke(); text(label, 1354, y + 26, '700 12px "Segoe UI", Arial, sans-serif', color)
  })
  const link = document.createElement('a')
  link.href = canvas.toDataURL('image/png')
  link.download = `deployment-fleet-${new Date().toISOString().slice(0, 10)}.png`
  document.body.append(link)
  link.click()
  link.remove()
}
function FleetTable({ items }: { items: CustomerLatest[]; sources: ArtifactSource[] }) { if (!items.length) return <Empty text="No authoritative deployment events have been received yet." />; return <div className="table-wrap fleet-table"><table><thead><tr><th>Customer name</th><th>BC version</th><th>Installed version</th><th>Installed date</th><th className="status-column">Status</th></tr></thead><tbody>{items.flatMap(item => [<tr key={item.customerId} className="fleet-customer"><td><Link to={`/customers/${item.customerId}`}><strong>{item.customerName}</strong></Link></td><td>{item.bcVersion ? `BC${item.bcVersion}` : 'Not reported'}</td><td></td><td></td><td></td></tr>, ...item.tenants.map(tenant => { const label = tenant.health === 'current' ? 'Current' : tenant.health === 'failed' ? 'Update failed' : tenant.health === 'attention' ? 'Needs attention' : 'Not reported'; return <tr key={`${item.customerId}-${tenant.tenantId}`} className={`fleet-${tenant.health}`}><td className="tenant-name">↳&nbsp;&nbsp;{tenant.tenantLabel || tenant.tenantId}</td><td></td><td><span className={`version-pill ${tenant.health === 'current' ? 'current' : tenant.health === 'failed' ? 'failed' : ''}`}>{tenant.installedVersion || 'Unavailable'}</span></td><td>{tenant.installedAt ? <Time value={tenant.installedAt} /> : '—'}</td><td className="status-column"><Tooltip content={label} relationship="label">{tenant.health === 'current' ? <CheckmarkCircleRegular className="status-icon current" aria-label={label} /> : <WarningRegular className={`status-icon ${tenant.health === 'failed' ? 'failed' : 'warning'}`} aria-label={label} />}</Tooltip><span className="sr-only">{label}</span></td></tr> })])}</tbody></table></div> }
function DesiredAppTable({ grouped }: { grouped: Record<string, import('./types').DesiredAppState[]> }) { if (Object.keys(grouped).length === 0) return <Empty text="No desired-app inventory has been reported by an execute deployment yet." />; return <>{Object.entries(grouped).map(([tenant, states]) => <div className="tenant" key={tenant}><h3>{tenant}</h3><div className="table-wrap"><table className="desired-app-table"><thead><tr><th>Application</th><th>Desired version</th><th>Installed version</th><th>Installed date</th><th>Status</th></tr></thead><tbody>{states!.map(state => { const label = state.state === 'current' ? 'Current' : state.state === 'failed' ? 'Update failed' : state.state === 'outdated' ? 'Outdated' : state.state === 'planned' ? 'Planned' : 'Installed version unavailable'; return <tr key={`${state.tenantId}-${state.applicationId}`} className={`app-${state.state}`}><td><strong>{state.applicationName}</strong>{state.safeMessage && <small>{state.safeMessage}</small>}</td><td>{state.desiredVersion || 'Package unavailable'}</td><td>{state.installedVersion || 'Unavailable'}</td><td>{state.installedAt ? <Time value={state.installedAt} /> : '—'}</td><td>{label}</td></tr> })}</tbody></table></div></div>)}</> }
function DeploymentTable({ items }: { items: Deployment[] }) { if (!items.length) return <Empty text="No deployments match these filters." />; return <div className="table-wrap"><table><thead><tr><th>Status</th><th>Customer</th><th>Mode</th><th>Completed</th><th>Result</th><th></th></tr></thead><tbody>{items.map(item => <tr key={item.eventId}><td><Status status={item.status} /></td><td>{item.customer.name}</td><td>{item.mode === 'dryRun' ? 'Dry run' : 'Execute'}</td><td><Time value={item.completedAt} /></td><td>{item.summary.succeeded} ok · {item.summary.failed} failed</td><td><Link to={`/deployments/${encodeURIComponent(item.eventId)}`}>Details →</Link></td></tr>)}</tbody></table></div> }
function Time({ value }: { value: string }) { return <Tooltip content={new Date(value).toISOString()} relationship="label"><span>{formatDate(value)}</span></Tooltip> }
function OperationTable({ operations, adaptive }: { operations: Operation[]; adaptive: boolean }) { return <div className="table-wrap"><table><thead><tr><th>Application</th><th>Action</th><th>Version</th><th>Outcome</th><th>Message</th></tr></thead><tbody>{operations.map((item, index) => <tr key={`${item.applicationId}-${item.action}-${index}`}><td><strong>{item.applicationName}</strong>{item.publisher && <small>{item.publisher}</small>}</td><td>{item.action}</td><td>{item.previousVersion ? `${item.previousVersion} → ${item.observedVersion || item.targetVersion || 'unknown'}` : item.observedVersion || item.targetVersion || '—'}</td><td>{item.outcome}</td><td>{item.message || '—'}{adaptive && item.internalError && <details><summary>Internal error</summary><pre>{item.internalError}</pre></details>}</td></tr>)}</tbody></table></div> }
function Centered({ children }: { children: ReactNode }) { return <main className="centered">{children}</main> }
function Empty({ text }: { text: string }) { return <div className="empty"><strong>Nothing to show</strong><p>{text}</p></div> }
function ErrorState({ error, onSignIn }: { error: unknown; onSignIn?: () => void }) { const message = error instanceof Error ? error.message : 'Unexpected error.'; const expired = /session expired|rejected the signed-in session/i.test(message); const title = expired ? 'Session expired' : message.includes('not authorized') ? 'Access not authorized' : 'Could not load deployment data'; return <main className="centered"><div className="error-state"><strong>{title}</strong><p>{message}</p>{expired && onSignIn && <Button appearance="primary" onClick={onSignIn}>Sign in again</Button>}</div></main> }
function groupBy<T>(items: T[], key: (item: T) => string): Record<string, T[]> { return items.reduce((result, item) => { const value = key(item); (result[value] ||= []).push(item); return result }, {} as Record<string, T[]>) }
