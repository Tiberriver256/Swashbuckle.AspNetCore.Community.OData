using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;

namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection
{
    /// <summary>
    /// The service used for configuring <see cref="SwaggerODataGeneratorOptions"/>.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ConfigureSwaggerODataGeneratorOptions"/> class.
    /// </remarks>
    /// <param name="opts">Options injected.</param>
    /// <param name="odataOpts">OData options</param>
    public class ConfigureSwaggerODataGeneratorOptions(
        IOptions<SwaggerGenODataOptions> opts,
        IOptions<ODataOptions> odataOpts
    ) : IConfigureOptions<SwaggerODataGeneratorOptions>
    {
        private readonly ODataOptions odataOptions = odataOpts.Value;
        private readonly SwaggerGenODataOptions options = opts.Value;

        /// <summary>
        /// Clones the options provided in SwaggerGenODataOptions into an instance of SwaggerGeneratorODataOptions.
        /// </summary>
        /// <param name="options">SwaggerGeneratorODataOptions.</param>
        public void Configure(SwaggerODataGeneratorOptions options)
        {
            DeepCopy(this.options.SwaggerGeneratorODataOptions, options);

            if (options.EdmModels != null && !options.EdmModels.Any())
            {
                foreach (var route in this.odataOptions.RouteComponents)
                {
                    options.EdmModels.Add(route.Key, route.Value.EdmModel);
                }
            }
        }

        private static void DeepCopy(
            SwaggerODataGeneratorOptions source,
            SwaggerODataGeneratorOptions target
        )
        {
            target.SwaggerDocs = new Dictionary<string, (string, OpenApiInfo)>(source.SwaggerDocs);
            target.EdmModels = new Dictionary<string, IEdmModel>(source.EdmModels);
            target.QueryOptionsSettings = source.QueryOptionsSettings;
        }
    }
}
