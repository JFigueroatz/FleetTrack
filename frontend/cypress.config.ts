import { defineConfig } from "cypress";

export default defineConfig({
  e2e: {
    baseUrl: process.env.CYPRESS_BASE_URL ?? "http://localhost:5173",
    supportFile: "cypress/support/e2e.ts",
    video: false,
  },
  env: {
    apiUrl: process.env.CYPRESS_API_URL ?? "http://localhost:8080",
  },
});
