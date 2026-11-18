import { expect, test } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import type { CustomerLatest, TenantLatest } from '../src/types'

async function routeEmptyAdaptive(page: import('@playwright/test').Page) {
  await page.route('**/api/v1/me', route => route.fulfill({ json: { name: 'local@adaptive.test', isAdaptive: true, customerIds: [] } }))
  await page.route('**/api/v1/customers', route => route.fulfill({ json: { items: [], generatedAt: '2026-08-17T10:00:00Z' } }))
  await page.route('**/api/v1/artifact-sources', route => route.fulfill({ json: { items: [], generatedAt: '2026-08-17T10:00:00Z' } }))
  await page.route('**/api/v1/deployments?**', route => route.fulfill({ json: { items: [] } }))
}

test('renders the secured application shell in local development mode', async ({ page }) => {
  await routeEmptyAdaptive(page)
  await page.goto('/')
  await expect(page.getByText('Fleet Status Overview', { exact: true })).toBeVisible()
  await expect(page.getByText('No authoritative deployment events have been received yet.')).toBeVisible()
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
})

test('shows separate Adaptive and Avalara installed versions in the fleet', async ({ page }) => {
  await routeEmptyAdaptive(page)
  const production: TenantLatest = {
    tenantId: 'baileybox', tenantLabel: 'BAILEYBOX', installedVersion: 'Multiple versions',
    adaptiveInstalledVersion: '1.0.1318.0', avalaraInstalledVersion: '27.0.52.0',
    desiredAppCount: 2, currentAppCount: 2, attentionAppCount: 0, failedAppCount: 0, health: 'current'
  }
  const customer: CustomerLatest = {
    customerId: 'baileybox', customerName: 'baileybox', customerType: 'Retail', bcVersion: '27',
    eventId: 'event', status: 'success', mode: 'execute', completedAt: '2026-08-17T10:00:00Z',
    summary: { total: 3, succeeded: 3, failed: 0, skipped: 0, planned: 0 },
    typeApps: [], desiredAppCount: 3, currentAppCount: 3, attentionAppCount: 0, failedAppCount: 0, health: 'current',
    tenants: [production, {
      ...production, tenantId: 'baileyboxplay', tenantLabel: 'BAILEYBOXPLAY',
      installedVersion: '1.0.1318.0', avalaraInstalledVersion: null, desiredAppCount: 1, currentAppCount: 1
    }]
  }
  await page.route('**/api/v1/customers', route => route.fulfill({ json: { items: [customer], generatedAt: customer.completedAt } }))
  await page.goto('/')

  const headers = page.getByRole('columnheader')
  await expect(headers.nth(3)).toHaveText('Adaptive apps version')
  await expect(headers.nth(4)).toHaveText('Avalara AvaTax version')
  const rows = page.getByRole('row')
  await expect(rows.nth(2).getByRole('cell').nth(3)).toHaveText('1.0.1318.0')
  await expect(rows.nth(2).getByRole('cell').nth(4)).toHaveText('27.0.52.0')
  await expect(rows.nth(3).getByRole('cell').nth(4)).toHaveText('—')
  await expect(page.getByText('Multiple versions')).toHaveCount(0)

  const [snapshot] = await Promise.all([
    page.waitForEvent('download'),
    page.getByRole('button', { name: 'Download fleet snapshot as PNG' }).click()
  ])
  expect(snapshot.suggestedFilename()).toMatch(/^deployment-fleet-.+\.png$/)
  expect(await snapshot.failure()).toBeNull()
})

test('shows the customer landing page without Adaptive filters or metrics', async ({ page }) => {
  await page.route('**/api/v1/me', route => route.fulfill({ json: { name: 'guest@customer.test', isAdaptive: false, customerIds: ['tappers'] } }))
  const customer = { customerId: 'tappers', customerName: 'Tappers', eventId: 'event', status: 'success', mode: 'execute', completedAt: '2026-08-17T10:00:00Z', summary: { total: 1, succeeded: 1, failed: 0, skipped: 0, planned: 0 }, desiredAppCount: 1, currentAppCount: 1, attentionAppCount: 0, failedAppCount: 0, health: 'current', tenants: [{ tenantId: 'default', tenantLabel: 'Main', installedVersion: '3.0.0.0', installedAt: '2026-08-17T10:00:00Z', desiredAppCount: 1, currentAppCount: 1, attentionAppCount: 0, failedAppCount: 0, health: 'current' }] }
  await page.route('**/api/v1/customers', route => route.fulfill({ json: { generatedAt: '2026-08-17T10:00:00Z', items: [customer] } }))
  await page.route('**/api/v1/customers/tappers', route => route.fulfill({ json: { customer, desiredAppState: [{ customerId: 'tappers', tenantId: 'default', tenantLabel: 'Main', applicationId: 'app', applicationName: 'Retail', desiredVersion: '3.0.0.0', installedVersion: '3.0.0.0', state: 'current', lastOutcome: 'success', observedAt: '2026-08-17T10:00:00Z', updatedAt: '2026-08-17T10:00:00Z', eventId: 'event' }] } }))
  await page.route('**/api/v1/deployments?**', route => route.fulfill({ json: { items: [] } }))
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Tappers' })).toBeVisible()
  await page.goto('/customers/tappers')
  await expect(page.locator('a[href="/customers/tappers/tenants/default"]')).toBeVisible()
  await expect(page.getByText('Desired-app inventory is tenant-specific.')).toBeVisible()
  await expect(page.getByRole('cell', { name: '3.0.0.0' })).toHaveCount(0)
  await expect(page.getByLabel('Customer')).toHaveCount(0)
  await expect(page.getByText('Adaptive operations')).toHaveCount(0)
})

test('renders an explicit expired-session state', async ({ page }) => {
  await page.route('**/api/v1/me', route => route.fulfill({ status: 401, json: { error: 'Authentication is required.' } }))
  await page.goto('/')
  await expect(page.getByText('Session expired', { exact: true })).toBeVisible()
})

test('remains usable at a narrow mobile viewport', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 })
  await routeEmptyAdaptive(page)
  await page.goto('/')
  await expect(page.getByText('Fleet Status Overview', { exact: true })).toBeVisible()
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
  expect(overflow).toBeLessThanOrEqual(0)
})
