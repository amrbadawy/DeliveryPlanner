# Software Delivery Planner

This repository contains two .NET applications and a shared test project for software delivery planning and scheduling.

## Projects

- `SoftwareDeliveryPlanner.Blazor/` - Blazor web application.
- `SoftwareDeliveryPlannerCS/` - Windows Forms desktop application.
- `SoftwareDeliveryPlanner.Tests/` - xUnit tests.
- `SoftwareDeliveryPlanner.slnx` - Solution file.

## Prerequisites

- .NET SDK 10.0+
- Windows (required for WinForms project and current test target framework)
- Optional: Node.js (only needed for Playwright e2e tests under the Blazor project)

## Bootstrap Assets

Bootstrap files under `SoftwareDeliveryPlanner.Blazor/wwwroot/lib/bootstrap/` are intentionally vendored and committed.

## Build and Test

From the repository root:

```bash
dotnet restore SoftwareDeliveryPlanner.slnx
dotnet build SoftwareDeliveryPlanner.slnx
dotnet test SoftwareDeliveryPlanner.Tests/SoftwareDeliveryPlanner.Tests.csproj
```

## Run Applications

### Blazor App

```bash
dotnet run --project SoftwareDeliveryPlanner.Blazor/SoftwareDeliveryPlanner.Blazor.csproj
```

### WinForms App

```bash
dotnet run --project SoftwareDeliveryPlannerCS/SoftwareDeliveryPlanner.csproj
```

## Publish Desktop App

```bash
dotnet publish SoftwareDeliveryPlannerCS/SoftwareDeliveryPlanner.csproj -c Release
```

The project is configured for self-contained single-file publish on `win-x64`.

## CI

GitHub Actions workflow is defined at `.github/workflows/ci.yml` and runs restore, build, and tests on pushes and pull requests targeting `master` or `main`.
