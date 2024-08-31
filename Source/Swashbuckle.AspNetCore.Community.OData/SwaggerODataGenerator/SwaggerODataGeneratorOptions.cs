using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;

namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection
{
    /// <summary>
    /// Options required to generate Swagger from your OData API.
    /// </summary>
    public class SwaggerODataGeneratorOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwaggerODataGeneratorOptions"/> class.
        /// </summary>
        public SwaggerODataGeneratorOptions()
        {
            this.SwaggerDocs = new Dictionary<string, (string, OpenApiInfo)>();
            this.EdmModels = new Dictionary<string, IEdmModel>();
        }

        /// <summary>
        /// Gets or sets swagger documents that have been parsed from the OData Edm Models.
        /// </summary>
        public IDictionary<
            string,
            (string OdataRoute, OpenApiInfo ApiInfo)
        > SwaggerDocs { get; set; }

        /// <summary>
        /// Gets or sets OData Edm Models.
        /// </summary>
        public IDictionary<string, IEdmModel> EdmModels { get; set; }
    }
}
