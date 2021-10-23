using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Community.OData.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleOdataApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers().AddOData(o => o.AddRouteComponents("odata", GetEdmModel()));
            services.AddSwaggerGenOData(opt => opt.SwaggerDoc("v1", "odata", new OpenApiInfo
            {
                Title = "My Open API",
                Version = "v1",
            }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseSwagger();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "My OData API");
            });
        }

        private static IEdmModel GetEdmModel()
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
    }
}
