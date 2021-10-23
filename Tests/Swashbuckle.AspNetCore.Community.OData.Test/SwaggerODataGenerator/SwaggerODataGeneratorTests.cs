namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection.Tests
{
    using FluentAssertions;
    using Microsoft.Extensions.Options;
    using Microsoft.OData.Edm;
    using Microsoft.OData.ModelBuilder;
    using Microsoft.OpenApi.Models;
    using Xunit;

    public class SwaggerODataGeneratorTests
    {
        [Fact]
        public void GetSwagger_GeneratesSwaggerDocument_ForApiDescriptionsWithMatchingGroupName()
        {
            // Arrange
            var swaggerGeneratorODataOptions = new SwaggerODataGeneratorOptions();
            swaggerGeneratorODataOptions.SwaggerDocs.Add("v1", ("odata", new OpenApiInfo { Version = "V1", Title = "Test API" }));
            swaggerGeneratorODataOptions.EdmModels.Add("odata", GetFakeEdmModel());

            var subject = new SwaggerODataGenerator(options: Options.Create(swaggerGeneratorODataOptions));

            // Act
            var document = subject.GetSwagger("v1");

            // Assert
            document.Info.Version.Should().Be("V1");
            document.Info.Title.Should().Be("Test API");
            document.Paths.Keys.Should().Contain("/People");
            document.Paths.Keys.Should().Contain("/People({Id})");
        }

        private static IEdmModel GetFakeEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            builder.EntitySet<Person>("People");

            return builder.GetEdmModel();
        }

        private class Person
        {
            public int Id { get; set; }

            public string? Name { get; set; }
        }
    }
}
