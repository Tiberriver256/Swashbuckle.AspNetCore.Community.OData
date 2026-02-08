// -----------------------------------------------------------------------------
// <copyright file="ODataEndpointPathProviderTests.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
            var endpointDataSource = new TestEndpointDataSource([]);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings);

            // Assert
            Assert.IsEmpty(paths);
        }

        [TestMethod]
        public void GetPaths_WithODataEndpoints_ReturnsPaths()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpoints = CreateSampleODataEndpoints("odata", model);
            var endpointDataSource = new TestEndpointDataSource(endpoints);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings).ToList();

            // Assert
            Assert.IsNotEmpty(paths);
            Assert.Contains(p => p.PathTemplate == "/Products", paths);
            Assert.Contains(p => p.PathTemplate == "/Products({key})", paths);
        }

        [TestMethod]
        public void GetPaths_WithDifferentPrefix_FiltersByPrefix()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpoints = new List<Endpoint>
            {
                CreateODataEndpoint("odata", "/odata/Products", model),
                CreateODataEndpoint("v1", "/v1/Products", model)
            };
            var endpointDataSource = new TestEndpointDataSource(endpoints);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "v1");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings).ToList();

            // Assert
            Assert.HasCount(1, paths);
            Assert.IsTrue(paths.All(p => p.PathTemplate == "/Products"));
        }

        [TestMethod]
        public void GetPaths_CapturesHttpMethods()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpoints = new List<Endpoint>
            {
                CreateODataEndpointWithMethod("odata", "/odata/Products", "GET", model),
                CreateODataEndpointWithMethod("odata", "/odata/Products", "POST", model)
            };
            var endpointDataSource = new TestEndpointDataSource(endpoints);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings).ToList();

            // Assert
            var productPath = paths.Single(p => p.PathTemplate == "/Products");
            Assert.Contains("GET", productPath.HttpMethods);
            Assert.Contains("POST", productPath.HttpMethods);
        }

        [TestMethod]
        public void GetPaths_MergesDuplicatePaths()
        {
            // Arrange - Multiple operations on same path
            var model = CreateSampleEdmModel();
            var endpoints = new List<Endpoint>
            {
                CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "GET", model),
                CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "PUT", model),
                CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "PATCH", model),
                CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "DELETE", model)
            };
            var endpointDataSource = new TestEndpointDataSource(endpoints);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");
            var settings = new OpenApiConvertSettings();

            // Act
            var paths = provider.GetPaths(model, settings).ToList();

            // Assert
            var singleProductPath = paths.Single(p => p.PathTemplate?.Contains("({key})") == true);
            Assert.HasCount(4, singleProductPath.HttpMethods);
            Assert.Contains("GET", singleProductPath.HttpMethods);
            Assert.Contains("PUT", singleProductPath.HttpMethods);
            Assert.Contains("PATCH", singleProductPath.HttpMethods);
            Assert.Contains("DELETE", singleProductPath.HttpMethods);
        }

        [TestMethod]
        public void CanFilter_AlwaysReturnsTrue()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpointDataSource = new TestEndpointDataSource([]);
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");

            // Act & Assert
            Assert.IsTrue(provider.CanFilter(null!));
            Assert.IsNotNull(model.EntityContainer);
            Assert.IsTrue(provider.CanFilter(model.EntityContainer));
        }

        #region Helper Methods

        private static IEdmModel CreateSampleEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Product>("Products");
            builder.EntitySet<Category>("Categories");
            return builder.GetEdmModel();
        }

        private static List<Endpoint> CreateSampleODataEndpoints(string prefix, IEdmModel model) =>
            [
                CreateODataEndpointWithMethod(prefix, $"/{prefix}/Products", "GET", model),
                CreateODataEndpointWithMethod(prefix, $"/{prefix}/Products({{key}})", "GET", model),
                CreateODataEndpointWithMethod(prefix, $"/{prefix}/Products({{key}})", "PUT", model),
                CreateODataEndpointWithMethod(prefix, $"/{prefix}/Categories", "GET", model)
            ];

        private static RouteEndpoint CreateODataEndpoint(string prefix, string path, IEdmModel model) => CreateODataEndpointWithMethod(prefix, path, "GET", model);

        private static RouteEndpoint CreateODataEndpointWithMethod(string prefix, string rawText, string httpMethod, IEdmModel model)
        {
            var routePattern = RoutePatternFactory.Parse(rawText);
            var entitySetName = rawText.Trim('/').Split('/').Last().Split('(')[0];
            var entitySet = model.EntityContainer.FindEntitySet(entitySetName)
                ?? throw new InvalidOperationException($"Entity set '{entitySetName}' not found.");

            var metadataCollection = new EndpointMetadataCollection(
                new HttpMethodMetadata([httpMethod]),
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

        private sealed class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

        private sealed class Category
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private sealed class TestEndpointDataSource(List<Endpoint> endpoints) : EndpointDataSource
        {
            private readonly List<Endpoint> endpointsValue = endpoints;

            public override IChangeToken GetChangeToken() => new NullChangeToken();

            public override IReadOnlyList<Endpoint> Endpoints => this.endpointsValue;
        }

        private sealed class NullChangeToken : IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new NullDisposable();
        }

        private sealed class NullDisposable : IDisposable
        {
            public void Dispose() { }
        }

        private sealed class TestODataRoutingMetadata(string prefix, IEdmModel model, ODataPathTemplate template) : IODataRoutingMetadata
        {
            public string Prefix { get; } = prefix;
            public IEdmModel Model { get; } = model;
            public ODataPathTemplate Template { get; } = template;
            public bool IsConventional => true;
        }

        #endregion
    }
}
