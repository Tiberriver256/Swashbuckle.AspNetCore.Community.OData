// -----------------------------------------------------------------------------
// <copyright file="ODataEndpointPathProvider.cs" company=".NET Foundation">
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.OData;
using Microsoft.OpenApi.OData.Edm;

namespace Swashbuckle.AspNetCore.Community.OData.ODataRouting
{
    /// <summary>
    /// Provides OData paths from actual ASP.NET Core endpoint routing data.
    /// This captures real controller endpoints with their HTTP methods, 
    /// enabling accurate OpenAPI generation that reflects the actual API surface.
    /// </summary>
    internal class ODataEndpointPathProvider : IODataPathProvider
    {
        private readonly IEdmModel _model;
        private readonly EndpointDataSource _endpointDataSource;
        private readonly string _routePrefix;
        private IList<ODataPath> _paths;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataEndpointPathProvider"/> class.
        /// </summary>
        /// <param name="model">The EDM model.</param>
        /// <param name="endpointDataSource">The endpoint data source.</param>
        /// <param name="routePrefix">The route prefix (e.g., "odata", "v1").</param>
        public ODataEndpointPathProvider(IEdmModel model, EndpointDataSource endpointDataSource, string routePrefix)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _endpointDataSource = endpointDataSource ?? throw new ArgumentNullException(nameof(endpointDataSource));
            _routePrefix = routePrefix ?? string.Empty;
            _paths = new List<ODataPath>();
        }

        /// <inheritdoc/>
        public bool CanFilter(IEdmElement element) => true;

        /// <inheritdoc/>
        public IEnumerable<ODataPath> GetPaths(IEdmModel model, OpenApiConvertSettings settings)
        {
            if (_paths.Any())
            {
                return _paths;
            }

            CollectPathsFromEndpoints();
            return _paths;
        }

        /// <summary>
        /// Collects OData paths from actual ASP.NET Core endpoints.
        /// </summary>
        private void CollectPathsFromEndpoints()
        {
            if (_endpointDataSource == null)
            {
                return;
            }

            var templateToPathDict = new Dictionary<string, ODataPath>();

            foreach (var endpoint in _endpointDataSource.Endpoints)
            {
                var metadata = endpoint.Metadata.GetMetadata<IODataRoutingMetadata>();
                if (metadata == null)
                {
                    continue;
                }

                // Filter by route prefix
                if (!string.IsNullOrEmpty(_routePrefix) && !metadata.Prefix.Equals(_routePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!(endpoint is RouteEndpoint routeEndpoint))
                {
                    continue;
                }

                // Extract route template (remove prefix)
                string routeTemplate = ExtractRouteTemplate(routeEndpoint, metadata.Prefix);
                if (string.IsNullOrEmpty(routeTemplate))
                {
                    continue;
                }

                // Get HTTP methods from endpoint
                var httpMethods = GetHttpMethods(endpoint);
                if (!httpMethods.Any())
                {
                    // Default to GET if no methods specified
                    httpMethods = new[] { "GET" };
                }

                // Check for existing path with same template
                if (templateToPathDict.TryGetValue(routeTemplate, out var existingPath))
                {
                    // Add HTTP methods to existing path
                    foreach (var method in httpMethods)
                    {
                        if (!existingPath.HttpMethods.Contains(method))
                        {
                            existingPath.HttpMethods.Add(method);
                        }
                    }
                    continue;
                }

                // Translate path template to ODataPath
                var path = TranslatePathTemplate(metadata.Template, _model);
                if (path == null)
                {
                    continue;
                }

                // Store the original route template for accurate path generation
                path.PathTemplate = routeTemplate;

                // Add HTTP methods
                foreach (var method in httpMethods)
                {
                    path.HttpMethods.Add(method);
                }

                // Store capabilities based on endpoint metadata
                AddEndpointCapabilities(endpoint, path);

                _paths.Add(path);
                templateToPathDict[routeTemplate] = path;
            }
        }

        /// <summary>
        /// Extracts the route template from a RouteEndpoint, removing the OData prefix.
        /// </summary>
        private string ExtractRouteTemplate(RouteEndpoint routeEndpoint, string prefix)
        {
            string rawText = routeEndpoint.RoutePattern.RawText;
            if (string.IsNullOrEmpty(rawText))
            {
                return null;
            }

            // Remove prefix from route
            int prefixLength = prefix?.Length ?? 0;
            if (prefixLength > 0 && rawText.Length > prefixLength)
            {
                rawText = rawText.Substring(prefixLength);
            }

            // Ensure starts with /
            if (!rawText.StartsWith("/"))
            {
                rawText = "/" + rawText;
            }

            return rawText;
        }

        /// <summary>
        /// Gets HTTP methods from endpoint metadata.
        /// </summary>
        private IEnumerable<string> GetHttpMethods(Endpoint endpoint)
        {
            var httpMethodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            if (httpMethodMetadata != null)
            {
                return httpMethodMetadata.HttpMethods;
            }

            // Try to infer from OData action metadata
            var odataActionMetadata = endpoint.Metadata.GetMetadata<IActionHttpMethodProvider>();
            if (odataActionMetadata != null)
            {
                return new[] { odataActionMetadata.HttpMethod };
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Adds endpoint-specific capabilities to the OData path.
        /// </summary>
        private void AddEndpointCapabilities(Endpoint endpoint, ODataPath path)
        {
            // Check for EnableQuery attribute (indicates $filter, $select, etc. support)
            var enableQueryMetadata = endpoint.Metadata.GetMetadata<IEnableQueryMetadata>();
            if (enableQueryMetadata != null)
            {
                path.EnableQuery = true;
                path.AllowedQueryOptions = enableQueryMetadata.AllowedQueryOptions;
            }

            // Check for specific OData capabilities
            var capabilitiesMetadata = endpoint.Metadata.GetMetadata<IODataCapabilitiesMetadata>();
            if (capabilitiesMetadata != null)
            {
                path.SupportsFilter = capabilitiesMetadata.SupportsFilter;
                path.SupportsExpand = capabilitiesMetadata.SupportsExpand;
                path.SupportsSelect = capabilitiesMetadata.SupportsSelect;
                path.SupportsOrderBy = capabilitiesMetadata.SupportsOrderBy;
                path.SupportsCount = capabilitiesMetadata.SupportsCount;
                path.SupportsTop = capabilitiesMetadata.SupportsTop;
                path.SupportsSkip = capabilitiesMetadata.SupportsSkip;
            }
        }

        /// <summary>
        /// Translates an ODataPathTemplate to an ODataPath with actual segment mappings.
        /// </summary>
        private ODataPath TranslatePathTemplate(ODataPathTemplate template, IEdmModel model)
        {
            if (template.Count == 0)
            {
                return null;
            }

            var segments = new List<ODataSegment>();

            foreach (var segment in template)
            {
                var translatedSegment = TranslateSegment(segment, model);
                if (translatedSegment == null)
                {
                    // Some segments we skip (like dynamic segments)
                    // but for property/value/count we should handle them
                    translatedSegment = TryCreateSpecialSegment(segment, model);
                    if (translatedSegment == null)
                    {
                        continue;
                    }
                }
                segments.Add(translatedSegment);
            }

            if (!segments.Any())
            {
                return null;
            }

            return new ODataPath(segments);
        }

        /// <summary>
        /// Translates a segment template to an OData segment.
        /// </summary>
        private ODataSegment TranslateSegment(ODataSegmentTemplate segment, IEdmModel model)
        {
            return segment switch
            {
                EntitySetSegmentTemplate entitySet => new ODataNavigationSourceSegment(entitySet.Segment.EntitySet),
                SingletonSegmentTemplate singleton => new ODataNavigationSourceSegment(singleton.Singleton),
                KeySegmentTemplate key => new ODataKeySegment(key.EntityType, key.KeyMappings),
                CastSegmentTemplate cast => new ODataTypeCastSegment(cast.ExpectedType as IEdmEntityType, model),
                NavigationSegmentTemplate navigation => new ODataNavigationPropertySegment(navigation.Segment.NavigationProperty),
                NavigationLinkSegmentTemplate navLink => new ODataNavigationPropertySegment(navLink.NavigationProperty),
                FunctionSegmentTemplate function => new ODataOperationSegment(function.Function),
                ActionSegmentTemplate action => new ODataOperationSegment(action.Action),
                FunctionImportSegmentTemplate funcImport => new ODataOperationImportSegment(funcImport.FunctionImport, funcImport.ParameterMappings),
                ActionImportSegmentTemplate actionImport => new ODataOperationImportSegment(actionImport.ActionImport),
                CountSegmentTemplate count => new ODataDollarCountSegment(),
                MetadataSegmentTemplate => new ODataMetadataSegment(),
                _ => null
            };
        }

        /// <summary>
        /// Attempts to create segments for special cases like property access, value, etc.
        /// </summary>
        private ODataSegment TryCreateSpecialSegment(ODataSegmentTemplate segment, IEdmModel model)
        {
            // Handle property segment
            if (segment is PropertySegmentTemplate propertySegment)
            {
                // Create a property segment for accessing entity properties
                return new ODataPropertySegment(propertySegment.Property);
            }

            // Handle value segment
            if (segment is ValueSegmentTemplate)
            {
                return new ODataValueSegment();
            }

            // Handle navigation links
            if (segment is NavigationLinkSegmentTemplate navLink)
            {
                return new ODataReferenceSegment(navLink.NavigationProperty);
            }

            return null;
        }
    }

    /// <summary>
    /// Represents a segment for accessing entity properties.
    /// </summary>
    internal class ODataPropertySegment : ODataSegment
    {
        private readonly IEdmStructuralProperty _property;

        public ODataPropertySegment(IEdmStructuralProperty property)
        {
            _property = property;
        }

        public override string SegmentName => _property.Name;

        public override IEdmType EdmType => _property.Type.Definition;
    }

    /// <summary>
    /// Represents a $value segment.
    /// </summary>
    internal class ODataValueSegment : ODataSegment
    {
        public override string SegmentName => "$value";

        public override IEdmType EdmType => null;
    }

    /// <summary>
    /// Represents a $ref segment for navigation links.
    /// </summary>
    internal class ODataReferenceSegment : ODataSegment
    {
        private readonly IEdmNavigationProperty _navigationProperty;

        public ODataReferenceSegment(IEdmNavigationProperty navigationProperty)
        {
            _navigationProperty = navigationProperty;
        }

        public override string SegmentName => "$ref";

        public override IEdmType EdmType => _navigationProperty?.Type?.Definition;
    }

    /// <summary>
    /// Extended ODataPath with additional endpoint metadata.
    /// </summary>
    internal class ODataPathWithMetadata : ODataPath
    {
        public ODataPathWithMetadata(IEnumerable<ODataSegment> segments) : base(segments)
        {
        }

        public bool EnableQuery { get; set; }
        public Microsoft.AspNetCore.OData.Query.AllowedQueryOptions AllowedQueryOptions { get; set; }
        public bool SupportsFilter { get; set; }
        public bool SupportsExpand { get; set; }
        public bool SupportsSelect { get; set; }
        public bool SupportsOrderBy { get; set; }
        public bool SupportsCount { get; set; }
        public bool SupportsTop { get; set; }
        public bool SupportsSkip { get; set; }
    }

    /// <summary>
    /// Interface for EnableQuery metadata.
    /// </summary>
    internal interface IEnableQueryMetadata
    {
        Microsoft.AspNetCore.OData.Query.AllowedQueryOptions AllowedQueryOptions { get; }
    }

    /// <summary>
    /// Interface for OData capabilities metadata.
    /// </summary>
    internal interface IODataCapabilitiesMetadata
    {
        bool SupportsFilter { get; }
        bool SupportsExpand { get; }
        bool SupportsSelect { get; }
        bool SupportsOrderBy { get; }
        bool SupportsCount { get; }
        bool SupportsTop { get; }
        bool SupportsSkip { get; }
    }

    /// <summary>
    /// Interface for HTTP method provider.
    /// </summary>
    internal interface IActionHttpMethodProvider
    {
        string HttpMethod { get; }
    }
}
