using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Community.OData.OpenApi;

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
            this.QueryOptionsSettings = new ODataQueryOptionsSettings();
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

        /// <summary>
        /// Gets or sets settings for generated OData query options.
        /// </summary>
        public ODataQueryOptionsSettings QueryOptionsSettings { get; set; }
    }
}
