// -----------------------------------------------------------------------------
// <copyright file="EnhancedSwaggerGenODataServiceCollectionExtensions.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Community.OData.OpenApi;
using Swashbuckle.AspNetCore.Community.OData.SwaggerODataGenerator;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection
{
    /// <summary>
    /// Enhanced extension methods for adding OData Swagger support with endpoint routing integration.
    /// </summary>
    public static class EnhancedSwaggerGenODataServiceCollectionExtensions
    {
        /// <summary>
        /// Adds enhanced Swagger support for OData that uses actual endpoint routing data
        /// to produce accurate OpenAPI documentation with full OData query support.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="setupAction">Configuration action for OData Swagger options.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddEnhancedSwaggerGenOData(
            this IServiceCollection services,
            Action<SwaggerGenODataOptions>? setupAction = null)
        {
            // Register options
            services.AddOptions<SwaggerGenODataOptions>();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            // Register the options configuration
            services.AddTransient<
                IConfigureOptions<SwaggerODataGeneratorOptions>,
                ConfigureSwaggerODataGeneratorOptions
            >();

            // Register the enhanced generator
            services.TryAddTransient<ISwaggerProvider, EnhancedSwaggerODataGenerator>();

            // Register transient access to options
            services.TryAddTransient(s =>
                s.GetRequiredService<IOptions<SwaggerODataGeneratorOptions>>().Value
            );

            // Ensure EndpointDataSource is available
            services.TryAddSingleton<EndpointDataSource>(sp =>
            {
                // Get the endpoint data source from the routing services
                var compositeDataSource = sp.GetService<CompositeEndpointDataSource>();
                return compositeDataSource ?? new CompositeEndpointDataSource([]);
            });

            return services;
        }

        /// <summary>
        /// Adds enhanced Swagger support for OData with automatic OData query options documentation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="odataSetupAction">Configuration for OData Swagger options.</param>
        /// <param name="queryOptionsSettings">Settings for OData query options documentation.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddEnhancedSwaggerGenODataWithQueryOptions(
            this IServiceCollection services,
            Action<SwaggerGenODataOptions>? odataSetupAction = null,
            ODataQueryOptionsSettings? queryOptionsSettings = null)
        {
            var resolvedQueryOptionsSettings = queryOptionsSettings ?? new ODataQueryOptionsSettings();

            // Add the enhanced OData Swagger generator
            services.AddEnhancedSwaggerGenOData(odataSetupAction);

            // Wire query option settings into the OData generator options.
            services.Configure<SwaggerGenODataOptions>(options => options.SwaggerGeneratorODataOptions.QueryOptionsSettings = resolvedQueryOptionsSettings);

            // Add OData query options document filter through SwaggerGen
            services.AddSwaggerGen(swaggerOptions => swaggerOptions.DocumentFilter<ODataQueryOptionsDocumentFilter>(resolvedQueryOptionsSettings));

            return services;
        }

        /// <summary>
        /// Configures SwaggerGen to use OData-specific document filters and operation filters.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureSwaggerGen">Additional SwaggerGen configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddODataSwaggerGen(
            this IServiceCollection services,
            Action<SwaggerGenOptions>? configureSwaggerGen = null)
        {
            services.AddSwaggerGen(options =>
            {
                // Add OData query options document filter
                options.DocumentFilter<ODataQueryOptionsDocumentFilter>();

                // Add any custom configuration
                configureSwaggerGen?.Invoke(options);
            });

            return services;
        }

        /// <summary>
        /// Adds Swagger support for OData and configures the pipeline with all OData features.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Configuration for OData Swagger options.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddSwaggerGenForOData(
            this IServiceCollection services,
            Action<ODataSwaggerGenOptions>? configureOptions = null)
        {
            var options = new ODataSwaggerGenOptions();
            configureOptions?.Invoke(options);

            // Add enhanced OData Swagger generator
            services.AddEnhancedSwaggerGenOData(opt =>
            {
                foreach (var doc in options.SwaggerDocs)
                {
                    opt.SwaggerDoc(doc.Key, doc.Value.Route, doc.Value.Info);
                }
            });

            services.Configure<SwaggerGenODataOptions>(swaggerOptions => swaggerOptions.SwaggerGeneratorODataOptions.QueryOptionsSettings = options.QueryOptionsSettings);

            // Add SwaggerGen with OData filters
            services.AddSwaggerGen(swaggerOptions =>
            {
                // Add OData query options
                swaggerOptions.DocumentFilter<ODataQueryOptionsDocumentFilter>(options.QueryOptionsSettings);

                // Apply any additional configuration
                options.ConfigureSwaggerGen?.Invoke(swaggerOptions);
            });

            return services;
        }
    }

    /// <summary>
    /// Configuration options for OData Swagger generation.
    /// </summary>
    public class ODataSwaggerGenOptions
    {
        /// <summary>
        /// Swagger documents to generate.
        /// </summary>
        internal Dictionary<string, (string Route, OpenApiInfo Info)> SwaggerDocs { get; } = [];

        /// <summary>
        /// Settings for OData query options.
        /// </summary>
        public ODataQueryOptionsSettings QueryOptionsSettings { get; set; } = new();

        /// <summary>
        /// Additional SwaggerGen configuration.
        /// </summary>
        public Action<SwaggerGenOptions>? ConfigureSwaggerGen { get; set; }

        /// <summary>
        /// Adds a Swagger document for an OData route.
        /// </summary>
        /// <param name="name">Document name.</param>
        /// <param name="odataRoute">OData route prefix.</param>
        /// <param name="info">API info.</param>
        /// <returns>This options instance.</returns>
        public ODataSwaggerGenOptions AddDocument(string name, string odataRoute, OpenApiInfo info)
        {
            this.SwaggerDocs[name] = (odataRoute, info);
            return this;
        }
    }
}
