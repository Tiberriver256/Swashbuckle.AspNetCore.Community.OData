# Enhanced OData Swagger Features

This document describes the enhanced features added to `Swashbuckle.AspNetCore.Community.OData` that make it the most comprehensive OData OpenAPI documentation solution available.

## 🚀 Quick Start

```csharp
// In Startup.cs ConfigureServices
services.AddEnhancedSwaggerGenODataWithQueryOptions(
    odataSetupAction: opt =>
    {
        opt.SwaggerDoc("v1", "odata", new OpenApiInfo 
        { 
            Title = "My OData API", 
            Version = "v1",
            Description = "Full OData query support with $filter, $select, $expand, and more!"
        });
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
```

## ✨ Key Features

### 1. **Real Endpoint-Based Path Generation**

Unlike basic EDM-to-OpenAPI conversion, the enhanced generator:

- **Uses actual ASP.NET Core endpoint routing data**
- **Captures real HTTP methods** (GET, POST, PUT, PATCH, DELETE) from controllers
- **Supports method overloads** (multiple actions with same name, different parameters)
- **Accurately reflects the actual API surface** rather than inferred structure

```csharp
// Both of these are captured correctly
[HttpGet] public IActionResult Get() { }           // GET /Products
[HttpGet] public IActionResult Get(int key) { }    // GET /Products({key})
[HttpPost] public IActionResult Post(...) { }      // POST /Products
```

### 2. **Full OData Query Options Support**

Every GET operation on collection endpoints automatically includes:

| Parameter | Type | Description |
|-----------|------|-------------|
| `$filter` | string | OData filter expressions (e.g., `Name eq 'John'`) |
| `$select` | string | Select specific properties (e.g., `Name,Age,Email`) |
| `$expand` | string | Expand related entities (e.g., `Orders($filter=Amount gt 100)`) |
| `$orderby` | string | Sort results (e.g., `Name desc,Age asc`) |
| `$top` | integer | Limit results (configurable max) |
| `$skip` | integer | Skip N results for pagination |
| `$count` | boolean | Include total count in response |
| `$search` | string | Free-text search (when enabled) |
| `$format` | string | Response format control |

### 3. **Missing OData Path Coverage**

The enhanced generator adds paths that EDM-based generation misses:

```
✅ Entity property access:
   GET /Products({key})/Name
   GET /Products({key})/Category

✅ Raw value access:
   GET /Products({key})/Name/$value
   GET /Products({key})/$value

✅ Navigation property references:
   GET /Products({key})/Category/$ref
   PUT /Products({key})/Category/$ref
   DELETE /Products({key})/Category/$ref

✅ Collection count:
   GET /Products/$count
```

### 4. **HTTP Method-Specific Operations**

Each endpoint captures its actual HTTP methods with proper semantics:

```yaml
/Products({key}):
  get:
    summary: Get Product by key
  put:
    summary: Update Product (full update)
    requestBody:
      required: true
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Product'
  patch:
    summary: Update Product (partial update with Delta<T>)
  delete:
    summary: Delete Product
```

### 5. **Type Cast Segment Support**

Proper documentation of type casting in URLs:

```yaml
/Products({key})/ODataSample.Models.SpecialProduct:
  get:
    summary: Cast Product to SpecialProduct type
```

### 6. **Function and Action Import Support**

Both bound and unbound operations:

```yaml
/Products/ODataSample.GetTopProducts(amount={amount}):
  get:
    summary: Get top N products
    
/ResetData:
  post:
    summary: Reset all data
```

## 📊 Quality Comparison

| Feature | EDM-Only | OData Sample | **Enhanced Swashbuckle** |
|---------|----------|--------------|--------------------------|
| Entity sets & types | ✅ | ✅ | ✅ |
| OData query options | ❌ | ❌ | ✅ **Complete** |
| Method overloads | ❌ | ⚠️ Limited | ✅ **Full** |
| HTTP semantics | ⚠️ Generic | ✅ Good | ✅ **Excellent** |
| Property access | ❌ | ❌ | ✅ **Added** |
| $value paths | ❌ | ❌ | ✅ **Added** |
| $ref paths | ❌ | ❌ | ✅ **Added** |
| Endpoint metadata | ❌ | ✅ | ✅ **Enhanced** |
| Swashbuckle integration | N/A | ❌ | ✅ **Native** |

## 🛠️ Configuration Options

### OData Query Options Settings

```csharp
var settings = new ODataQueryOptionsSettings
{
    // Enable/disable specific query options
    EnableFilter = true,
    EnableSelect = true,
    EnableExpand = true,
    EnableOrderBy = true,
    EnableTop = true,
    EnableSkip = true,
    EnableCount = true,
    EnableSearch = true,
    EnableFormat = true,

    // Pagination settings
    MaxTop = 100,
    DefaultTop = 50,
    EnablePagination = true,

    // Examples for documentation
    FilterExample = "Name eq 'John' and Age gt 18",
    SelectExample = "Name,Age,Email",
    ExpandExample = "Orders($filter=Amount gt 100; $select=Id,Amount)",
    OrderByExample = "Name asc, CreatedDate desc"
};
```

### Service Registration Options

```csharp
// Option 1: Enhanced with query options
services.AddEnhancedSwaggerGenODataWithQueryOptions(
    odataSetupAction: opt => { ... },
    queryOptionsSettings: settings
);

// Option 2: Enhanced generator only
services.AddEnhancedSwaggerGenOData(opt => { ... });

// Option 3: Full customization
services.AddSwaggerGenForOData(options =>
{
    options.AddDocument("v1", "odata", apiInfo);
    options.AddDocument("v2", "v2", apiInfo);
    
    options.QueryOptionsSettings.MaxTop = 50;
    
    options.ConfigureSwaggerGen = swaggerOptions =>
    {
        swaggerOptions.IncludeXmlComments("MyApi.xml");
        swaggerOptions.CustomSchemaIds(type => type.FullName);
    };
});
```

## 📝 Example Output

### Before (EDM-Only)

```yaml
paths:
  /Products:
    get:
      summary: Get entities from Products
      # Missing: $filter, $select, $expand, etc.
  
  /Products({Id}):
    get:
      summary: Get entity from Products by key
      # Missing: PUT, PATCH, DELETE
```

### After (Enhanced)

```yaml
paths:
  /Products:
    get:
      summary: Get entities from Products
      description: Returns a list of Products with OData query support
      parameters:
        - name: $filter
          in: query
          description: Filter results using OData filter expressions
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
          example: 50
        - name: $skip
          in: query
          schema:
            type: integer
          example: 0
        - name: $count
          in: query
          schema:
            type: boolean
          description: Include total count in response (@odata.count)
      responses:
        200:
          description: Success
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Product'
    post:
      summary: Create a new Product
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Product'
  
  /Products({Id}):
    get:
      summary: Get Product by key
    put:
      summary: Update Product (full replacement)
    patch:
      summary: Update Product (partial with Delta)
    delete:
      summary: Delete Product
  
  /Products({Id})/Category:
    get:
      summary: Get Category navigation property
  
  /Products({Id})/Category/$ref:
    get:
      summary: Get Category reference
    put:
      summary: Update Category reference
    delete:
      summary: Remove Category reference
  
  /Products({Id})/Name/$value:
    get:
      summary: Get raw Name value
```

## 🔧 Advanced Usage

### Combining with Standard Swashbuckle

```csharp
services.AddEnhancedSwaggerGenOData(opt =>
{
    opt.SwaggerDoc("v1", "odata", new OpenApiInfo { Title = "OData API", Version = "v1" });
});

services.AddSwaggerGen(options =>
{
    // Standard Swashbuckle config for non-OData endpoints
    options.SwaggerDoc("public", new OpenApiInfo { Title = "Public API", Version = "v1" });
    
    // Add security definitions
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme { ... });
    
    // Add custom operation filters
    options.OperationFilter<AuthorizationOperationFilter>();
});
```

### Multi-Version OData APIs

```csharp
services.AddEnhancedSwaggerGenODataWithQueryOptions(
    odataSetupAction: opt =>
    {
        // Version 1 - Basic features
        opt.SwaggerDoc("v1", "odata", new OpenApiInfo 
        { 
            Title = "OData API", 
            Version = "1.0",
            Description = "Basic OData query support"
        });
        
        // Version 2 - Advanced features
        opt.SwaggerDoc("v2", "v2", new OpenApiInfo 
        { 
            Title = "OData API", 
            Version = "2.0",
            Description = "Advanced OData with functions and actions"
        });
    },
    queryOptionsSettings: new ODataQueryOptionsSettings
    {
        MaxTop = 1000,
        EnableSearch = true
    }
);
```

## 🎯 Benefits Summary

1. **Complete API Documentation** - No more missing endpoints or query options
2. **Accurate HTTP Semantics** - Proper methods, status codes, request/response schemas
3. **OData Ecosystem Integration** - Works seamlessly with Swashbuckle UI, code generators
4. **Developer Experience** - Full IntelliSense in Swagger UI for filter expressions
5. **Standards Compliant** - Based on official OData-OpenAPI mapping specification

## 📚 Further Reading

- [OData to OpenAPI Mapping](https://www.oasis-open.org/committees/document.php?document_id=61852&wg_abbrev=odata)
- [Microsoft.OpenApi.OData](https://github.com/microsoft/OpenAPI.NET.OData)
- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [ASP.NET Core OData](https://github.com/OData/AspNetCoreOData)
