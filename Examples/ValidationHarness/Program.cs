using ValidationHarness;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Community.OData.DependencyInjection;
using Swashbuckle.AspNetCore.Community.OData.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add EF Core In-Memory Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ValidationDb"));

// Add OData with full query support
builder.Services.AddControllers()
    .AddOData(options => options
        .AddRouteComponents("odata", GetEdmModel())
        .AddRouteComponents("v2", GetEdmModelV2())
        .EnableQueryFeatures(100));

// ==========================================
// ENHANCED ODATA SWAGGER CONFIGURATION
// ==========================================

// Option 1: Enhanced with query options (RECOMMENDED)
builder.Services.AddEnhancedSwaggerGenODataWithQueryOptions(
    odataSetupAction: opt =>
    {
        // Document 1: Default OData route
        opt.SwaggerDoc(
            "v1",
            "odata",
            new OpenApiInfo
            {
                Title = "OData Validation API (v1)",
                Version = "v1",
                Description = "Full OData query support: $filter, $select, $expand, $orderby, $top, $skip, $count, $search"
            }
        );

        // Document 2: Version 2 route
        opt.SwaggerDoc(
            "v2",
            "v2",
            new OpenApiInfo
            {
                Title = "OData Validation API (v2)",
                Version = "v2",
                Description = "Advanced OData with functions and actions"
            }
        );
    },
    queryOptionsSettings: new ODataQueryOptionsSettings
    {
        // Enable all query options
        EnableFilter = true,
        EnableSelect = true,
        EnableExpand = true,
        EnableOrderBy = true,
        EnableTop = true,
        EnableSkip = true,
        EnableCount = true,
        EnableSearch = true,
        EnableFormat = true,
        EnablePagination = true,

        // Pagination settings
        MaxTop = 100,
        DefaultTop = 25,

        // Examples for documentation
        FilterExample = "Name eq 'Product A' and Price gt 100",
        SelectExample = "Id,Name,Price,Category",
        ExpandExample = "Category($select=Name),Supplier",
        OrderByExample = "Price desc,Name asc"
    }
);

// Add standard SwaggerGen for any non-OData endpoints
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("non-odata", new OpenApiInfo
    {
        Title = "Non-OData API",
        Version = "v1"
    });

    // Enable XML comments
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "ValidationHarness.xml");
    if (File.Exists(xmlFile))
    {
        c.IncludeXmlComments(xmlFile);
    }
});

var app = builder.Build();

// Seed test data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    SeedTestData(db);
}

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

// Enable Swagger
app.UseSwagger();

// Configure Swagger UI with multiple endpoints
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OData API v1 (Default Route)");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "OData API v2 (Advanced)");
    c.SwaggerEndpoint("/swagger/non-odata/swagger.json", "Standard REST API");

    c.DocumentTitle = "OData Enhanced Swagger - Validation UI";
    c.EnableDeepLinking();
    c.DisplayRequestDuration();
    c.ShowExtensions();
    c.EnableValidator();
    c.EnableFilter();
});

app.MapControllers();
app.Run();

// ==========================================
// EDM MODEL CONFIGURATION
// ==========================================

static IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();

    // Product entity
    var product = builder.EntityType<Product>();
    product.HasKey(p => p.Id);
    product.Property(p => p.Price).Order = 1;
    product.Property(p => p.Name).Order = 2;

    builder.EntitySet<Product>("Products");

    // Category entity
    var category = builder.EntityType<Category>();
    category.HasKey(c => c.Id);

    builder.EntitySet<Category>("Categories");

    // Supplier entity (singleton + entity set)
    builder.EntitySet<Supplier>("Suppliers");
    builder.Singleton<Supplier>("PrimarySupplier");

    // Complex types
    builder.ComplexType<Address>();

    // Functions
    var getByPriceRange = builder.EntityType<Product>().Collection
        .Function("GetByPriceRange")
        .ReturnsCollectionFromEntitySet<Product>("Products");
    getByPriceRange.Parameter<decimal>("minPrice");
    getByPriceRange.Parameter<decimal>("maxPrice");

    // Actions
    var rateProduct = builder.EntityType<Product>()
        .Action("Rate")
        .Returns<double>();
    rateProduct.Parameter<int>("rating");
    rateProduct.Parameter<string>("comment");

    // Unbound action
    builder.Action("ResetData");

    return builder.GetEdmModel();
}

static IEdmModel GetEdmModelV2()
{
    var builder = new ODataConventionModelBuilder();

    // Extended Product for v2
    builder.EntitySet<Product>("Products");
    builder.EntitySet<Category>("Categories");
    builder.EntitySet<Customer>("Customers");

    // Customer entity with more complex relationships
    var customer = builder.EntityType<Customer>();
    customer.HasKey(c => c.Id);

    // Function with complex parameter
    var canPurchase = builder.EntityType<Customer>()
        .Function("CanPurchase")
        .Returns<bool>();
    canPurchase.Parameter<int>("productId");
    canPurchase.Parameter<decimal>("maxPrice");

    // Composable function
    var getPremiumCustomers = builder.EntityType<Customer>().Collection
        .Function("GetPremium")
        .ReturnsCollectionFromEntitySet<Customer>("Customers");
    getPremiumCustomers.IsComposable = true;

    return builder.GetEdmModel();
}

// ==========================================
// TEST DATA SEEDING
// ==========================================

static void SeedTestData(AppDbContext db)
{
    if (db.Products.Any()) return;

    // Categories
    var electronics = new Category { Id = 1, Name = "Electronics", Description = "Electronic devices" };
    var clothing = new Category { Id = 2, Name = "Clothing", Description = "Apparel and accessories" };
    var food = new Category { Id = 3, Name = "Food", Description = "Food and beverages" };

    db.Categories.AddRange(electronics, clothing, food);

    // Suppliers
    var supplier1 = new Supplier
    {
        Id = 1,
        Name = "TechCorp Inc.",
        Address = new Address { Street = "123 Tech Ave", City = "Silicon Valley", State = "CA", ZipCode = "94000" }
    };
    var supplier2 = new Supplier
    {
        Id = 2,
        Name = "FashionHub",
        Address = new Address { Street = "456 Style St", City = "New York", State = "NY", ZipCode = "10001" }
    };

    db.Suppliers.AddRange(supplier1, supplier2);

    // Products
    var products = new List<Product>
    {
        new()
        {
            Id = 1,
            Name = "Laptop Pro",
            Price = 1299.99m,
            Stock = 50,
            Category = electronics,
            Supplier = supplier1,
            Description = "High-performance laptop",
            IsAvailable = true,
            SecretCode = "SECRET001"
        },
        new()
        {
            Id = 2,
            Name = "Wireless Mouse",
            Price = 29.99m,
            Stock = 200,
            Category = electronics,
            Supplier = supplier1,
            Description = "Ergonomic wireless mouse",
            IsAvailable = true,
            SecretCode = "SECRET002"
        },
        new()
        {
            Id = 3,
            Name = "Cotton T-Shirt",
            Price = 19.99m,
            Stock = 500,
            Category = clothing,
            Supplier = supplier2,
            Description = "Comfortable cotton tee",
            IsAvailable = true,
            SecretCode = "SECRET003"
        },
        new()
        {
            Id = 4,
            Name = "Designer Jeans",
            Price = 89.99m,
            Stock = 100,
            Category = clothing,
            Supplier = supplier2,
            Description = "Premium denim jeans",
            IsAvailable = false,
            SecretCode = "SECRET004"
        },
        new()
        {
            Id = 5,
            Name = "Coffee Beans",
            Price = 14.99m,
            Stock = 300,
            Category = food,
            Supplier = supplier1,
            Description = "Organic arabica coffee",
            IsAvailable = true,
            SecretCode = "SECRET005"
        }
    };

    db.Products.AddRange(products);

    // Customers for v2
    var customers = new List<Customer>
    {
        new() { Id = 1, Name = "John Doe", Email = "john@example.com", IsPremium = true, TotalSpent = 5000 },
        new() { Id = 2, Name = "Jane Smith", Email = "jane@example.com", IsPremium = false, TotalSpent = 500 },
        new() { Id = 3, Name = "Bob Johnson", Email = "bob@example.com", IsPremium = true, TotalSpent = 12000 }
    };

    db.Customers.AddRange(customers);

    db.SaveChanges();
}
