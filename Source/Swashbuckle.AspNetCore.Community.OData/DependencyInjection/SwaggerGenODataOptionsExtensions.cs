using Microsoft.OpenApi.Models;

namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection
{
    /// <summary>
    /// Static methods for working with <see cref="SwaggerGenODataOptions"/> class.
    /// </summary>
    public static class SwaggerGenODataOptionsExtensions
    {
        /// <summary>
        /// Define one or more EdmModels to be created by the Swagger generator.
        /// </summary>
        /// <param name="swaggerGenODataOptions">The options to modify.</param>
        /// <param name="name">A URI-friendly name that uniquely identifies the document.</param>
        /// <param name="odataRoute">The OData routes this information is associated with.</param>
        /// <param name="info">Global metadata to be included in the Swagger output.</param>
        public static void SwaggerDoc(
            this SwaggerGenODataOptions swaggerGenODataOptions,
            string name,
            string odataRoute,
            OpenApiInfo info
        ) =>
            swaggerGenODataOptions.SwaggerGeneratorODataOptions.SwaggerDocs.Add(
                name,
                (odataRoute, info)
            );
    }
}
