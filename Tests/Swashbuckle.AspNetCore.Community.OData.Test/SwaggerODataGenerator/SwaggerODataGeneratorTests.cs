using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Swashbuckle.AspNetCore.Community.OData.DependencyInjection.Tests
{
    [TestClass]
    public class SwaggerODataGeneratorTests
    {
        [TestMethod]
        public void GetSwagger_GeneratesSwaggerDocument_ForApiDescriptionsWithMatchingGroupName()
        {
            // Arrange
            var swaggerGeneratorODataOptions = new SwaggerODataGeneratorOptions();
            swaggerGeneratorODataOptions.SwaggerDocs.Add(
                "v1",
                ("odata", new OpenApiInfo { Version = "V1", Title = "Test API" })
            );
            swaggerGeneratorODataOptions.EdmModels.Add("odata", GetFakeEdmModel());

            var subject = new SwaggerODataGenerator(
                options: Options.Create(swaggerGeneratorODataOptions)
            );

            // Act
            var document = subject.GetSwagger("v1");

            // Assert
            Assert.AreEqual("V1", document.Info.Version);
            Assert.AreEqual("Test API", document.Info.Title);
            Assert.Contains("/People", document.Paths.Keys);
            Assert.Contains("/People({Id})", document.Paths.Keys);
        }

        private static IEdmModel GetFakeEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            builder.EntitySet<Person>("People");

            return builder.GetEdmModel();
        }

        private sealed class Person
        {
            public int Id { get; set; }

            public string? Name { get; set; }
        }
    }
}
