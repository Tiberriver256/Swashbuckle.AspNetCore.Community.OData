// -----------------------------------------------------------------------------
// <copyright file="ODataQueryOptionsDocumentFilterTests.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Community.OData.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Swashbuckle.AspNetCore.Community.OData.Tests.OpenApi
{
    /// <summary>
    /// Tests for the ODataQueryOptionsDocumentFilter.
    /// </summary>
    public class ODataQueryOptionsDocumentFilterTests
    {
        [Fact]
        public void Apply_AddsODataQueryParameters_ToCollectionEndpoints()
        {
            // Arrange
            var filter = new ODataQueryOptionsDocumentFilter();
            var document = CreateTestDocument();
            var context = new DocumentFilterContext(new List<ApiDescription>(), null, null);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

            getOperation.Parameters.Should().Contain(p => p.Name == "$filter");
            getOperation.Parameters.Should().Contain(p => p.Name == "$select");
            getOperation.Parameters.Should().Contain(p => p.Name == "$expand");
            getOperation.Parameters.Should().Contain(p => p.Name == "$orderby");
            getOperation.Parameters.Should().Contain(p => p.Name == "$top");
            getOperation.Parameters.Should().Contain(p => p.Name == "$skip");
            getOperation.Parameters.Should().Contain(p => p.Name == "$count");
        }

        [Fact]
        public void Apply_DoesNotAddParameters_ToSingleEntityEndpoints()
        {
            // Arrange
            var filter = new ODataQueryOptionsDocumentFilter();
            var document = CreateTestDocument();
            var context = new DocumentFilterContext(new List<ApiDescription>(), null, null);

            // Act
            filter.Apply(document, context);

            // Assert
            var singleProductPath = document.Paths["/Products({key})"];
            var getOperation = singleProductPath.Operations[OperationType.Get];

            getOperation.Parameters.Should().NotContain(p => p.Name == "$filter");
            getOperation.Parameters.Should().NotContain(p => p.Name == "$top");
            getOperation.Parameters.Should().NotContain(p => p.Name == "$skip");
        }

        [Fact]
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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null, null);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

            getOperation.Parameters.Should().NotContain(p => p.Name == "$filter");
            getOperation.Parameters.Should().NotContain(p => p.Name == "$select");
            getOperation.Parameters.Should().NotContain(p => p.Name == "$expand");
        }

        [Fact]
        public void Apply_AddsCorrectParameterTypes()
        {
            // Arrange
            var filter = new ODataQueryOptionsDocumentFilter();
            var document = CreateTestDocument();
            var context = new DocumentFilterContext(new List<ApiDescription>(), null, null);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

            var topParam = getOperation.Parameters.Should().ContainSingle(p => p.Name == "$top").Subject;
            topParam.Schema.Type.Should().Be("integer");
            topParam.Schema.Format.Should().Be("int32");
            topParam.Schema.Maximum.Should().Be(100);

            var countParam = getOperation.Parameters.Should().ContainSingle(p => p.Name == "$count").Subject;
            countParam.Schema.Type.Should().Be("boolean");

            var filterParam = getOperation.Parameters.Should().ContainSingle(p => p.Name == "$filter").Subject;
            filterParam.Schema.Type.Should().Be("string");
            filterParam.In.Should().Be(ParameterLocation.Query);
        }

        [Fact]
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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null, null);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

            var filterParam = getOperation.Parameters.Should().ContainSingle(p => p.Name == "$filter").Subject;
            filterParam.Example.Should().NotBeNull();

            var selectParam = getOperation.Parameters.Should().ContainSingle(p => p.Name == "$select").Subject;
            selectParam.Example.Should().NotBeNull();
        }

        [Fact]
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
            var context = new DocumentFilterContext(new List<ApiDescription>(), null, null);

            // Act
            filter.Apply(document, context);

            // Assert
            getOperation.Parameters.Where(p => p.Name == "$filter").Should().HaveCount(1);
        }

        [Fact]
        public void Apply_WithSearchEnabled_AddsSearchParameter()
        {
            // Arrange
            var settings = new ODataQueryOptionsSettings
            {
                EnableSearch = true
            };
            var filter = new ODataQueryOptionsDocumentFilter(settings);
            var document = CreateTestDocument();
            var context = new DocumentFilterContext(new List<ApiDescription>(), null, null);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

            getOperation.Parameters.Should().Contain(p => p.Name == "$search");
        }

        [Fact]
        public void Apply_WithFormatEnabled_AddsFormatParameter()
        {
            // Arrange
            var settings = new ODataQueryOptionsSettings
            {
                EnableFormat = true
            };
            var filter = new ODataQueryOptionsDocumentFilter(settings);
            var document = CreateTestDocument();
            var context = new DocumentFilterContext(new List<ApiDescription>(), null, null);

            // Act
            filter.Apply(document, context);

            // Assert
            var productsPath = document.Paths["/Products"];
            var getOperation = productsPath.Operations[OperationType.Get];

            var formatParam = getOperation.Parameters.Should().ContainSingle(p => p.Name == "$format").Subject;
            formatParam.Schema.Enum.Should().NotBeNullOrEmpty();
        }

        [Fact]
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

            var context = new DocumentFilterContext(new List<ApiDescription>(), null, null);

            // Act
            filter.Apply(document, context);

            // Assert - POST should not have query parameters
            var postOperation = productsPath.Operations[OperationType.Post];
            postOperation.Parameters.Should().NotContain(p => p.Name.StartsWith("$"));
        }

        [Fact]
        public void Settings_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var settings = new ODataQueryOptionsSettings();

            // Assert
            settings.EnableFilter.Should().BeTrue();
            settings.EnableSelect.Should().BeTrue();
            settings.EnableExpand.Should().BeTrue();
            settings.EnableOrderBy.Should().BeTrue();
            settings.EnableTop.Should().BeTrue();
            settings.EnableSkip.Should().BeTrue();
            settings.EnableCount.Should().BeTrue();
            settings.EnableSearch.Should().BeFalse();
            settings.EnableFormat.Should().BeTrue();
            settings.EnablePagination.Should().BeTrue();
            settings.MaxTop.Should().Be(100);
            settings.DefaultTop.Should().Be(50);
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

        #endregion
    }
}
