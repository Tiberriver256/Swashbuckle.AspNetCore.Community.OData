// -----------------------------------------------------------------------------
// <copyright file="ODataEndpointPathProviderTests.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.OData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Swashbuckle.AspNetCore.Community.OData.ODataRouting;

namespace Swashbuckle.AspNetCore.Community.OData.Tests.ODataRouting
{
    /// <summary>
    /// Tests for the ODataEndpointPathProvider.
    /// </summary>
    [TestClass]
    public class ODataEndpointPathProviderTests
    {
        [TestMethod]
        public void GetPaths_WithNoEndpoints_ReturnsEmptyList()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpointDataSource = new TestEndpointDataSource(new List<Endpoint>());
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings);

            // Assert
            paths.Should().BeEmpty();
        }

        [TestMethod]
        public void GetPaths_WithODataEndpoints_ReturnsPaths()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpoints = this.CreateSampleODataEndpoints("odata", model);
            var endpointDataSource = new TestEndpointDataSource(endpoints);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings).ToList();

            // Assert
            paths.Should().NotBeEmpty();
            paths.Should().Contain(p => p.PathTemplate == "/Products");
            paths.Should().Contain(p => p.PathTemplate == "/Products({key})");
        }

        [TestMethod]
        public void GetPaths_WithDifferentPrefix_FiltersByPrefix()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpoints = new List<Endpoint>
            {
                this.CreateODataEndpoint("odata", "/odata/Products", model),
                this.CreateODataEndpoint("v1", "/v1/Products", model)
            };
            var endpointDataSource = new TestEndpointDataSource(endpoints);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "v1");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings).ToList();

            // Assert
            paths.Should().HaveCount(1);
            paths.Should().OnlyContain(p => p.PathTemplate == "/Products");
        }

        [TestMethod]
        public void GetPaths_CapturesHttpMethods()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpoints = new List<Endpoint>
            {
                this.CreateODataEndpointWithMethod("odata", "/odata/Products", "GET", model),
                this.CreateODataEndpointWithMethod("odata", "/odata/Products", "POST", model)
            };
            var endpointDataSource = new TestEndpointDataSource(endpoints);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings).ToList();

            // Assert
            var productPath = paths.Should().ContainSingle(p => p.PathTemplate == "/Products").Subject;
            productPath.HttpMethods.Should().Contain("GET");
            productPath.HttpMethods.Should().Contain("POST");
        }

        [TestMethod]
        public void GetPaths_MergesDuplicatePaths()
        {
            // Arrange - Multiple operations on same path
            var model = CreateSampleEdmModel();
            var endpoints = new List<Endpoint>
            {
                this.CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "GET", model),
                this.CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "PUT", model),
                this.CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "PATCH", model),
                this.CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "DELETE", model)
            };
            var endpointDataSource = new TestEndpointDataSource(endpoints);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings).ToList();

            // Assert
            var singleProductPath = paths.Should().ContainSingle(p => p.PathTemplate.Contains("({key})")).Subject;
            singleProductPath.HttpMethods.Should().HaveCount(4);
            singleProductPath.HttpMethods.Should().Contain(new[] { "GET", "PUT", "PATCH", "DELETE" });
        }

        [TestMethod]
        public void CanFilter_AlwaysReturnsTrue()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpointDataSource = new TestEndpointDataSource(new List<Endpoint>());
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");

            // Act & Assert
            provider.CanFilter(null!).Should().BeTrue();
            provider.CanFilter(model.EntityContainer).Should().BeTrue();
        }

        #region Helper Methods

        private IEdmModel CreateSampleEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Product>("Products");
            builder.EntitySet<Category>("Categories");
            return builder.GetEdmModel();
        }

        private List<Endpoint> CreateSampleODataEndpoints(string prefix, IEdmModel model)
        {
            return new List<Endpoint>
            {
                this.CreateODataEndpointWithMethod(prefix, $"/{prefix}/Products", "GET", model),
                this.CreateODataEndpointWithMethod(prefix, $"/{prefix}/Products({{key}})", "GET", model),
                this.CreateODataEndpointWithMethod(prefix, $"/{prefix}/Products({{key}})", "PUT", model),
                this.CreateODataEndpointWithMethod(prefix, $"/{prefix}/Categories", "GET", model)
            };
        }

        private Endpoint CreateODataEndpoint(string prefix, string path, IEdmModel model)
        {
            return this.CreateODataEndpointWithMethod(prefix, path, "GET", model);
        }

        private Endpoint CreateODataEndpointWithMethod(string prefix, string rawText, string httpMethod, IEdmModel model)
        {
            var routePattern = RoutePatternFactory.Parse(rawText);
            var entitySetName = rawText.Trim('/').Split('/').Last().Split('(')[0];
            var entitySet = model.EntityContainer.FindEntitySet(entitySetName)
                ?? throw new InvalidOperationException($"Entity set '{entitySetName}' not found.");

            var metadataCollection = new EndpointMetadataCollection(
                new HttpMethodMetadata(new[] { httpMethod }),
                new TestODataRoutingMetadata(prefix, model, new ODataPathTemplate(new EntitySetSegmentTemplate(entitySet)))
            );

            return new RouteEndpoint(
                _ => throw new NotImplementedException(),
                routePattern,
                0,
                metadataCollection,
                "Test"
            );
        }

        #endregion

        #region Test Classes

        private class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

        private class Category
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class TestEndpointDataSource : EndpointDataSource
        {
            private readonly List<Endpoint> _endpoints;

            public TestEndpointDataSource(List<Endpoint> endpoints)
            {
                _endpoints = endpoints;
            }

            public override IChangeToken GetChangeToken() => new NullChangeToken();

            public override IReadOnlyList<Endpoint> Endpoints => _endpoints;
        }

        private class NullChangeToken : IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new NullDisposable();
        }

        private class NullDisposable : IDisposable
        {
            public void Dispose() { }
        }

        private class TestODataRoutingMetadata : IODataRoutingMetadata
        {
            public TestODataRoutingMetadata(string prefix, IEdmModel model, ODataPathTemplate template)
            {
                Prefix = prefix;
                Model = model;
                Template = template;
            }

            public string Prefix { get; }
            public IEdmModel Model { get; }
            public ODataPathTemplate Template { get; }
            public bool IsConventional => true;
        }

        #endregion
    }
}
