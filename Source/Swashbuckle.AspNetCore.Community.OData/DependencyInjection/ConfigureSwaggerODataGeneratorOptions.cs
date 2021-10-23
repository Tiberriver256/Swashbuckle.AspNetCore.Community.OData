namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.OData;
    using Microsoft.OData.Edm;
    using Microsoft.OpenApi.Models;
    using System.Linq;

    /// <summary>
    /// The service used for configuring <see cref="SwaggerODataGeneratorOptions"/>.
    /// </summary>
    public class ConfigureSwaggerODataGeneratorOptions : IConfigureOptions<SwaggerODataGeneratorOptions>
    {
        private readonly ODataOptions odataOptions;
        private readonly SwaggerGenODataOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureSwaggerODataGeneratorOptions"/> class.
        /// </summary>
        /// <param name="opts">Options injected.</param>
        public ConfigureSwaggerODataGeneratorOptions(IOptions<SwaggerGenODataOptions> opts, IOptions<ODataOptions> odataOpts)
        {
            this.odataOptions = odataOpts.Value;
            this.options = opts.Value;
        }

        /// <summary>
        /// Clones the options provided in SwaggerGenODataOptions into an instance of SwaggerGeneratorODataOptions.
        /// </summary>
        /// <param name="generatorOptions">SwaggerGeneratorODataOptions.</param>
        public void Configure(SwaggerODataGeneratorOptions generatorOptions)
        {
            DeepCopy(this.options.SwaggerGeneratorODataOptions, generatorOptions);

            if (generatorOptions.EdmModels != null && !generatorOptions.EdmModels.Any())
            {
                foreach (var route in this.odataOptions.RouteComponents)
                {
                    generatorOptions.EdmModels.Add(route.Key, route.Value.EdmModel);
                }
            }
        }

        private static void DeepCopy(SwaggerODataGeneratorOptions source, SwaggerODataGeneratorOptions target)
        {
            target.SwaggerDocs = new Dictionary<string, (string, OpenApiInfo)>(source.SwaggerDocs);
            target.EdmModels = new Dictionary<string, IEdmModel>(source.EdmModels);
        }
    }
}
