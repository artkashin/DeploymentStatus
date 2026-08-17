import { expect, test } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

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

test('shows the customer landing page without Adaptive filters or metrics', async ({ page }) => {
  await page.route('**/api/v1/me', route => route.fulfill({ json: { name: 'guest@customer.test', isAdaptive: false, customerIds: ['tappers'] } }))
  await page.route('**/api/v1/customers', route => route.fulfill({ json: { generatedAt: '2026-08-17T10:00:00Z', items: [{ customerId: 'tappers', customerName: 'Tappers', eventId: 'event', status: 'success', mode: 'execute', completedAt: '2026-08-17T10:00:00Z', summary: { total: 1, succeeded: 1, failed: 0, skipped: 0, planned: 0 } }] } }))
  await page.route('**/api/v1/customers/tappers', route => route.fulfill({ json: { customer: { customerId: 'tappers', customerName: 'Tappers', eventId: 'event', status: 'success', mode: 'execute', completedAt: '2026-08-17T10:00:00Z', summary: { total: 1, succeeded: 1, failed: 0, skipped: 0, planned: 0 } }, currentState: [{ customerId: 'tappers', tenantId: 'default', tenantLabel: 'Main', applicationId: 'app', applicationName: 'Retail', version: '3.0.0.0', lastOutcome: 'success', verifiedAt: '2026-08-17T10:00:00Z', eventId: 'event' }] } }))
  await page.route('**/api/v1/deployments?**', route => route.fulfill({ json: { items: [] } }))
  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Tappers' })).toBeVisible()
  await expect(page.getByText('3.0.0.0')).toBeVisible()
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
