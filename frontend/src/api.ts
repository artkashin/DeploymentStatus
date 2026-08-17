import type { AccountInfo, IPublicClientApplication } from '@azure/msal-browser'
import { apiScope, authDisabled } from './auth'
import type { CustomerDetail, CustomerLatest, Deployment, DeploymentPage, Me } from './types'

const baseUrl = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, '')

export class ApiClient {
  constructor(private readonly msal: IPublicClientApplication, private readonly account?: AccountInfo) {}

  private async request<T>(path: string): Promise<T> {
    const headers = new Headers({ Accept: 'application/json' })
    if (authDisabled) {
      headers.set('X-Development-User', 'local@adaptive.test')
      headers.set('X-Development-Roles', 'DeploymentStatus.Adaptive.All')
    } else {
      if (!this.account) throw new Error('No signed-in account is available.')
      let result
      try { result = await this.msal.acquireTokenSilent({ account: this.account, scopes: [apiScope] }) }
      catch (error) {
        const detail = error instanceof Error ? error.message : 'The browser could not restore an access token.'
        throw new Error(`Your session expired. Sign in again. (${detail})`)
      }
      headers.set('Authorization', `Bearer ${result.accessToken}`)
    }
    const response = await fetch(`${baseUrl}${path}`, { headers })
    if (response.status === 401) throw new Error('The API rejected the signed-in session. Sign in again.')
    if (response.status === 403) throw new Error('Your account is not authorized for this deployment data.')
    if (!response.ok) throw new Error((await response.json().catch(() => null))?.error || `API request failed (${response.status}).`)
    return response.json() as Promise<T>
  }

  me = () => this.request<Me>('/v1/me')
  customers = () => this.request<{ items: CustomerLatest[]; generatedAt: string }>('/v1/customers')
  customer = (customerId: string) => this.request<CustomerDetail>(`/v1/customers/${encodeURIComponent(customerId)}`)
  deployments = (search: URLSearchParams) => this.request<DeploymentPage>(`/v1/deployments?${search}`)
  deployment = (eventId: string) => this.request<Deployment>(`/v1/deployments/${encodeURIComponent(eventId)}`)
}

export function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export function duration(startedAt: string, completedAt: string): string {
  const seconds = Math.max(0, Math.round((new Date(completedAt).getTime() - new Date(startedAt).getTime()) / 1000))
  return seconds < 60 ? `${seconds}s` : `${Math.floor(seconds / 60)}m ${seconds % 60}s`
}
