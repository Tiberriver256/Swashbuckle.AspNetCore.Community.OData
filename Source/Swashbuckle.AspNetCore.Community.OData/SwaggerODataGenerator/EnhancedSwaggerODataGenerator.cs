// -----------------------------------------------------------------------------
// <copyright file="EnhancedSwaggerODataGenerator.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.OData;
using Swashbuckle.AspNetCore.Community.OData.ODataRouting;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.Community.OData.SwaggerODataGenerator
{
    /// <summary>
    /// Enhanced Swagger generator for OData that uses actual endpoint routing data
    /// to produce accurate OpenAPI documentation with full OData query support.
    /// </summary>
    public class EnhancedSwaggerODataGenerator : ISwaggerProvider
    {
        private readonly SwaggerODataGeneratorOptions _options;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedSwaggerODataGenerator"/> class.
        /// </summary>
        /// <param name="options">The generator options.</param>
        /// <param name="serviceProvider">The service provider for accessing endpoint data.</param>
        public EnhancedSwaggerODataGenerator(
            IOptions<SwaggerODataGeneratorOptions> options,
            IServiceProvider serviceProvider)
        {
            _options = options?.Value ?? new SwaggerODataGeneratorOptions();
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc/>
        public OpenApiDocument GetSwagger(string documentName, string host = null, string basePath = null)
        {
            if (!_options.SwaggerDocs.TryGetValue(documentName, out var docInfo))
            {
                throw new UnknownSwaggerDocument(
                    documentName,
                    _options.SwaggerDocs.Select(d => d.Key)
                );
            }

            var (odataRoute, apiInfo) = docInfo;

            if (!_options.EdmModels.TryGetValue(odataRoute, out var edmModel))
            {
                throw new UnknownODataEdm(
                    odataRoute,
                    _options.EdmModels.Select(d => d.Key)
                );
            }

            // Get endpoint data source if available
            var endpointDataSource = _serviceProvider.GetService<EndpointDataSource>();

            // Create the OpenAPI document using endpoint-aware path provider
            var document = CreateOpenApiDocument(edmModel, odataRoute, apiInfo, endpointDataSource, host, basePath);

            return document;
        }

        /// <summary>
        /// Creates an OpenAPI document from the EDM model with endpoint routing data.
        /// </summary>
        private OpenApiDocument CreateOpenApiDocument(
            IEdmModel model,
            string routePrefix,
            OpenApiInfo info,
            EndpointDataSource endpointDataSource,
            string host,
            string basePath)
        {
            // Build service root URI
            var serviceRoot = BuildServiceRoot(host, basePath, routePrefix);

            // Create path provider with endpoint data
            IODataPathProvider pathProvider = endpointDataSource != null
                ? new ODataEndpointPathProvider(model, endpointDataSource, routePrefix)
                : null;

            // Configure conversion settings
            var settings = new OpenApiConvertSettings
            {
                ServiceRoot = serviceRoot,
                PathProvider = pathProvider,
                EnableKeyAsSegment = false,
                EnableUnqualifiedCall = false,
                EnableOperationId = true,
                EnableOperationPath = true,
                EnableOperationImportPath = true,
                EnableNavigationPropertyPath = true,
                EnableDollarCountPath = true,
                EnableODataTypeCast = true,
                AddEnumDescriptionExtension = true,
                ShowSchemaExamples = true,
                ShowExternalDocs = true,
                PrefixEntityTypeNameBeforeKey = true,
                UseSuccessStatusCodeRange = false
            };

            // Generate base document from EDM model
            var document = model.ConvertToOpenApi(settings);

            // Set API info
            document.Info = info;
            document.Servers = new List<OpenApiServer>
            {
                new OpenApiServer { Url = $"/{routePrefix}" }
            };

            // Enhance with endpoint-specific metadata if we have endpoint data
            if (endpointDataSource != null)
            {
                EnhanceDocumentWithEndpointMetadata(document, endpointDataSource, routePrefix);
            }

            // Add OData query options to collection endpoints
            AddODataQueryOptions(document);

            // Add property access, value, and reference paths
            AddMissingODataPaths(document, model, routePrefix);

            return document;
        }

        /// <summary>
        /// Builds the service root URI.
        /// </summary>
        private Uri BuildServiceRoot(string host, string basePath, string routePrefix)
        {
            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = host ?? "localhost",
                Path = $"/{routePrefix}"
            };

            return builder.Uri;
        }

        /// <summary>
        /// Enhances the document with metadata from actual endpoints.
        /// </summary>
        private void EnhanceDocumentWithEndpointMetadata(OpenApiDocument document, EndpointDataSource endpointDataSource, string routePrefix)
        {
            foreach (var endpoint in endpointDataSource.Endpoints)
            {
                var metadata = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.OData.Routing.IODataRoutingMetadata>();
                if (metadata == null || metadata.Prefix != routePrefix)
                {
                    continue;
                }

                if (!(endpoint is RouteEndpoint routeEndpoint))
                {
                    continue;
                }

                // Extract path
                string pathTemplate = ExtractPathTemplate(routeEndpoint.RoutePattern.RawText, routePrefix);
                if (string.IsNullOrEmpty(pathTemplate) || !document.Paths.ContainsKey(pathTemplate))
                {
                    continue;
                }

                var pathItem = document.Paths[pathTemplate];

                // Get HTTP methods
                var httpMethods = GetHttpMethods(endpoint);
                
                // Ensure all declared HTTP methods are documented
                foreach (var method in httpMethods)
                {
                    var operationType = ParseOperationType(method);
                    if (operationType.HasValue && !pathItem.Operations.ContainsKey(operationType.Value))
                    {
                        // Create missing operation based on HTTP method conventions
                        var operation = CreateDefaultOperation(endpoint, operationType.Value, pathTemplate);
                        if (operation != null)
                        {
                            pathItem.Operations[operationType.Value] = operation;
                        }
                    }
                }

                // Enhance existing operations with endpoint metadata
                foreach (var operation in pathItem.Operations)
                {
                    EnhanceOperationWithMetadata(operation.Value, endpoint);
                }
            }
        }

        /// <summary>
        /// Adds OData query options ($filter, $select, etc.) to GET operations on collection endpoints.
        /// </summary>
        private void AddODataQueryOptions(OpenApiDocument document)
        {
            foreach (var path in document.Paths)
            {
                // Only add to collection endpoints (not single entity by key)
                if (IsCollectionEndpoint(path.Key) && path.Value.Operations.TryGetValue(OperationType.Get, out var getOperation))
                {
                    AddQueryParametersToOperation(getOperation);
                }
            }
        }

        /// <summary>
        /// Adds missing OData paths like property access, $value, $ref.
        /// </summary>
        private void AddMissingODataPaths(OpenApiDocument document, IEdmModel model, string routePrefix)
        {
            // Get all entity sets and singletons
            var entitySets = model.EntityContainer?.EntitySets() ?? Enumerable.Empty<IEdmEntitySet>();
            var singletons = model.EntityContainer?.Singletons() ?? Enumerable.Empty<IEdmSingleton>();

            var pathsToAdd = new Dictionary<string, OpenApiPathItem>();

            foreach (var entitySet in entitySets)
            {
                var entityType = entitySet.EntityType;

                // Add paths for each entity (key access)
                string entityByKeyPath = $"/{entitySet.Name}({{key}})";
                
                if (document.Paths.ContainsKey(entityByKeyPath))
                {
                    // Add property access paths
                    AddPropertyAccessPaths(pathsToAdd, entityType, entityByKeyPath, model);

                    // Add $value path
                    AddValuePath(pathsToAdd, entityByKeyPath);

                    // Add $ref path for navigation properties
                    AddNavigationReferencePaths(pathsToAdd, entityType, entityByKeyPath);
                }
            }

            foreach (var singleton in singletons)
            {
                var entityType = singleton.EntityType;
                string singletonPath = $"/{singleton.Name}";

                if (document.Paths.ContainsKey(singletonPath))
                {
                    // Add property access paths for singleton
                    AddPropertyAccessPaths(pathsToAdd, entityType, singletonPath, model);

                    // Add $value path
                    AddValuePath(pathsToAdd, singletonPath);

                    // Add $ref path for navigation properties
                    AddNavigationReferencePaths(pathsToAdd, entityType, singletonPath);
                }
            }

            // Add collected paths to document
            foreach (var kvp in pathsToAdd)
            {
                if (!document.Paths.ContainsKey(kvp.Key))
                {
                    document.Paths.Add(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Adds paths for accessing entity properties directly.
        /// </summary>
        private void AddPropertyAccessPaths(Dictionary<string, OpenApiPathItem> paths, IEdmEntityType entityType, string basePath, IEdmModel model)
        {
            foreach (var property in entityType.DeclaredProperties.OfType<IEdmStructuralProperty>())
            {
                string propertyPath = $"{basePath}/{property.Name}";
                if (paths.ContainsKey(propertyPath))
                {
                    continue;
                }

                var pathItem = new OpenApiPathItem();
                
                // GET for property value
                pathItem.Operations[OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = entityType.Name } },
                    Summary = $"Get {property.Name} property value",
                    OperationId = $"Get{entityType.Name}{property.Name}",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Success",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = CreatePropertySchema(property)
                                }
                            }
                        }
                    }
                };

                paths[propertyPath] = pathItem;

                // Add $value path for raw property value
                string valuePath = $"{propertyPath}/$value";
                if (!paths.ContainsKey(valuePath))
                {
                    var valuePathItem = new OpenApiPathItem();
                    valuePathItem.Operations[OperationType.Get] = new OpenApiOperation
                    {
                        Tags = new List<OpenApiTag> { new OpenApiTag { Name = entityType.Name } },
                        Summary = $"Get raw {property.Name} value",
                        OperationId = $"Get{entityType.Name}{property.Name}Value",
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse
                            {
                                Description = "Raw property value"
                            }
                        }
                    };
                    paths[valuePath] = valuePathItem;
                }
            }
        }

        /// <summary>
        /// Adds $value path for raw entity value access.
        /// </summary>
        private void AddValuePath(Dictionary<string, OpenApiPathItem> paths, string basePath)
        {
            string valuePath = $"{basePath}/$value";
            if (paths.ContainsKey(valuePath))
            {
                return;
            }

            var pathItem = new OpenApiPathItem();
            pathItem.Operations[OperationType.Get] = new OpenApiOperation
            {
                Summary = "Get raw entity value",
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse { Description = "Raw entity value" }
                }
            };

            paths[valuePath] = pathItem;
        }

        /// <summary>
        /// Adds $ref paths for navigation properties.
        /// </summary>
        private void AddNavigationReferencePaths(Dictionary<string, OpenApiPathItem> paths, IEdmEntityType entityType, string basePath)
        {
            foreach (var navProperty in entityType.DeclaredNavigationProperties())
            {
                string refPath = $"{basePath}/{navProperty.Name}/$ref";
                if (paths.ContainsKey(refPath))
                {
                    continue;
                }

                var pathItem = new OpenApiPathItem();

                // GET $ref
                pathItem.Operations[OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = entityType.Name } },
                    Summary = $"Get {navProperty.Name} reference",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Navigation reference"
                        }
                    }
                };

                // PUT $ref (update reference)
                pathItem.Operations[OperationType.Put] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = entityType.Name } },
                    Summary = $"Update {navProperty.Name} reference",
                    Responses = new OpenApiResponses
                    {
                        ["204"] = new OpenApiResponse { Description = "Reference updated" }
                    }
                };

                // DELETE $ref (remove reference)
                pathItem.Operations[OperationType.Delete] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new OpenApiTag { Name = entityType.Name } },
                    Summary = $"Delete {navProperty.Name} reference",
                    Responses = new OpenApiResponses
                    {
                        ["204"] = new OpenApiResponse { Description = "Reference deleted" }
                    }
                };

                paths[refPath] = pathItem;
            }
        }

        /// <summary>
        /// Creates a schema for a property.
        /// </summary>
        private OpenApiSchema CreatePropertySchema(IEdmStructuralProperty property)
        {
            return new OpenApiSchema
            {
                Type = ConvertEdmTypeToOpenApiType(property.Type),
                Format = ConvertEdmTypeToOpenApiFormat(property.Type)
            };
        }

        /// <summary>
        /// Converts EDM type to OpenAPI type.
        /// </summary>
        private string ConvertEdmTypeToOpenApiType(IEdmTypeReference edmType)
        {
            if (edmType.IsCollection)
            {
                return "array";
            }

            return edmType.PrimitiveKind() switch
            {
                EdmPrimitiveTypeKind.Boolean => "boolean",
                EdmPrimitiveTypeKind.Byte => "integer",
                EdmPrimitiveTypeKind.SByte => "integer",
                EdmPrimitiveTypeKind.Int16 => "integer",
                EdmPrimitiveTypeKind.Int32 => "integer",
                EdmPrimitiveTypeKind.Int64 => "integer",
                EdmPrimitiveTypeKind.Single => "number",
                EdmPrimitiveTypeKind.Double => "number",
                EdmPrimitiveTypeKind.Decimal => "number",
                EdmPrimitiveTypeKind.String => "string",
                EdmPrimitiveTypeKind.Date => "string",
                EdmPrimitiveTypeKind.DateTimeOffset => "string",
                EdmPrimitiveTypeKind.TimeOfDay => "string",
                EdmPrimitiveTypeKind.Duration => "string",
                EdmPrimitiveTypeKind.Guid => "string",
                EdmPrimitiveTypeKind.Binary => "string",
                _ => "object"
            };
        }

        /// <summary>
        /// Converts EDM type to OpenAPI format.
        /// </summary>
        private string ConvertEdmTypeToOpenApiFormat(IEdmTypeReference edmType)
        {
            if (edmType.IsCollection)
            {
                return null;
            }

            return edmType.PrimitiveKind() switch
            {
                EdmPrimitiveTypeKind.Byte => "uint8",
                EdmPrimitiveTypeKind.SByte => "int8",
                EdmPrimitiveTypeKind.Int16 => "int16",
                EdmPrimitiveTypeKind.Int32 => "int32",
                EdmPrimitiveTypeKind.Int64 => "int64",
                EdmPrimitiveTypeKind.Single => "float",
                EdmPrimitiveTypeKind.Double => "double",
                EdmPrimitiveTypeKind.Decimal => "decimal",
                EdmPrimitiveTypeKind.Date => "date",
                EdmPrimitiveTypeKind.DateTimeOffset => "date-time",
                EdmPrimitiveTypeKind.TimeOfDay => "time",
                EdmPrimitiveTypeKind.Duration => "duration",
                EdmPrimitiveTypeKind.Guid => "uuid",
                EdmPrimitiveTypeKind.Binary => "base64",
                _ => null
            };
        }

        /// <summary>
        /// Checks if a path is a collection endpoint.
        /// </summary>
        private bool IsCollectionEndpoint(string path)
        {
            // Not a collection if it has a key segment or ends with a key pattern
            if (path.Contains("(") && path.Contains(")"))
            {
                return false;
            }

            // Not a collection if it's a property, $value, $ref, $count
            if (path.Contains("/") && (path.EndsWith("/$value") || path.EndsWith("/$ref") || path.EndsWith("/$count")))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds query parameters to an operation.
        /// </summary>
        private void AddQueryParametersToOperation(OpenApiOperation operation)
        {
            var queryParameters = new[]
            {
                CreateQueryParameter("$filter", "Filter results using OData filter expressions", "string", "Name eq 'John'"),
                CreateQueryParameter("$select", "Select specific properties", "string", "Name,Age"),
                CreateQueryParameter("$expand", "Expand related entities", "string", "Orders"),
                CreateQueryParameter("$orderby", "Order results by properties", "string", "Name asc"),
                CreateQueryParameter("$top", "Limit number of results", "integer", null, "int32", 50),
                CreateQueryParameter("$skip", "Skip first N results", "integer", null, "int32", 0),
                CreateQueryParameter("$count", "Include total count", "boolean", null),
                CreateQueryParameter("$search", "Free-text search", "string", null)
            };

            foreach (var param in queryParameters)
            {
                if (!operation.Parameters.Any(p => p.Name == param.Name))
                {
                    operation.Parameters.Add(param);
                }
            }
        }

        /// <summary>
        /// Creates a query parameter.
        /// </summary>
        private OpenApiParameter CreateQueryParameter(string name, string description, string type, string example, string format = null, object defaultValue = null)
        {
            var schema = new OpenApiSchema
            {
                Type = type,
                Format = format
            };

            if (defaultValue != null)
            {
                schema.Default = defaultValue is int intVal ? new Microsoft.OpenApi.Any.OpenApiInteger(intVal) : new Microsoft.OpenApi.Any.OpenApiBoolean((bool)defaultValue);
            }

            return new OpenApiParameter
            {
                Name = name,
                In = ParameterLocation.Query,
                Description = description,
                Required = false,
                Schema = schema,
                Example = example != null ? new Microsoft.OpenApi.Any.OpenApiString(example) : null
            };
        }

        /// <summary>
        /// Extracts path template.
        /// </summary>
        private string ExtractPathTemplate(string rawText, string prefix)
        {
            if (string.IsNullOrEmpty(rawText))
            {
                return null;
            }

            int prefixLength = prefix?.Length ?? 0;
            if (prefixLength > 0 && rawText.Length > prefixLength)
            {
                rawText = rawText.Substring(prefixLength);
            }

            if (!rawText.StartsWith("/"))
            {
                rawText = "/" + rawText;
            }

            return rawText;
        }

        /// <summary>
        /// Gets HTTP methods from endpoint.
        /// </summary>
        private IEnumerable<string> GetHttpMethods(Endpoint endpoint)
        {
            var metadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            if (metadata != null)
            {
                return metadata.HttpMethods;
            }

            return new[] { "GET" };
        }

        /// <summary>
        /// Parses operation type.
        /// </summary>
        private OperationType? ParseOperationType(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "GET" => OperationType.Get,
                "POST" => OperationType.Post,
                "PUT" => OperationType.Put,
                "PATCH" => OperationType.Patch,
                "DELETE" => OperationType.Delete,
                "HEAD" => OperationType.Head,
                "OPTIONS" => OperationType.Options,
                _ => null
            };
        }

        /// <summary>
        /// Creates default operation.
        /// </summary>
        private OpenApiOperation CreateDefaultOperation(Endpoint endpoint, OperationType operationType, string path)
        {
            return new OpenApiOperation
            {
                Summary = $"{operationType} operation for {path}",
                Responses = new OpenApiResponses
                {
                    ["200"] = new OpenApiResponse { Description = "Success" }
                }
            };
        }

        /// <summary>
        /// Enhances operation with endpoint metadata.
        /// </summary>
        private void EnhanceOperationWithMetadata(OpenApiOperation operation, Endpoint endpoint)
        {
            // Could add additional metadata here like:
            // - XML documentation
            // - Produces/Consumes
            // - Authorization
            // - Custom attributes
        }

        private class UnknownSwaggerDocument : InvalidOperationException
        {
            public UnknownSwaggerDocument(string documentName, IEnumerable<string> knownDocuments)
                : base(string.Format(
                    CultureInfo.InvariantCulture,
                    "Unknown Swagger document - \"{0}\". Known Swagger documents: {1}",
                    documentName,
                    string.Join(",", knownDocuments.Select(d => "\"" + d + "\""))
                ))
            {
            }
        }

        private class UnknownODataEdm : InvalidOperationException
        {
            public UnknownODataEdm(string routeName, IEnumerable<string> knownRoutes)
                : base(string.Format(
                    CultureInfo.InvariantCulture,
                    "Unknown OData Edm route - \"{0}\". Known routes: {1}",
                    routeName,
                    string.Join(",", knownRoutes.Select(r => "\"" + r + "\""))
                ))
            {
            }
        }
    }
}
