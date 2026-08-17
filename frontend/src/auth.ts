import { PublicClientApplication, type Configuration } from '@azure/msal-browser'

export const authDisabled = import.meta.env.VITE_AUTH_DISABLED === 'true'
export const apiScope = `api://${import.meta.env.VITE_ENTRA_API_CLIENT_ID}/Deployment.Read`

const configuration: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_SPA_CLIENT_ID || '00000000-0000-0000-0000-000000000000',
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID || 'organizations'}`,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: { cacheLocation: 'sessionStorage' },
}

export const msalInstance = new PublicClientApplication(configuration)
