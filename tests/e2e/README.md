# PoNovaWeight E2E Tests

End-to-end tests for PoNovaWeight using Playwright and TypeScript.

## Prerequisites

- Node.js 18+
- The API must be running at `http://localhost:5000`
- Start the API locally with `dotnet run --project src/PoNovaWeight.Api`

## Setup

```bash
cd tests/e2e
npm install
npx playwright install chromium
```

## Running Tests

```bash
# Run all tests
npm test

# Run tests with browser visible
npm run test:headed

# Run tests in UI mode
npm run test:ui

# Debug tests
npm run test:debug

# View test report
npm run test:report
```

## Test Suites

### Google Auth Tests (`google-auth.spec.ts`)
- Login page displays correct UI elements
- Auth flow works correctly (redirect, auth/me, logout)

### Client Homepage Tests (`client-homepage.spec.ts`)
- Homepage loads Blazor WebAssembly
- Homepage has correct title
- Homepage redirects unauthenticated users to login
- Homepage loads static assets
- Health endpoint returns healthy
