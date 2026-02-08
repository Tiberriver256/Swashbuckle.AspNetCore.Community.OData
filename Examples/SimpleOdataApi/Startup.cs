using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Community.OData.DependencyInjection;
using Swashbuckle.AspNetCore.Community.OData.OpenApi;

namespace SimpleOdataApi
{
    /// <summary>
    /// Startup class demonstrating enhanced OData Swagger integration.
    /// </summary>
    public class Startup(IConfiguration configuration)
    {
        public IConfiguration Configuration { get; } = configuration;

        /// <summary>
        /// Configures services for the application.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            // Add OData with routing
            services.AddControllers()
                .AddOData(o => o
                    .AddRouteComponents("odata", GetEdmModel())
                    .AddRouteComponents("v1", GetEdmModelV1())
                    .EnableQueryFeatures(100) // Enable $filter, $select, $top, etc.
                );

            // Option 1: Use the enhanced OData Swagger generator with query options
            services.AddEnhancedSwaggerGenODataWithQueryOptions(
                odataSetupAction: opt =>
                {
                    // Define Swagger documents for each OData route
                    opt.SwaggerDoc(
                        "v1",
                        "odata",
                        new OpenApiInfo
                        {
                            Title = "My OData API (Default)",
                            Version = "v1",
                            Description = "OData API with full query support ($filter, $select, $expand, etc.)"
                        }
                    );

                    opt.SwaggerDoc(
                        "v1-internal",
                        "v1",
                        new OpenApiInfo
                        {
                            Title = "My OData API (Version 1)",
                            Version = "v1",
                            Description = "Internal API with enhanced OData features"
                        }
                    );
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
                    EnableSearch = true,
                    MaxTop = 100,
                    DefaultTop = 50,
                    FilterExample = "Name eq 'John' and Age gt 18",
                    SelectExample = "Name,Age,Email,Orders",
                    ExpandExample = "Orders($filter=Amount gt 100)"
                }
            );

            // Option 2: Alternative - Use combined setup
            // services.AddSwaggerGenForOData(options =>
            // {
            //     options.AddDocument("v1", "odata", new OpenApiInfo { Title = "API", Version = "v1" })
            //            .AddDocument("v1-internal", "v1", new OpenApiInfo { Title = "API v1", Version = "v1" });
            //     
            //     options.QueryOptionsSettings.MaxTop = 100;
            //     
            //     options.ConfigureSwaggerGen = swaggerOptions =>
            //     {
            //         swaggerOptions.CustomSchemaIds(type => type.FullName);
            //     };
            // });

            // Add standard SwaggerGen for non-OData endpoints (optional)
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("non-odata", new OpenApiInfo { Title = "Non-OData API", Version = "v1" });
            });
        }

        /// <summary>
        /// Configures the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            // Enable Swagger middleware
            app.UseSwagger();

            // Configure Swagger UI with multiple endpoints
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "OData API (Default)");
                c.SwaggerEndpoint("/swagger/v1-internal/swagger.json", "OData API (v1)");
                c.SwaggerEndpoint("/swagger/non-odata/swagger.json", "Standard API");

                // Enable deep linking for easy navigation
                c.EnableDeepLinking();
                c.DisplayRequestDuration();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        /// <summary>
        /// Gets the default EDM model.
        /// </summary>
        private static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            // Configure WeatherForecast entity
            builder.EntityType<WeatherForecast>()
                .HasKey(f => f.Id);

            // Add restrictions (shown in OpenAPI as constraints)
            builder.EntityType<WeatherForecast>()
                .HasDeleteRestrictions()
                .IsDeletable(false)
                .HasDescription("Deletion of weather forecasts is not supported");

            builder.EntityType<WeatherForecast>()
                .HasUpdateRestrictions()
                .IsUpdatable(true)
                .HasDescription("Updates are allowed with restrictions");

            builder.EntityType<WeatherForecast>()
                .HasInsertRestrictions()
                .IsInsertable(true)
                .HasDescription("New forecasts can be added");

            builder.EntitySet<WeatherForecast>("WeatherForecasts");

            return builder.GetEdmModel();
        }

        /// <summary>
        /// Gets the v1 EDM model with more entities.
        /// </summary>
        private static IEdmModel GetEdmModelV1()
        {
            var builder = new ODataConventionModelBuilder();

            builder.EntityType<WeatherForecast>().HasKey(f => f.Id);
            builder.EntitySet<WeatherForecast>("WeatherForecasts");

            // Add a Product entity for v1
            builder.EntityType<Product>()
                .HasKey(p => p.Id);

            builder.EntitySet<Product>("Products");

            // Add a function to demonstrate OData functions in Swagger
            var getByTemperature = builder.EntityType<WeatherForecast>().Collection
                .Function("GetByTemperature")
                .ReturnsCollectionFromEntitySet<WeatherForecast>("WeatherForecasts");
            getByTemperature.Parameter<int>("minTemp");
            getByTemperature.Parameter<int>("maxTemp");

            return builder.GetEdmModel();
        }
    }

    /// <summary>
    /// Sample Product entity for v1 API.
    /// </summary>
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}
