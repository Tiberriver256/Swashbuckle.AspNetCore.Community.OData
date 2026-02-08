# Swashbuckle.AspNetCore.Community.OData Documentation

Comprehensive reference for integrating OData OpenAPI generation with Swashbuckle.

> [!TIP]
> For release history and breaking changes, see [RELEASE_NOTES.md](./RELEASE_NOTES.md).

## Table of contents

- [Overview](#overview)
- [Package goals](#package-goals)
- [Requirements and compatibility](#requirements-and-compatibility)
- [Installation](#installation)
- [Getting started](#getting-started)
- [Dependency injection API](#dependency-injection-api)
  - [Enhanced API (default)](#enhanced-api-default)
  - [Legacy compatibility API](#legacy-compatibility-api)
- [Configuration options](#configuration-options)
  - [SwaggerGenODataOptions](#swaggergenodataoptions)
  - [SwaggerODataGeneratorOptions](#swaggerodatageneratoroptions)
  - [ODataQueryOptionsSettings](#odataqueryoptionssettings)
- [What gets generated](#what-gets-generated)
- [OpenAPI and dependency strategy](#openapi-and-dependency-strategy)
- [Testing and validation](#testing-and-validation)
- [Troubleshooting](#troubleshooting)
- [Migration notes](#migration-notes)

## Overview

`Swashbuckle.AspNetCore.Community.OData` provides OData-aware OpenAPI generation on top of Swashbuckle.

The package defaults to **enhanced endpoint-aware generation**.

The enhanced mode uses ASP.NET Core endpoint metadata and augments the generated OpenAPI document with richer OData behavior (query options, path coverage, method mapping).

A legacy registration API is still available as a compatibility shim, but it now routes through the enhanced generator and is marked obsolete.

## Package goals

- Keep the package **Swashbuckle-first** for easy adoption in existing ASP.NET Core APIs.
- Generate docs from **actual endpoint routing data**, not only inferred EDM paths.
- Provide excellent documentation coverage for common OData features:
  - `$filter`, `$select`, `$expand`, `$orderby`, `$top`, `$skip`, `$count`, `$search`, `$format`
  - navigation references (`$ref`)
  - raw values (`$value`)
  - entity property paths
- Preserve compatibility with common OData + Swashbuckle usage patterns.

## Requirements and compatibility

Current baseline:

- .NET: `net10.0`
- `Microsoft.AspNetCore.OData`: `9.4.1`
- `Microsoft.OpenApi.OData`: `3.1.0`
- `Swashbuckle.AspNetCore.Swagger` / `SwaggerGen`: `10.1.2`

Check [RELEASE_NOTES.md](./RELEASE_NOTES.md) for compatibility updates and breaking changes.

## Installation

```bash
dotnet add package Swashbuckle.AspNetCore.Community.OData
```

## Getting started

### Minimal setup (enhanced mode)

```csharp
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Community.OData.DependencyInjection;
using Swashbuckle.AspNetCore.Community.OData.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddOData(o => o
        .AddRouteComponents("odata", GetEdmModel())
        .EnableQueryFeatures(100));

builder.Services.AddEnhancedSwaggerGenODataWithQueryOptions(
    odataSetupAction: opt =>
    {
        opt.SwaggerDoc("v1", "odata", new OpenApiInfo
        {
            Title = "My OData API",
            Version = "v1"
        });
    },
    queryOptionsSettings: new ODataQueryOptionsSettings
    {
        EnableFilter = true,
        EnableSelect = true,
        EnableExpand = true,
        EnableOrderBy = true,
        EnableTop = true,
        EnableSkip = true,
        EnableCount = true,
        MaxTop = 100
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));

app.MapControllers();
app.Run();

static Microsoft.OData.Edm.IEdmModel GetEdmModel()
{
    var modelBuilder = new ODataConventionModelBuilder();
    modelBuilder.EntitySet<Product>("Products");
    return modelBuilder.GetEdmModel();
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

## Dependency injection API

Namespace: `Swashbuckle.AspNetCore.Community.OData.DependencyInjection`

### Enhanced API (default)

#### `AddEnhancedSwaggerGenOData`

Registers the enhanced OData `ISwaggerProvider` and options pipeline.

```csharp
services.AddEnhancedSwaggerGenOData(options =>
{
    options.SwaggerDoc("v1", "odata", new OpenApiInfo { Title = "API", Version = "v1" });
});
```

#### `AddEnhancedSwaggerGenODataWithQueryOptions`

Same as above + registers `ODataQueryOptionsDocumentFilter` and applies `ODataQueryOptionsSettings`.

This method already wires `AddSwaggerGen(...)`; do not add a second bare `AddSwaggerGen()` call after it in the same registration path.

```csharp
services.AddEnhancedSwaggerGenODataWithQueryOptions(
    odataSetupAction: options => { /* docs */ },
    queryOptionsSettings: new ODataQueryOptionsSettings { MaxTop = 200 });
```

#### `AddSwaggerGenForOData`

Combined configuration model for OData docs + query options + extra SwaggerGen configuration.

```csharp
services.AddSwaggerGenForOData(options =>
{
    options.AddDocument("v1", "odata", new OpenApiInfo { Title = "API", Version = "v1" });
    options.QueryOptionsSettings.EnableSearch = true;
    options.ConfigureSwaggerGen = swagger =>
    {
        // additional Swashbuckle config
    };
});
```

#### `AddODataSwaggerGen`

Adds OData document filter behavior via standard `AddSwaggerGen` pipeline.

### Legacy compatibility API

#### `AddSwaggerGenOData`

Legacy compatibility shim. This API is marked obsolete and forwards to the enhanced generator pipeline.

```csharp
services.AddSwaggerGenOData(options =>
{
    options.SwaggerDoc("v1", "odata", new OpenApiInfo { Title = "API", Version = "v1" });
});
```

`SwaggerGenODataOptionsExtensions.SwaggerDoc(...)` is used to register named docs and OData route mappings.

Plan migration to `AddEnhancedSwaggerGenOData(...)` or `AddEnhancedSwaggerGenODataWithQueryOptions(...)` because the legacy shim is scheduled for removal in the next major after v2.

## Configuration options

### SwaggerGenODataOptions

Container for `SwaggerODataGeneratorOptions` (via the `SwaggerGeneratorODataOptions` property).

### SwaggerODataGeneratorOptions

Key properties:

- `SwaggerDocs`: named document map (`name -> (route, OpenApiInfo)`)
- `EdmModels`: route -> `IEdmModel`
- `QueryOptionsSettings`: query option documentation settings

### ODataQueryOptionsSettings

Controls OData query parameter generation and examples.

Key settings:

- Feature flags:
  - `EnableFilter`, `EnableSelect`, `EnableExpand`, `EnableOrderBy`
  - `EnableTop`, `EnableSkip`, `EnableCount`, `EnableSearch`, `EnableFormat`
  - `EnablePagination`
- Limits/defaults:
  - `MaxTop`, `DefaultTop`
- Example values:
  - `FilterExample`, `SelectExample`, `ExpandExample`, `OrderByExample`

## What gets generated

Enhanced generation can include:

- Collection and entity paths with correct HTTP methods.
- Query option parameters on collection GET operations.
- Property access and raw `$value` paths.
- Navigation `$ref` paths (GET/PUT/DELETE where applicable).
- OpenAPI metadata merged with endpoint routing information.

## OpenAPI and dependency strategy

This package tracks major OpenAPI and Swashbuckle changes deliberately to maintain compatibility with modern ASP.NET Core OData stacks.

Guidance:

- Keep OData packages aligned (`Microsoft.OData.Core` / `Microsoft.OData.Edm`) to avoid dependency constraint warnings.
- Prefer staged upgrades for major OpenAPI changes; validate generated document shape and runtime behavior.
- Run build + tests + package generation on all supported CI platforms before release.

## Testing and validation

Recommended validation sequence:

```bash
dotnet build Swashbuckle.AspNetCore.Community.OData.sln -c Release
dotnet test --project Tests/Swashbuckle.AspNetCore.Community.OData.Test/Swashbuckle.AspNetCore.Community.OData.Test.csproj -c Release
dotnet cake --target=Default
```

Validation harness resources:

- `Examples/ValidationHarness/VALIDATION_CHECKLIST.md`
- `Examples/ValidationHarness/VALIDATION_REPORT.md`

## Troubleshooting

### Swagger doc not generated for OData route

- Ensure your OData route prefix in `SwaggerDoc(name, route, info)` matches `AddRouteComponents(route, model)`.
- Ensure the corresponding EDM model is present in route components.

### Query options not visible

- Use `AddEnhancedSwaggerGenODataWithQueryOptions(...)` or ensure `ODataQueryOptionsDocumentFilter` is registered.

### Missing paths or wrong methods

- Prefer enhanced mode (endpoint-aware provider).
- Confirm controller actions are mapped and reachable in endpoint routing.

### Dependency warnings during restore

- Align explicit OData package references (`Microsoft.OData.Core`, `Microsoft.OData.Edm`) with resolved transitive constraints.

## Migration notes

### From legacy provider to enhanced provider

1. Replace `AddSwaggerGenOData(...)` with `AddEnhancedSwaggerGenOData(...)`.
2. If query options should be documented globally, use `AddEnhancedSwaggerGenODataWithQueryOptions(...)`.
3. Re-validate generated OpenAPI for route names, method coverage, and parameter shape.

> `AddSwaggerGenOData(...)` currently forwards to the enhanced pipeline, but is obsolete and intended only for temporary compatibility.

### From older OpenAPI.NET / Swashbuckle APIs

- Use `Microsoft.OpenApi` namespace APIs.
- Use `System.Net.Http.HttpMethod` operation keys where required by modern OpenAPI model.
- Update schema and example handling to current OpenAPI object model.

---

If you find inaccuracies in this document, please open an issue or submit a PR with updates to both docs and tests.
