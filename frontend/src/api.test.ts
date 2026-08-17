import { describe, expect, it, vi } from 'vitest'
import type { AccountInfo, IPublicClientApplication } from '@azure/msal-browser'
import { ApiClient, duration, formatDate } from './api'

describe('display formatting', () => {
  it('formats durations without losing seconds', () => {
    expect(duration('2026-08-17T10:00:00Z', '2026-08-17T10:02:05Z')).toBe('2m 5s')
  })
  it('formats timestamps in the browser locale', () => {
    vi.stubEnv('TZ', 'UTC')
    expect(formatDate('2026-08-17T10:00:00Z')).toContain('2026')
  })
  it('turns silent token failures into an explicit expired-session state', async () => {
    const msal = { acquireTokenSilent: vi.fn().mockRejectedValue(new Error('interaction required')) } as unknown as IPublicClientApplication
    const client = new ApiClient(msal, {} as AccountInfo)
    await expect(client.me()).rejects.toThrow('Your session expired. Sign in again.')
  })
})
