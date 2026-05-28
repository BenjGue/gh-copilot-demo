import { defineConfig, devices } from '@playwright/test'

const baseURL =
  process.env.PLAYWRIGHT_BASE_URL ||
  'https://dokploy-4ben7tuhatkfc.westeurope.cloudapp.azure.com/'

// Optional: connect to a shared remote Playwright server instead of running
// a browser locally. See ../playwright-server/README.md.
const wsEndpoint = process.env.PLAYWRIGHT_WS_ENDPOINT

export default defineConfig({
  testDir: './tests/e2e',
  timeout: 30_000,
  expect: { timeout: 5_000 },
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    ignoreHTTPSErrors: true,
    ...(wsEndpoint
      ? { connectOptions: { wsEndpoint } }
      : {}),
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
