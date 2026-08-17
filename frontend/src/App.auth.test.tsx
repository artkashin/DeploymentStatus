import { fireEvent, render, screen } from '@testing-library/react'
import type { ReactNode } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { App } from './App'

const loginRedirect = vi.hoisted(() => vi.fn())
vi.mock('./auth', () => ({ authDisabled: false, apiScope: 'api://test/Deployment.Read' }))
vi.mock('@azure/msal-react', () => ({
  useIsAuthenticated: () => false,
  useMsal: () => ({ instance: { loginRedirect }, accounts: [] }),
}))
vi.mock('@fluentui/react-components', () => ({
  Button: ({ children, onClick }: { children?: ReactNode; onClick?: () => void }) => <button onClick={onClick}>{children}</button>,
  Badge: () => null, Card: () => null, Input: () => null, Select: () => null, Spinner: () => null, Tooltip: () => null,
}))
vi.mock('@fluentui/react-icons', () => ({ ArrowClockwiseRegular: () => null, OpenRegular: () => null, SignOutRegular: () => null }))

describe('Microsoft Entra sign-in', () => {
  beforeEach(() => loginRedirect.mockReset())

  it('starts the PKCE redirect with the delegated read scope', () => {
    render(<App />)
    fireEvent.click(screen.getByRole('button', { name: 'Sign in with Microsoft Entra ID' }))
    expect(loginRedirect).toHaveBeenCalledWith({ scopes: ['api://test/Deployment.Read'] })
  })
})
