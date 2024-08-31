namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection
{
    /// <summary>
    /// Dependency Injection options for SwaggerGenOdata.
    /// </summary>
    public class SwaggerGenODataOptions
    {
        /// <summary>
        /// Gets or sets the SwaggerGeneratorODataOptions.
        /// </summary>
        public SwaggerODataGeneratorOptions SwaggerGeneratorODataOptions { get; set; } =
            new SwaggerODataGeneratorOptions();
    }
}
