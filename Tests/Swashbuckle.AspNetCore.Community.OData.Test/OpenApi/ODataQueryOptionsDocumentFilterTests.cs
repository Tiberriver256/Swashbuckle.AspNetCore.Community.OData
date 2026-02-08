// -----------------------------------------------------------------------------
// <copyright file="ODataQueryOptionsDocumentFilterTests.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.OpenApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Swashbuckle.AspNetCore.Community.OData.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.AspNetCore.Community.OData.Tests.OpenApi
{
    /// <summary>
    /// Tests for the ODataQueryOptionsDocumentFilter.
    /// </summary>
    [TestClass]
    public class ODataQueryOptionsDocumentFilterTests
    {
        [TestMethod]
        public void Apply_AddsODataQueryParameters_ToCollectionEndpoints()
        {
            // Arrange
            var filter = new ODataQueryOptionsDocumentFilter();
            var document = CreateTestDocument();
            var context = new DocumentFilterContext([], null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var getOperation = GetRequiredOperation(document, "/Products", HttpMethod.Get);

            AssertHasParameter(getOperation, "$filter");
            AssertHasParameter(getOperation, "$select");
            AssertHasParameter(getOperation, "$expand");
            AssertHasParameter(getOperation, "$orderby");
            AssertHasParameter(getOperation, "$top");
            AssertHasParameter(getOperation, "$skip");
            AssertHasParameter(getOperation, "$count");
        }

        [TestMethod]
        public void Apply_DoesNotAddParameters_ToSingleEntityEndpoints()
        {
            // Arrange
            var filter = new ODataQueryOptionsDocumentFilter();
            var document = CreateTestDocument();
            var context = new DocumentFilterContext([], null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var getOperation = GetRequiredOperation(document, "/Products({key})", HttpMethod.Get);

            AssertDoesNotHaveParameter(getOperation, "$filter");
            AssertDoesNotHaveParameter(getOperation, "$top");
            AssertDoesNotHaveParameter(getOperation, "$skip");
        }

        [TestMethod]
        public void Apply_WithDisabledSettings_DoesNotAddParameters()
        {
            // Arrange
            var settings = new ODataQueryOptionsSettings
            {
                EnableFilter = false,
                EnableSelect = false,
                EnableExpand = false
            };
            var filter = new ODataQueryOptionsDocumentFilter(settings);
            var document = CreateTestDocument();
            var context = new DocumentFilterContext([], null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var getOperation = GetRequiredOperation(document, "/Products", HttpMethod.Get);

            AssertDoesNotHaveParameter(getOperation, "$filter");
            AssertDoesNotHaveParameter(getOperation, "$select");
            AssertDoesNotHaveParameter(getOperation, "$expand");
        }

        [TestMethod]
        public void Apply_AddsCorrectParameterTypes()
        {
            // Arrange
            var filter = new ODataQueryOptionsDocumentFilter();
            var document = CreateTestDocument();
            var context = new DocumentFilterContext([], null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var getOperation = GetRequiredOperation(document, "/Products", HttpMethod.Get);

            var topParam = GetSingleParameter(getOperation, "$top");
            var topSchema = GetRequiredSchema(topParam);
            Assert.AreEqual(JsonSchemaType.Integer, topSchema.Type);
            Assert.AreEqual("int32", topSchema.Format);
            Assert.AreEqual("100", topSchema.Maximum);

            var countParam = GetSingleParameter(getOperation, "$count");
            Assert.AreEqual(JsonSchemaType.Boolean, GetRequiredSchema(countParam).Type);

            var filterParam = GetSingleParameter(getOperation, "$filter");
            Assert.AreEqual(JsonSchemaType.String, GetRequiredSchema(filterParam).Type);
            Assert.AreEqual(ParameterLocation.Query, filterParam.In);
        }

        [TestMethod]
        public void Apply_AddsExamplesToParameters()
        {
            // Arrange
            var settings = new ODataQueryOptionsSettings
            {
                FilterExample = "Name eq 'Test'",
                SelectExample = "Name,Price",
                ExpandExample = "Category"
            };
            var filter = new ODataQueryOptionsDocumentFilter(settings);
            var document = CreateTestDocument();
            var context = new DocumentFilterContext([], null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var getOperation = GetRequiredOperation(document, "/Products", HttpMethod.Get);

            var filterParam = GetSingleParameter(getOperation, "$filter");
            Assert.IsNotNull(filterParam.Example);

            var selectParam = GetSingleParameter(getOperation, "$select");
            Assert.IsNotNull(selectParam.Example);
        }

        [TestMethod]
        public void Apply_DoesNotDuplicateParameters()
        {
            // Arrange - Document with existing parameters
            var document = CreateTestDocument();
            var getOperation = GetRequiredOperation(document, "/Products", HttpMethod.Get);

            // Add an existing $filter parameter
            GetRequiredParameters(getOperation).Add(new OpenApiParameter
            {
                Name = "$filter",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String }
            });

            var filter = new ODataQueryOptionsDocumentFilter();
            var context = new DocumentFilterContext([], null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            Assert.ContainsSingle(p => p.Name == "$filter", GetRequiredParameters(getOperation));
        }

        [TestMethod]
        public void Apply_WithSearchEnabled_AddsSearchParameter()
        {
            // Arrange
            var settings = new ODataQueryOptionsSettings
            {
                EnableSearch = true
            };
            var filter = new ODataQueryOptionsDocumentFilter(settings);
            var document = CreateTestDocument();
            var context = new DocumentFilterContext([], null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var getOperation = GetRequiredOperation(document, "/Products", HttpMethod.Get);

            AssertHasParameter(getOperation, "$search");
        }

        [TestMethod]
        public void Apply_WithFormatEnabled_AddsFormatParameter()
        {
            // Arrange
            var settings = new ODataQueryOptionsSettings
            {
                EnableFormat = true
            };
            var filter = new ODataQueryOptionsDocumentFilter(settings);
            var document = CreateTestDocument();
            var context = new DocumentFilterContext([], null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var getOperation = GetRequiredOperation(document, "/Products", HttpMethod.Get);

            var formatParam = GetSingleParameter(getOperation, "$format");
            var formatSchema = GetRequiredSchema(formatParam);
            Assert.IsNotNull(formatSchema.Enum);
            Assert.IsNotEmpty(formatSchema.Enum);
        }

        [TestMethod]
        public void Apply_SkipsNonGetOperations()
        {
            // Arrange
            var filter = new ODataQueryOptionsDocumentFilter();
            var document = CreateTestDocument();
            var productsPath = GetRequiredPathItem(document, "/Products");

            // Add a POST operation
            var operations = productsPath.Operations;
            Assert.IsNotNull(operations);
            operations[HttpMethod.Post] = new OpenApiOperation
            {
                Summary = "Create Product",
                Parameters = []
            };

            var context = new DocumentFilterContext([], null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert - POST should not have query parameters
            var postOperation = GetRequiredOperation(document, "/Products", HttpMethod.Post);
            Assert.DoesNotContain(p => p.Name?.StartsWith('$') == true, GetRequiredParameters(postOperation));
        }

        [TestMethod]
        public void Settings_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var settings = new ODataQueryOptionsSettings();

            // Assert
            Assert.IsTrue(settings.EnableFilter);
            Assert.IsTrue(settings.EnableSelect);
            Assert.IsTrue(settings.EnableExpand);
            Assert.IsTrue(settings.EnableOrderBy);
            Assert.IsTrue(settings.EnableTop);
            Assert.IsTrue(settings.EnableSkip);
            Assert.IsTrue(settings.EnableCount);
            Assert.IsFalse(settings.EnableSearch);
            Assert.IsTrue(settings.EnableFormat);
            Assert.IsTrue(settings.EnablePagination);
            Assert.AreEqual(100, settings.MaxTop);
            Assert.AreEqual(50, settings.DefaultTop);
        }

        #region Helper Methods

        private static OpenApiDocument CreateTestDocument() => new()
        {
            Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
            Paths = new OpenApiPaths
            {
                ["/Products"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Summary = "Get Products",
                            Parameters = [],
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Description = "Success",
                                    Content = new Dictionary<string, IOpenApiMediaType>
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = JsonSchemaType.Array,
                                                Items = new OpenApiSchema { Type = JsonSchemaType.Object }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ["/Products({key})"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Summary = "Get Product",
                            Parameters = [],
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse { Description = "Success" }
                            }
                        }
                    }
                }
            }
        };

        private static IOpenApiPathItem GetRequiredPathItem(OpenApiDocument document, string path)
        {
            Assert.IsTrue(document.Paths.TryGetValue(path, out var pathItem));
            Assert.IsNotNull(pathItem);
            return pathItem;
        }

        private static OpenApiOperation GetRequiredOperation(OpenApiDocument document, string path, HttpMethod method)
        {
            var pathItem = GetRequiredPathItem(document, path);
            var operations = pathItem.Operations;
            Assert.IsNotNull(operations);
            Assert.IsTrue(operations.TryGetValue(method, out var operation));
            Assert.IsNotNull(operation);
            return operation;
        }

        private static IList<IOpenApiParameter> GetRequiredParameters(OpenApiOperation operation)
        {
            Assert.IsNotNull(operation.Parameters);
            return operation.Parameters;
        }

        private static IOpenApiSchema GetRequiredSchema(IOpenApiParameter parameter)
        {
            Assert.IsNotNull(parameter.Schema);
            return parameter.Schema;
        }

        private static void AssertHasParameter(OpenApiOperation operation, string parameterName) =>
            Assert.Contains(p => p.Name == parameterName, GetRequiredParameters(operation), $"Expected parameter '{parameterName}' to be present.");

        private static void AssertDoesNotHaveParameter(OpenApiOperation operation, string parameterName) =>
            Assert.DoesNotContain(p => p.Name == parameterName, GetRequiredParameters(operation), $"Expected parameter '{parameterName}' to be absent.");

        private static IOpenApiParameter GetSingleParameter(OpenApiOperation operation, string parameterName) =>
            Assert.ContainsSingle(p => p.Name == parameterName, GetRequiredParameters(operation), $"Expected exactly one '{parameterName}' parameter.");

        #endregion
    }
}
