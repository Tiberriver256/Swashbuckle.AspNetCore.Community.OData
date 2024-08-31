using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection
{
    /// <summary>
    /// Public static methods for injecting an ISwaggerProvider OData implementation into the service collection.
    /// </summary>
    public static class SwaggerGenODataServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Swagger support for your OData endpoints.
        /// </summary>
        /// <param name="services">The service collection to inject services into.</param>
        /// <param name="setupAction">Configuration for your.</param>
        /// <returns>A service collection.</returns>
        public static IServiceCollection AddSwaggerGenOData(
            this IServiceCollection services,
            Action<SwaggerGenODataOptions> setupAction
        )
        {
            services.AddTransient<
                IConfigureOptions<SwaggerODataGeneratorOptions>,
                ConfigureSwaggerODataGeneratorOptions
            >();

            services.TryAddTransient<ISwaggerProvider, SwaggerODataGenerator>();

            services.TryAddTransient(s =>
                s.GetRequiredService<IOptions<SwaggerODataGeneratorOptions>>().Value
            );

            if (setupAction != null)
            {
                services.ConfigureSwaggerODataGen(setupAction);
            }

            return services;
        }

        /// <summary>
        /// Configures options for configuring your OData Swagger.
        /// </summary>
        /// <param name="services">The service collection to modify.</param>
        /// <param name="setupAction">The actions to perform.</param>
        public static void ConfigureSwaggerODataGen(
            this IServiceCollection services,
            Action<SwaggerGenODataOptions> setupAction
        ) => services.Configure(setupAction);
    }
}
