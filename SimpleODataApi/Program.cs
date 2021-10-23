using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using SimpleODataApi;
using Swashbuckle.AspNetCore.Community.OData.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllers()
    .AddOData(o => o.AddRouteComponents("odata", GetEdmModel()));

builder.Services.AddSwaggerGenOData(opt => opt.SwaggerDoc("v1", "odata", new OpenApiInfo { Version = "V1", Title = "My OData API" }));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

app.UseSwagger();

app.MapControllers();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "My OData API");
});

app.Run();

static IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();

    builder.EntityType<WeatherForecast>()
        .HasKey(f => f.Id);
    builder.EntityType<WeatherForecast>()
        .HasDeleteRestrictions().HasDescription("Not supported");
    builder.EntityType<WeatherForecast>()
        .HasUpdateRestrictions().HasDescription("Not supported");
    builder.EntityType<WeatherForecast>()
        .HasInsertRestrictions().HasDescription("Not supported");

    builder.EntitySet<WeatherForecast>("WeatherForecasts");

    return builder.GetEdmModel();
}
