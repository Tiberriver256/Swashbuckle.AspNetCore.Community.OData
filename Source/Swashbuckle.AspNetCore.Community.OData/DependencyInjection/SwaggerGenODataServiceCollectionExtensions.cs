using System;
using Microsoft.Extensions.DependencyInjection;

namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection
{
    /// <summary>
    /// Legacy extension methods preserved as compatibility shims.
    /// </summary>
    public static class SwaggerGenODataServiceCollectionExtensions
    {
        /// <summary>
        /// Adds OData Swagger support using the enhanced generator pipeline.
        /// </summary>
        /// <param name="services">The service collection to inject services into.</param>
        /// <param name="setupAction">Configuration for OData Swagger options.</param>
        /// <returns>A service collection.</returns>
        [Obsolete("AddSwaggerGenOData is legacy and will be removed in v3. Use AddEnhancedSwaggerGenOData instead.")]
        public static IServiceCollection AddSwaggerGenOData(
            this IServiceCollection services,
            Action<SwaggerGenODataOptions> setupAction
        )
        {
            services.AddEnhancedSwaggerGenOData(setupAction);
            return services;
        }

        /// <summary>
        /// Configures options for OData Swagger generation.
        /// </summary>
        /// <param name="services">The service collection to modify.</param>
        /// <param name="setupAction">The actions to perform.</param>
        [Obsolete("ConfigureSwaggerODataGen is legacy and will be removed in v3. Configure via AddEnhancedSwaggerGenOData instead.")]
        public static void ConfigureSwaggerODataGen(
            this IServiceCollection services,
            Action<SwaggerGenODataOptions> setupAction
        ) => services.Configure(setupAction);
    }
}
