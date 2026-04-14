# Software Delivery Planner

This repository contains a Blazor application and a shared test project for software delivery planning and scheduling.

## Projects

- `SoftwareDeliveryPlanner.Web/` - Blazor web application.
- `SoftwareDeliveryPlanner.Tests/` - xUnit tests.
- `SoftwareDeliveryPlanner.slnx` - Solution file.

## Prerequisites

- .NET SDK 10.0+
- Windows (required for current test target framework)
- Optional: Node.js (only needed for Playwright e2e tests under the Blazor project)

## Bootstrap Assets

Bootstrap files under `SoftwareDeliveryPlanner.Web/wwwroot/lib/bootstrap/` are intentionally vendored and committed.

## Build and Test

From the repository root:

```bash
dotnet restore SoftwareDeliveryPlanner.slnx
dotnet build SoftwareDeliveryPlanner.slnx
dotnet test SoftwareDeliveryPlanner.Tests/SoftwareDeliveryPlanner.Tests.csproj
dotnet test SoftwareDeliveryPlanner.ArchTests/SoftwareDeliveryPlanner.ArchTests.csproj

# Optional but recommended quality gates
pwsh .github/scripts/verify-licenses.ps1 -RootPath . -PolicyFile .github/license-policy.json
npm --prefix SoftwareDeliveryPlanner.Web ci
npm --prefix SoftwareDeliveryPlanner.Web run test:e2e:smoke
```

## Run Applications

### Blazor App

```bash
dotnet run --project SoftwareDeliveryPlanner.Web/SoftwareDeliveryPlanner.Web.csproj
```

## CI

GitHub Actions workflow is defined at `.github/workflows/ci.yml` and runs restore, build, and tests on pushes and pull requests targeting `master` or `main`.
