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
    public class ODataEndpointPathProvider : IODataPathProvider
    {
        private readonly IEdmModel _model;
        private readonly EndpointDataSource _endpointDataSource;
        private readonly string _routePrefix;
        private readonly IList<ODataPath> _paths;

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

                if (endpoint is not RouteEndpoint routeEndpoint)
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
                var httpMethods = GetHttpMethods(endpoint).ToArray();
                if (!httpMethods.Any())
                {
                    // Default to GET if no methods specified
                    httpMethods = ["GET"];
                }

                // Check for existing path with same template
                if (templateToPathDict.TryGetValue(routeTemplate, out var existingPath))
                {
                    foreach (var method in httpMethods.Where(method => !existingPath.HttpMethods.Contains(method)))
                    {
                        existingPath.HttpMethods.Add(method);
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

                _paths.Add(path);
                templateToPathDict[routeTemplate] = path;
            }
        }

        /// <summary>
        /// Extracts the route template from a RouteEndpoint, removing the OData prefix.
        /// </summary>
        private static string ExtractRouteTemplate(RouteEndpoint routeEndpoint, string prefix)
        {
            string rawText = routeEndpoint.RoutePattern.RawText;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return null;
            }

            return RemovePrefixSegment(rawText, prefix);
        }

        /// <summary>
        /// Gets HTTP methods from endpoint metadata.
        /// </summary>
        private static IEnumerable<string> GetHttpMethods(Endpoint endpoint)
        {
            var httpMethodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            return httpMethodMetadata?.HttpMethods ?? Enumerable.Empty<string>();
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
                if (translatedSegment != null)
                {
                    segments.Add(translatedSegment);
                }
            }

            return segments.Any() ? new ODataPath(segments) : null;
        }

        /// <summary>
        /// Translates a segment template to an OData segment.
        /// </summary>
        private static ODataSegment TranslateSegment(ODataSegmentTemplate segment, IEdmModel model)
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
                PropertySegmentTemplate property when property.Property.Type.IsComplex() => new ODataComplexPropertySegment(property.Property),
                CountSegmentTemplate _ => new ODataDollarCountSegment(),
                MetadataSegmentTemplate => new ODataMetadataSegment(),
                _ => null
            };
        }

        private static string RemovePrefixSegment(string rawText, string prefix)
        {
            var normalizedPath = rawText.Trim();
            if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
            {
                normalizedPath = "/" + normalizedPath.TrimStart('/');
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                return normalizedPath;
            }

            var normalizedPrefix = prefix.Trim().Trim('/');
            if (normalizedPrefix.Length == 0)
            {
                return normalizedPath;
            }

            var prefixSegment = "/" + normalizedPrefix;
            var comparison = StringComparison.OrdinalIgnoreCase;

            if (string.Equals(normalizedPath, prefixSegment, comparison) ||
                string.Equals(normalizedPath, prefixSegment + "/", comparison))
            {
                return "/";
            }

            var prefixWithTrailingSlash = prefixSegment + "/";
            if (normalizedPath.StartsWith(prefixWithTrailingSlash, comparison))
            {
                var remaining = normalizedPath.Substring(prefixWithTrailingSlash.Length);
                return "/" + remaining.TrimStart('/');
            }

            return normalizedPath;
        }
    }
}
