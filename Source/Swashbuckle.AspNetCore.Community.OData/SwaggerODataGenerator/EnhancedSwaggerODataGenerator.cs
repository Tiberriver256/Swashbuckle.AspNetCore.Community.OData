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
using System.Net.Http;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OpenApi;
using Microsoft.OpenApi.OData;
using Swashbuckle.AspNetCore.Community.OData.DependencyInjection;
using Swashbuckle.AspNetCore.Community.OData.ODataRouting;
using Swashbuckle.AspNetCore.Community.OData.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace Swashbuckle.AspNetCore.Community.OData.SwaggerODataGenerator
{
    /// <summary>
    /// Enhanced Swagger generator for OData that uses actual endpoint routing data
    /// to produce accurate OpenAPI documentation with full OData query support.
    /// </summary>
    public class EnhancedSwaggerODataGenerator : ISwaggerProvider
    {
        private readonly SwaggerODataGeneratorOptions generatorOptions;
        private readonly ODataQueryOptionsSettings queryOptionsSettings;
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedSwaggerODataGenerator"/> class.
        /// </summary>
        /// <param name="options">The generator options.</param>
        /// <param name="serviceProvider">The service provider for accessing endpoint data.</param>
        public EnhancedSwaggerODataGenerator(
            IOptions<SwaggerODataGeneratorOptions> options,
            IServiceProvider serviceProvider)
        {
            this.generatorOptions = options?.Value ?? new SwaggerODataGeneratorOptions();
            this.queryOptionsSettings = this.generatorOptions.QueryOptionsSettings ?? new ODataQueryOptionsSettings();
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc/>
        public OpenApiDocument GetSwagger(string documentName, string? host = null, string? basePath = null)
        {
            if (!this.generatorOptions.SwaggerDocs.TryGetValue(documentName, out var docInfo))
            {
                throw new UnknownSwaggerDocument(
                    documentName,
                    this.generatorOptions.SwaggerDocs.Select(d => d.Key)
                );
            }

            var (odataRoute, apiInfo) = docInfo;

            if (!this.generatorOptions.EdmModels.TryGetValue(odataRoute, out var edmModel))
            {
                throw new UnknownODataEdm(
                    odataRoute,
                    this.generatorOptions.EdmModels.Select(d => d.Key)
                );
            }

            // Get endpoint data source if available
            var endpointDataSource = this.serviceProvider.GetService<EndpointDataSource>();

            // Create the OpenAPI document using endpoint-aware path provider
            var document = this.CreateOpenApiDocument(edmModel, odataRoute, apiInfo, endpointDataSource, host, basePath);

            return document;
        }

        /// <summary>
        /// Creates an OpenAPI document from the EDM model with endpoint routing data.
        /// </summary>
        private OpenApiDocument CreateOpenApiDocument(
            IEdmModel model,
            string routePrefix,
            OpenApiInfo info,
            EndpointDataSource? endpointDataSource,
            string? host,
            string? basePath)
        {
            // Build service root URI
            var serviceRoot = BuildServiceRoot(host, basePath, routePrefix);

            // Create path provider with endpoint data
            Microsoft.OpenApi.OData.Edm.IODataPathProvider? pathProvider = endpointDataSource != null
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
            document.Servers =
            [
                new OpenApiServer { Url = $"/{routePrefix}" }
            ];

            // Enhance with endpoint-specific metadata if we have endpoint data
            if (endpointDataSource != null)
            {
                EnhanceDocumentWithEndpointMetadata(document, endpointDataSource, routePrefix);
            }

            // Add OData query options to collection endpoints
            this.AddODataQueryOptions(document);

            // Add property access, value, and reference paths
            AddMissingODataPaths(document, model);

            return document;
        }

        /// <summary>
        /// Builds the service root URI.
        /// </summary>
        private static Uri BuildServiceRoot(string? host, string? basePath, string routePrefix)
        {
            var scheme = "https";
            var hostName = "localhost";
            var port = -1;

            if (!string.IsNullOrWhiteSpace(host))
            {
                if (Uri.TryCreate(host, UriKind.Absolute, out var absoluteHost))
                {
                    scheme = absoluteHost.Scheme;
                    hostName = absoluteHost.Host;
                    port = absoluteHost.IsDefaultPort ? -1 : absoluteHost.Port;
                }
                else if (Uri.TryCreate($"https://{host}", UriKind.Absolute, out var hostWithDefaultScheme))
                {
                    hostName = hostWithDefaultScheme.Host;
                    port = hostWithDefaultScheme.IsDefaultPort ? -1 : hostWithDefaultScheme.Port;
                }
            }

            var pathParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                pathParts.Add(basePath.Trim('/'));
            }

            if (!string.IsNullOrWhiteSpace(routePrefix))
            {
                pathParts.Add(routePrefix.Trim('/'));
            }

            var combinedPath = pathParts.Count == 0
                ? "/"
                : "/" + string.Join("/", pathParts);

            return new UriBuilder(scheme, hostName, port)
            {
                Path = combinedPath
            }.Uri;
        }

        /// <summary>
        /// Enhances the document with metadata from actual endpoints.
        /// </summary>
        private static void EnhanceDocumentWithEndpointMetadata(OpenApiDocument document, EndpointDataSource endpointDataSource, string routePrefix)
        {
            foreach (var endpoint in endpointDataSource.Endpoints)
            {
                var metadata = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.OData.Routing.IODataRoutingMetadata>();
                if (metadata == null || !string.Equals(metadata.Prefix, routePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (endpoint is not RouteEndpoint routeEndpoint)
                {
                    continue;
                }

                // Extract path
                var pathTemplate = ExtractPathTemplate(routeEndpoint.RoutePattern.RawText, routePrefix);
                if (string.IsNullOrEmpty(pathTemplate)
                    || !document.Paths.TryGetValue(pathTemplate, out var pathItem)
                    || pathItem == null)
                {
                    continue;
                }

                var pathOperations = pathItem.Operations;
                if (pathOperations == null)
                {
                    continue;
                }

                var operationTypes = GetHttpMethods(endpoint)
                    .Select(ParseHttpMethod)
                    .OfType<HttpMethod>();

                foreach (var operationType in operationTypes)
                {
                    if (!pathOperations.ContainsKey(operationType))
                    {
                        // Create missing operation based on HTTP method conventions.
                        pathOperations[operationType] = CreateDefaultOperation(operationType, pathTemplate);
                    }
                }

                // Enhance existing operations with endpoint metadata.
                foreach (var operation in pathOperations.Values)
                {
                    if (operation != null)
                    {
                        EnhanceOperationWithMetadata(operation, endpoint);
                    }
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
                if (path.Value?.Operations is not { } operations)
                {
                    continue;
                }

                // Only add to collection endpoints (not single entity by key).
                if (IsCollectionEndpoint(path.Key)
                    && operations.TryGetValue(HttpMethod.Get, out var getOperation)
                    && getOperation != null)
                {
                    this.AddQueryParametersToOperation(getOperation);
                }
            }
        }

        /// <summary>
        /// Adds missing OData paths like property access, $value, $ref.
        /// </summary>
        private static void AddMissingODataPaths(OpenApiDocument document, IEdmModel model)
        {
            // Get all entity sets and singletons
            var entitySets = model.EntityContainer?.EntitySets() ?? [];
            var singletons = model.EntityContainer?.Singletons() ?? [];

            var pathsToAdd = new Dictionary<string, OpenApiPathItem>();

            foreach (var entitySet in entitySets)
            {
                var entityType = entitySet.EntityType;

                // Add paths for each entity (key access)
                var entityByKeyPath = $"/{entitySet.Name}({{key}})";

                if (document.Paths.ContainsKey(entityByKeyPath))
                {
                    // Add property access paths
                    AddPropertyAccessPaths(pathsToAdd, entityType, entityByKeyPath);

                    // Add $value path
                    AddValuePath(pathsToAdd, entityByKeyPath);

                    // Add $ref path for navigation properties
                    AddNavigationReferencePaths(pathsToAdd, entityType, entityByKeyPath);
                }
            }

            foreach (var singleton in singletons)
            {
                var entityType = singleton.EntityType;
                var singletonPath = $"/{singleton.Name}";

                if (document.Paths.ContainsKey(singletonPath))
                {
                    // Add property access paths for singleton
                    AddPropertyAccessPaths(pathsToAdd, entityType, singletonPath);

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
        private static void AddPropertyAccessPaths(Dictionary<string, OpenApiPathItem> paths, IEdmEntityType entityType, string basePath)
        {
            foreach (var property in entityType.DeclaredProperties.OfType<IEdmStructuralProperty>())
            {
                var propertyPath = $"{basePath}/{property.Name}";
                if (paths.ContainsKey(propertyPath))
                {
                    continue;
                }

                var pathItem = new OpenApiPathItem();
                var operations = pathItem.Operations ??= [];

                // GET for property value
                operations[HttpMethod.Get] = new OpenApiOperation
                {
                    Summary = $"Get {property.Name} property value",
                    OperationId = $"Get{entityType.Name}{property.Name}",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Success",
                            Content = new Dictionary<string, IOpenApiMediaType>
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
                var valuePath = $"{propertyPath}/$value";
                if (!paths.ContainsKey(valuePath))
                {
                    var valuePathItem = new OpenApiPathItem();
                    var valueOperations = valuePathItem.Operations ??= [];
                    valueOperations[HttpMethod.Get] = new OpenApiOperation
                    {
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
        private static void AddValuePath(Dictionary<string, OpenApiPathItem> paths, string basePath)
        {
            var valuePath = $"{basePath}/$value";
            if (paths.ContainsKey(valuePath))
            {
                return;
            }

            var pathItem = new OpenApiPathItem();
            var operations = pathItem.Operations ??= [];
            operations[HttpMethod.Get] = new OpenApiOperation
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
        private static void AddNavigationReferencePaths(Dictionary<string, OpenApiPathItem> paths, IEdmEntityType entityType, string basePath)
        {
            foreach (var navProperty in entityType.DeclaredNavigationProperties())
            {
                var refPath = $"{basePath}/{navProperty.Name}/$ref";
                if (paths.ContainsKey(refPath))
                {
                    continue;
                }

                var pathItem = new OpenApiPathItem();
                var operations = pathItem.Operations ??= [];

                // GET $ref
                operations[HttpMethod.Get] = new OpenApiOperation
                {
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
                operations[HttpMethod.Put] = new OpenApiOperation
                {
                    Summary = $"Update {navProperty.Name} reference",
                    Responses = new OpenApiResponses
                    {
                        ["204"] = new OpenApiResponse { Description = "Reference updated" }
                    }
                };

                // DELETE $ref (remove reference)
                operations[HttpMethod.Delete] = new OpenApiOperation
                {
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
        private static OpenApiSchema CreatePropertySchema(IEdmStructuralProperty property) => CreateSchemaForEdmType(property.Type);

        /// <summary>
        /// Creates a schema for an EDM type reference.
        /// Used for property types and array item schemas.
        /// </summary>
        private static OpenApiSchema CreateSchemaForEdmType(IEdmTypeReference edmType)
        {
            if (edmType.IsCollection())
            {
                var elementType = edmType.AsCollection().ElementType();

                return new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = CreateSchemaForEdmType(elementType)
                };
            }

            return new OpenApiSchema
            {
                Type = ConvertEdmTypeToOpenApiType(edmType),
                Format = ConvertEdmTypeToOpenApiFormat(edmType)
            };
        }

#pragma warning disable IDE0072

        /// <summary>
        /// Converts EDM type to OpenAPI type.
        /// </summary>
        private static JsonSchemaType ConvertEdmTypeToOpenApiType(IEdmTypeReference edmType)
        {
            if (edmType.IsCollection())
            {
                return JsonSchemaType.Array;
            }

            return edmType.PrimitiveKind() switch
            {
                EdmPrimitiveTypeKind.Boolean => JsonSchemaType.Boolean,
                EdmPrimitiveTypeKind.Byte => JsonSchemaType.Integer,
                EdmPrimitiveTypeKind.SByte => JsonSchemaType.Integer,
                EdmPrimitiveTypeKind.Int16 => JsonSchemaType.Integer,
                EdmPrimitiveTypeKind.Int32 => JsonSchemaType.Integer,
                EdmPrimitiveTypeKind.Int64 => JsonSchemaType.Integer,
                EdmPrimitiveTypeKind.Single => JsonSchemaType.Number,
                EdmPrimitiveTypeKind.Double => JsonSchemaType.Number,
                EdmPrimitiveTypeKind.Decimal => JsonSchemaType.Number,
                EdmPrimitiveTypeKind.String => JsonSchemaType.String,
                EdmPrimitiveTypeKind.Date => JsonSchemaType.String,
                EdmPrimitiveTypeKind.DateTimeOffset => JsonSchemaType.String,
                EdmPrimitiveTypeKind.TimeOfDay => JsonSchemaType.String,
                EdmPrimitiveTypeKind.Duration => JsonSchemaType.String,
                EdmPrimitiveTypeKind.Guid => JsonSchemaType.String,
                EdmPrimitiveTypeKind.Binary => JsonSchemaType.String,
                _ => JsonSchemaType.Object
            };
        }

        /// <summary>
        /// Converts EDM type to OpenAPI format.
        /// </summary>
        private static string? ConvertEdmTypeToOpenApiFormat(IEdmTypeReference edmType)
        {
            if (edmType.IsCollection())
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

#pragma warning restore IDE0072

        /// <summary>
        /// Checks if a path is a collection endpoint.
        /// </summary>
        private static bool IsCollectionEndpoint(string path)
        {
            // Not a collection if it has a key segment or ends with a key pattern
            if (path.Contains('(') && path.Contains(')'))
            {
                return false;
            }

            // Not a collection if it's a property, $value, $ref, $count
            if (path.Contains('/')
                && (path.EndsWith("/$value", StringComparison.Ordinal)
                    || path.EndsWith("/$ref", StringComparison.Ordinal)
                    || path.EndsWith("/$count", StringComparison.Ordinal)))
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
            var operationParameters = operation.Parameters ??= [];

            var queryParameters = new List<OpenApiParameter>();

            if (this.queryOptionsSettings.EnableFilter)
            {
                queryParameters.Add(CreateQueryParameter("$filter", "Filter results using OData filter expressions", JsonSchemaType.String, this.queryOptionsSettings.FilterExample));
            }

            if (this.queryOptionsSettings.EnableSelect)
            {
                queryParameters.Add(CreateQueryParameter("$select", "Select specific properties", JsonSchemaType.String, this.queryOptionsSettings.SelectExample));
            }

            if (this.queryOptionsSettings.EnableExpand)
            {
                queryParameters.Add(CreateQueryParameter("$expand", "Expand related entities", JsonSchemaType.String, this.queryOptionsSettings.ExpandExample));
            }

            if (this.queryOptionsSettings.EnableOrderBy)
            {
                queryParameters.Add(CreateQueryParameter("$orderby", "Order results by properties", JsonSchemaType.String, this.queryOptionsSettings.OrderByExample));
            }

            if (this.queryOptionsSettings.EnableTop)
            {
                queryParameters.Add(CreateQueryParameter("$top", "Limit number of results", JsonSchemaType.Integer, null, "int32", this.queryOptionsSettings.DefaultTop, this.queryOptionsSettings.MaxTop));
            }

            if (this.queryOptionsSettings.EnableSkip)
            {
                queryParameters.Add(CreateQueryParameter("$skip", "Skip first N results", JsonSchemaType.Integer, null, "int32", 0));
            }

            if (this.queryOptionsSettings.EnableCount)
            {
                queryParameters.Add(CreateQueryParameter("$count", "Include total count", JsonSchemaType.Boolean, null));
            }

            if (this.queryOptionsSettings.EnableSearch)
            {
                queryParameters.Add(CreateQueryParameter("$search", "Free-text search", JsonSchemaType.String, null));
            }

            foreach (var parameter in queryParameters.Where(param => !operationParameters.Any(existing => existing.Name == param.Name)))
            {
                operationParameters.Add(parameter);
            }
        }

        /// <summary>
        /// Creates a query parameter.
        /// </summary>
        private static OpenApiParameter CreateQueryParameter(string name, string description, JsonSchemaType type, string? example, string? format = null, object? defaultValue = null, decimal? maximum = null)
        {
            var schema = new OpenApiSchema
            {
                Type = type,
                Format = format,
                Maximum = maximum?.ToString(CultureInfo.InvariantCulture)
            };

            if (defaultValue is int intValue)
            {
                schema.Default = JsonValue.Create(intValue);
            }
            else if (defaultValue is bool boolValue)
            {
                schema.Default = JsonValue.Create(boolValue);
            }

            return new OpenApiParameter
            {
                Name = name,
                In = ParameterLocation.Query,
                Description = description,
                Required = false,
                Schema = schema,
                Example = example != null ? JsonValue.Create(example) : null
            };
        }

        /// <summary>
        /// Extracts path template.
        /// </summary>
        private static string? ExtractPathTemplate(string? rawText, string prefix)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return null;
            }

            return RemovePrefixSegment(rawText, prefix);
        }

        private static string RemovePrefixSegment(string rawText, string prefix)
        {
            var normalizedPath = rawText.Trim();
            if (!normalizedPath.StartsWith('/'))
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
                var remaining = normalizedPath[prefixWithTrailingSlash.Length..];
                return "/" + remaining.TrimStart('/');
            }

            return normalizedPath;
        }

        /// <summary>
        /// Gets HTTP methods from endpoint.
        /// </summary>
        private static IEnumerable<string> GetHttpMethods(Endpoint endpoint)
        {
            var metadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            if (metadata != null)
            {
                return metadata.HttpMethods;
            }

            return ["GET"];
        }

        /// <summary>
        /// Parses operation type.
        /// </summary>
        private static HttpMethod? ParseHttpMethod(string method) => method.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "PATCH" => HttpMethod.Patch,
            "DELETE" => HttpMethod.Delete,
            "HEAD" => HttpMethod.Head,
            "OPTIONS" => HttpMethod.Options,
            _ => null
        };

        /// <summary>
        /// Creates default operation.
        /// </summary>
        private static OpenApiOperation CreateDefaultOperation(HttpMethod operationType, string path) => new()
        {
            Summary = $"{operationType.Method} operation for {path}",
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse { Description = "Success" }
            }
        };

        /// <summary>
        /// Enhances operation with endpoint metadata.
        /// </summary>
        private static void EnhanceOperationWithMetadata(OpenApiOperation operation, Endpoint endpoint)
        {
            _ = operation;
            _ = endpoint;
            // Could add additional metadata here like:
            // - XML documentation
            // - Produces/Consumes
            // - Authorization
            // - Custom attributes
        }

        private sealed class UnknownSwaggerDocument(string documentName, IEnumerable<string> knownDocuments) : InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Unknown Swagger document - \"{0}\". Known Swagger documents: {1}",
                    documentName,
                    string.Join(",", knownDocuments.Select(d => "\"" + d + "\""))
                ))
        {
        }

        private sealed class UnknownODataEdm(string routeName, IEnumerable<string> knownRoutes) : InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Unknown OData Edm route - \"{0}\". Known routes: {1}",
                    routeName,
                    string.Join(",", knownRoutes.Select(r => "\"" + r + "\""))
                ))
        {
        }
    }
}
