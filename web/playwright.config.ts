import { randomUUID } from "node:crypto";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { defineConfig } from "@playwright/test";

const databasePath = join(tmpdir(), `task-manager-e2e-${randomUUID()}.db`);

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  forbidOnly: Boolean(process.env.CI),
  reporter: process.env.CI ? "github" : "list",
  globalTeardown: "./e2e/cleanup.ts",
  metadata: { databasePath },
  use: {
    baseURL: "http://127.0.0.1:5378",
    browserName: "chromium",
    channel: "chromium",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    { name: "desktop", use: { viewport: { width: 1280, height: 800 } } },
    {
      name: "mobile",
      use: {
        viewport: { width: 390, height: 844 },
        isMobile: true,
        hasTouch: true,
      },
    },
  ],
  webServer: [
    {
      command:
        "dotnet run --project ../src/TaskManager.Api --configuration Release --no-restore --no-launch-profile --urls http://127.0.0.1:5455",
      url: "http://127.0.0.1:5455/api/health",
      reuseExistingServer: false,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: "Testing",
        Jwt__SigningKey:
          "e2e-only-signing-key-not-for-deployment-64-bytes-long",
        Database__Path: databasePath,
        Demo__Seed: "false",
        Logging__LogLevel__Default: "Warning",
      },
    },
    {
      command: "npm run dev -- --port 5378",
      url: "http://127.0.0.1:5378",
      reuseExistingServer: false,
      env: { API_PROXY_TARGET: "http://127.0.0.1:5455" },
    },
  ],
});
