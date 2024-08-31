using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.OData;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection
{
    /// <summary>
    /// The Swagger generator for OData Edm Models.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SwaggerODataGenerator"/> class.
    /// </remarks>
    /// <param name="options">The options configured during startup.</param>
    public class SwaggerODataGenerator(IOptions<SwaggerODataGeneratorOptions> options) : ISwaggerProvider
    {
        /// <summary>
        /// Options for the swagger generator.
        /// </summary>
        private readonly SwaggerODataGeneratorOptions options = options.Value ?? new SwaggerODataGeneratorOptions();

        /// <inheritdoc/>
        public OpenApiDocument GetSwagger(string documentName, string? host = null, string? basePath = null)
        {
            if (!this.options.SwaggerDocs.TryGetValue(documentName, out var info))
            {
                throw new UnknownSwaggerDocument(documentName, this.options.SwaggerDocs.Select(d => d.Key));
            }

            if (!this.options.EdmModels.TryGetValue(info.OdataRoute, out var edmModel))
            {
                throw new UnknownODataEdm(info.OdataRoute, this.options.EdmModels.Select(d => d.Key));
            }

            var document = edmModel.ConvertToOpenApi();
            document.Info = info.ApiInfo;
            document.Servers = [new() { Url = $"/{info.OdataRoute}" }];

            return document;
        }

        private sealed class UnknownODataEdm(string documentName, IEnumerable<string> knownEdmModels) : InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unknown EdmModel document - \"{0}\". Known EdmModels: {1}", documentName, string.Join(",", knownEdmModels.Select((string x) => "\"" + x + "\""))))
        {
        }
    }
}
