import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { FluentProvider, webLightTheme } from '@fluentui/react-components'
import { BrowserRouter } from 'react-router-dom'
import { App } from './App'
import { msalInstance } from './auth'
import './styles.css'

const queryClient = new QueryClient({ defaultOptions: { queries: { retry: 1, staleTime: 15_000, refetchInterval: 30_000 } } })

await msalInstance.initialize()
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MsalProvider instance={msalInstance}>
      <QueryClientProvider client={queryClient}>
        <FluentProvider theme={webLightTheme}>
          <BrowserRouter><App /></BrowserRouter>
        </FluentProvider>
      </QueryClientProvider>
    </MsalProvider>
  </StrictMode>,
)
