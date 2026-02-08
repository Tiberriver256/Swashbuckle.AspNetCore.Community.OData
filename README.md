# Swashbuckle.AspNetCore.Community.OData

[![experimental](https://badges.github.io/stability-badges/dist/experimental.svg)](https://github.com/badges/stability-badges)

[![GitHub Actions Status](https://github.com/tiberriver256/Swashbuckle.AspNetCore.Community.OData/workflows/Build/badge.svg?branch=main)](https://github.com/tiberriver256/Swashbuckle.AspNetCore.Community.OData/actions) [![Swashbuckle.AspNetCore.Community.OData NuGet Package Downloads](https://img.shields.io/nuget/dt/Swashbuckle.AspNetCore.Community.OData)](https://www.nuget.org/packages/Swashbuckle.AspNetCore.Community.OData)

[![GitHub Actions Build History](https://buildstats.info/github/chart/tiberriver256/Swashbuckle.AspNetCore.Community.OData?branch=main&includeBuildsFromPullRequest=false)](https://github.com/tiberriver256/Swashbuckle.AspNetCore.Community.OData/actions)

**The most comprehensive OpenAPI (Swagger) documentation generator for ASP.NET Core OData APIs.**

Provides full support for OData query options ($filter, $select, $expand, etc.), real endpoint-based path generation, and seamless Swashbuckle integration.

## 🎬 Swagger UI Demo

<img src="./Images/swagger-ui-demo.webp" alt="Swagger UI demo" width="800" />

If animation is not shown in your Markdown viewer, open [`Images/swagger-ui-demo.webp`](./Images/swagger-ui-demo.webp) directly.

> [!TIP]
> Start with [DOCUMENTATION.md](./DOCUMENTATION.md) for the canonical reference and [RELEASE_NOTES.md](./RELEASE_NOTES.md) for change history and breaking changes.
>
> `AddEnhancedSwaggerGenOData*` is the default API path. `AddSwaggerGenOData` remains only as an obsolete compatibility shim.

## ✨ Features

- 🔍 **Full OData Query Support** - Automatic documentation of `$filter`, `$select`, `$expand`, `$orderby`, `$top`, `$skip`, `$count`, `$format` (and `$search` when enabled)
- 📡 **Real Endpoint-Based Generation** - Uses actual ASP.NET Core endpoint routing for accurate API documentation
- 🎯 **Complete OData Path Coverage** - Entity sets, singletons, functions, actions, property access, `$value`, `$ref`
- 🚀 **HTTP Method Accuracy** - Correctly captures GET, POST, PUT, PATCH, DELETE with proper request/response schemas
- 🔧 **Swashbuckle Integration** - Native integration with Swashbuckle.AspNetCore for UI and code generation
- 📊 **Method Overloads** - Supports multiple actions with same name, different parameters
- 🎨 **Customizable** - Configure query option examples, max page sizes, and more

## 🚀 Quick Start

### 1. Install Package

```bash
dotnet add package Swashbuckle.AspNetCore.Community.OData
```

### 2. Configure in Startup.cs

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add OData with query features
    services.AddControllers()
        .AddOData(o => o
            .AddRouteComponents("odata", GetEdmModel())
            .EnableQueryFeatures(100)
        );

    // Add enhanced OData Swagger with query options
    services.AddEnhancedSwaggerGenODataWithQueryOptions(
        odataSetupAction: opt =>
        {
            opt.SwaggerDoc(
                "v1",
                "odata",
                new OpenApiInfo 
                { 
                    Title = "My OData API", 
                    Version = "v1",
                    Description = "Full OData query support with $filter, $select, $expand!"
                }
            );
        },
        queryOptionsSettings: new ODataQueryOptionsSettings
        {
            EnableFilter = true,
            EnableSelect = true,
            EnableExpand = true,
            MaxTop = 100,
            FilterExample = "Name eq 'John' and Age gt 18"
        }
    );

}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();
    app.UseAuthorization();

    // Enable Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My OData API v1");
    });

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}

private static IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();
    builder.EntitySet<Product>("Products");
    return builder.GetEdmModel();
}
```

### 3. Configure in Program.cs (minimal hosting)

```csharp
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
    });

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My OData API v1"));
app.MapControllers();
app.Run();
```

## 📖 v2.0 Feature Set (Released)

### Enhanced Features

- **Endpoint-Based Path Generation**: Uses actual ASP.NET Core endpoint routing instead of just EDM inference
- **Full Query Options Documentation**: Every GET collection endpoint includes `$filter`, `$select`, `$expand`, etc.
- **Complete Path Coverage**: Property access (`/Products(1)/Name`), `$value`, `$ref` paths
- **Method Overload Support**: Multiple actions with same name are correctly documented
- **Better HTTP Semantics**: Accurate methods, status codes, request/response schemas

These features shipped in the v2.0 line (starting with v2.0.0).
Track version-by-version status in [RELEASE_NOTES.md](./RELEASE_NOTES.md).
See [ENHANCED_FEATURES.md](./ENHANCED_FEATURES.md) for detailed feature documentation.

## 📊 Comparison with Other Approaches

| Feature | Basic EDM | OData's Sample | **Enhanced Swashbuckle** |
|---------|-----------|----------------|--------------------------|
| OData Query Options | ❌ | ❌ | ✅ **Full support** |
| Real Endpoint Paths | ❌ | ✅ | ✅ **+ Enhanced** |
| Property Access Paths | ❌ | ❌ | ✅ **Added** |
| $value/$ref Paths | ❌ | ❌ | ✅ **Added** |
| Method Overloads | ❌ | ⚠️ | ✅ **Full** |
| Swashbuckle Integration | ✅ | ❌ | ✅ **Native** |

## 📝 Example Output

```yaml
paths:
  /Products:
    get:
      summary: Get entities from Products
      parameters:
        - name: $filter
          in: query
          description: Filter using OData expressions
          example: "Name eq 'John' and Price gt 100"
        - name: $select
          in: query
          description: Select specific properties
          example: "Name,Price,Category"
        - name: $expand
          in: query
          description: Expand related entities
          example: "Category,Orders"
        - name: $top
          in: query
          schema:
            type: integer
            maximum: 100
    post:
      summary: Create a new Product
  
  /Products({key}):
    get:
      summary: Get Product by key
    put:
      summary: Update Product (full)
    patch:
      summary: Update Product (partial with Delta)
    delete:
      summary: Delete Product
  
  /Products({key})/Category/$ref:
    get:
      summary: Get Category reference
    put:
      summary: Update Category reference
    delete:
      summary: Remove Category reference
```

## 🛠️ Advanced Configuration

### Multi-Route OData APIs

```csharp
services.AddEnhancedSwaggerGenODataWithQueryOptions(
    odataSetupAction: opt =>
    {
        opt.SwaggerDoc("v1", "odata", new OpenApiInfo { Title = "Public API", Version = "v1" });
        opt.SwaggerDoc("internal", "internal", new OpenApiInfo { Title = "Internal API", Version = "v1" });
    },
    queryOptionsSettings: new ODataQueryOptionsSettings
    {
        MaxTop = 1000,
        EnableSearch = true,
        FilterExample = "CreatedDate gt 2023-01-01"
    }
);
```

### Combine with Standard Swashbuckle

```csharp
services.AddEnhancedSwaggerGenOData(opt =>
{
    opt.SwaggerDoc("odata", "odata", new OpenApiInfo { Title = "OData API", Version = "v1" });
});

services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("rest", new OpenApiInfo { Title = "REST API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
});
```

## 🎯 Use Cases

- **API Documentation**: Interactive Swagger UI with full OData query support
- **Code Generation**: Client SDK generation from accurate OpenAPI specs
- **Testing**: Discover all endpoints including navigation properties
- **Standards Compliance**: OData-OpenAPI mapping specification compliance

## 📚 Documentation

- [DOCUMENTATION.md](./DOCUMENTATION.md) - Canonical reference and configuration guide
- [ENHANCED_FEATURES.md](./ENHANCED_FEATURES.md) - Feature-focused deep dive
- [RELEASE_NOTES.md](./RELEASE_NOTES.md) - Release and breaking-change history
- [DEVGUIDE.md](./DEVGUIDE.md) - Maintainer release/build workflow
- [MAINTAINERS.md](./MAINTAINERS.md) - Maintainer list
- [Examples/SimpleOdataApi](./Examples/SimpleOdataApi) - Lightweight sample implementation
- [Examples/ValidationHarness](./Examples/ValidationHarness) - Validation-focused sample with richer OData surface area
- [OData to OpenAPI Mapping](https://www.oasis-open.org/committees/document.php?document_id=61852&wg_abbrev=odata) - Official OData OpenAPI specification

## 🏗️ Building from Source

```bash
git clone https://github.com/Tiberriver256/Swashbuckle.AspNetCore.Community.OData.git
cd Swashbuckle.AspNetCore.Community.OData
dotnet restore
dotnet build
dotnet test
```

## 🤝 Contributing

Contributions are welcome. Please read our [Contributing Guidelines](.github/CONTRIBUTING.md).

For behavior-affecting changes, contributions are expected to include:

- unit/integration test updates
- relevant documentation updates (`README.md` and/or `DOCUMENTATION.md`)
- release note entries in `RELEASE_NOTES.md` when appropriate

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## 🙏 Acknowledgments

- Built on [Microsoft.OpenApi.OData](https://github.com/microsoft/OpenAPI.NET.OData) for EDM-to-OpenAPI conversion
- Integrated with [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) for Swagger UI
- Inspired by [ASP.NET Core OData](https://github.com/OData/AspNetCoreOData) routing samples
