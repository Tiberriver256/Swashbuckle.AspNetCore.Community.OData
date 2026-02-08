// -----------------------------------------------------------------------------
// <copyright file="ODataQueryOptionsDocumentFilterTests.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var singleProductPath = document.Paths["/Products({key})"];
            var getOperation = singleProductPath.Operations[OperationType.Get];

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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

            var topParam = GetSingleParameter(getOperation, "$top");
            Assert.AreEqual("integer", topParam.Schema.Type);
            Assert.AreEqual("int32", topParam.Schema.Format);
            Assert.AreEqual(100m, topParam.Schema.Maximum);

            var countParam = GetSingleParameter(getOperation, "$count");
            Assert.AreEqual("boolean", countParam.Schema.Type);

            var filterParam = GetSingleParameter(getOperation, "$filter");
            Assert.AreEqual("string", filterParam.Schema.Type);
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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

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
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

            // Add an existing $filter parameter
            getOperation.Parameters.Add(new OpenApiParameter
            {
                Name = "$filter",
                In = ParameterLocation.Query,
                Schema = new OpenApiSchema { Type = "string" }
            });

            var filter = new ODataQueryOptionsDocumentFilter();
            var context = new DocumentFilterContext(new List<ApiDescription>(), null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            Assert.AreEqual(1, getOperation.Parameters.Count(p => p.Name == "$filter"));
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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

            var formatParam = GetSingleParameter(getOperation, "$format");
            Assert.IsNotNull(formatParam.Schema.Enum);
            Assert.IsTrue(formatParam.Schema.Enum.Count > 0);
        }

        [TestMethod]
        public void Apply_SkipsNonGetOperations()
        {
            // Arrange
            var filter = new ODataQueryOptionsDocumentFilter();
            var document = CreateTestDocument();
            var productsPath = document.Paths["/Products"];

            // Add a POST operation
            productsPath.Operations[OperationType.Post] = new OpenApiOperation
            {
                Summary = "Create Product",
                Parameters = new List<OpenApiParameter>()
            };

            var context = new DocumentFilterContext(new List<ApiDescription>(), null!, null!);

            // Act
            filter.Apply(document, context);

            // Assert - POST should not have query parameters
            var postOperation = productsPath.Operations[OperationType.Post];
            Assert.IsFalse(postOperation.Parameters.Any(p => p.Name.StartsWith('$')));
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

        private OpenApiDocument CreateTestDocument()
        {
            return new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
                Paths = new OpenApiPaths
                {
                    ["/Products"] = new OpenApiPathItem
                    {
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            [OperationType.Get] = new OpenApiOperation
                            {
                                Summary = "Get Products",
                                Parameters = new List<OpenApiParameter>(),
                                Responses = new OpenApiResponses
                                {
                                    ["200"] = new OpenApiResponse
                                    {
                                        Description = "Success",
                                        Content = new Dictionary<string, OpenApiMediaType>
                                        {
                                            ["application/json"] = new OpenApiMediaType
                                            {
                                                Schema = new OpenApiSchema
                                                {
                                                    Type = "array",
                                                    Items = new OpenApiSchema { Type = "object" }
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
                        Operations = new Dictionary<OperationType, OpenApiOperation>
                        {
                            [OperationType.Get] = new OpenApiOperation
                            {
                                Summary = "Get Product",
                                Parameters = new List<OpenApiParameter>(),
                                Responses = new OpenApiResponses
                                {
                                    ["200"] = new OpenApiResponse { Description = "Success" }
                                }
                            }
                        }
                    }
                }
            };
        }

        private static void AssertHasParameter(OpenApiOperation operation, string parameterName)
        {
            Assert.IsTrue(operation.Parameters.Any(p => p.Name == parameterName), $"Expected parameter '{parameterName}' to be present.");
        }

        private static void AssertDoesNotHaveParameter(OpenApiOperation operation, string parameterName)
        {
            Assert.IsFalse(operation.Parameters.Any(p => p.Name == parameterName), $"Expected parameter '{parameterName}' to be absent.");
        }

        private static OpenApiParameter GetSingleParameter(OpenApiOperation operation, string parameterName)
        {
            var matches = operation.Parameters.Where(p => p.Name == parameterName).ToList();
            Assert.AreEqual(1, matches.Count, $"Expected exactly one '{parameterName}' parameter.");
            return matches[0];
        }

        #endregion
    }
}
