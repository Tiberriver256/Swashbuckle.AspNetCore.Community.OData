// -----------------------------------------------------------------------------
// <copyright file="ODataEndpointPathProviderTests.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.OData;
using Swashbuckle.AspNetCore.Community.OData.ODataRouting;
using Xunit;

namespace Swashbuckle.AspNetCore.Community.OData.Tests.ODataRouting
{
    /// <summary>
    /// Tests for the ODataEndpointPathProvider.
    /// </summary>
    public class ODataEndpointPathProviderTests
    {
        [Fact]
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

        [Fact]
        public void GetPaths_WithODataEndpoints_ReturnsPaths()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpoints = CreateSampleODataEndpoints("odata");
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

        [Fact]
        public void GetPaths_WithDifferentPrefix_FiltersByPrefix()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpoints = new List<Endpoint>
            {
                CreateODataEndpoint("odata", "/odata/Products"),
                CreateODataEndpoint("v1", "/v1/Products")
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

        [Fact]
        public void GetPaths_CapturesHttpMethods()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpoints = new List<Endpoint>
            {
                CreateODataEndpointWithMethod("odata", "/odata/Products", "GET"),
                CreateODataEndpointWithMethod("odata", "/odata/Products", "POST")
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

        [Fact]
        public void GetPaths_MergesDuplicatePaths()
        {
            // Arrange - Multiple operations on same path
            var model = CreateSampleEdmModel();
            var endpoints = new List<Endpoint>
            {
                CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "GET"),
                CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "PUT"),
                CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "PATCH"),
                CreateODataEndpointWithMethod("odata", "/odata/Products({key})", "DELETE")
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

        [Fact]
        public void CanFilter_AlwaysReturnsTrue()
        {
            // Arrange
            var model = CreateSampleEdmModel();
            var endpointDataSource = new TestEndpointDataSource(new List<Endpoint>());
            var provider = new ODataEndpointPathProvider(model, endpointDataSource, "odata");

            // Act & Assert
            provider.CanFilter(null).Should().BeTrue();
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

        private List<Endpoint> CreateSampleODataEndpoints(string prefix)
        {
            return new List<Endpoint>
            {
                CreateODataEndpointWithMethod(prefix, $"/{prefix}/Products", "GET"),
                CreateODataEndpointWithMethod(prefix, $"/{prefix}/Products({{key}})", "GET"),
                CreateODataEndpointWithMethod(prefix, $"/{prefix}/Products({{key}})", "PUT"),
                CreateODataEndpointWithMethod(prefix, $"/{prefix}/Categories", "GET")
            };
        }

        private Endpoint CreateODataEndpoint(string prefix, string path)
        {
            return CreateODataEndpointWithMethod(prefix, path, "GET");
        }

        private Endpoint CreateODataEndpointWithMethod(string prefix, string rawText, string httpMethod)
        {
            var routePattern = RoutePatternFactory.Parse(rawText);
            var metadataCollection = new EndpointMetadataCollection(
                new TestODataRoutingMetadata(prefix, null, new ODataPathTemplate())
            );
            
            var endpoint = new RouteEndpoint(
                requestContext => throw new System.NotImplementedException(),
                routePattern,
                0,
                new EndpointNameMetadata("Test"),
                metadataCollection
            );

            return endpoint;
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
            public IDisposable RegisterChangeCallback(System.Action<object> callback, object state) => new NullDisposable();
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
        }

        #endregion
    }
}
