import { defineConfig, devices } from '@playwright/test';

/**
 * Read environment variables from file.
 * https://github.com/motdotla/dotenv
 */
// import dotenv from 'dotenv';
// import path from 'path';
// dotenv.config({ path: path.resolve(__dirname, '.env') });

/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Blazor WASM Seller Hub — Frontend/EbayClone.Frontend (https profile) */
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? 'https://localhost:7251',
    ignoreHTTPSErrors: true,

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    /* Test against mobile viewports. */
    // {
    //   name: 'Mobile Chrome',
    //   use: { ...devices['Pixel 5'] },
    // },
    // {
    //   name: 'Mobile Safari',
    //   use: { ...devices['iPhone 12'] },
    // },

    /* Test against branded browsers. */
    // {
    //   name: 'Microsoft Edge',
    //   use: { ...devices['Desktop Edge'], channel: 'msedge' },
    // },
    // {
    //   name: 'Google Chrome',
    //   use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    // },
  ],

  /* Chạy API + Blazor trước khi test (từ thư mục Test/). Có thể bỏ qua nếu đã dotnet run sẵn. */
  webServer: [
    {
      command:
        'dotnet run --launch-profile https --project ../Backend/EbayClone.API/EbayClone.API.csproj',
      url: 'https://localhost:7250/swagger/index.html',
      reuseExistingServer: !process.env.CI,
      ignoreHTTPSErrors: true,
      timeout: 180_000,
    },
    {
      command:
        'dotnet run --launch-profile https --project ../Frontend/EbayClone.Frontend/EbayClone.Frontend.csproj',
      url: 'https://localhost:7251/',
      reuseExistingServer: !process.env.CI,
      ignoreHTTPSErrors: true,
      timeout: 180_000,
    },
  ],
});
