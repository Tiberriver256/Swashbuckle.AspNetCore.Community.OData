// -----------------------------------------------------------------------------
// <copyright file="ODataQueryOptionsDocumentFilter.cs" company=".NET Foundation">
//      Copyright (c) .NET Foundation and Contributors. All rights reserved.
//      Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// </copyright>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.AspNetCore.Community.OData.OpenApi
{
    /// <summary>
    /// Adds OData query options ($filter, $select, $expand, $orderby, $top, $skip, $count, $search)
    /// to GET operations for collection-style OData paths.
    /// </summary>
    public class ODataQueryOptionsDocumentFilter : IDocumentFilter
    {
        private readonly ODataQueryOptionsSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataQueryOptionsDocumentFilter"/> class.
        /// </summary>
        /// <param name="settings">The settings for OData query options.</param>
        public ODataQueryOptionsDocumentFilter(ODataQueryOptionsSettings settings = null)
        {
            _settings = settings ?? new ODataQueryOptionsSettings();
        }

        /// <inheritdoc/>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var path in swaggerDoc.Paths)
            {
                if (path.Value.Operations.TryGetValue(HttpMethod.Get, out var getOperation)
                    && IsCollectionEndpoint(path.Key))
                {
                    AddODataQueryParameters(getOperation);
                }
            }
        }

        /// <summary>
        /// Determines if the path represents a collection endpoint.
        /// </summary>
        private bool IsCollectionEndpoint(string path)
        {
            // Collection endpoints typically don't end with )} which indicates a single entity key
            // and don't contain $ref or property access
            if (path.Contains("({") || path.Contains("($"))
            {
                return false;
            }

            // Check if it ends with a key segment pattern
            if (path.EndsWith(")") && path.Contains("("))
            {
                return false;
            }

            // It's a collection if it doesn't have a key segment
            return true;
        }

        /// <summary>
        /// Adds OData query parameters to the operation.
        /// </summary>
        private void AddODataQueryParameters(OpenApiOperation operation)
        {
            operation.Parameters ??= new List<IOpenApiParameter>();

            var parameters = new List<IOpenApiParameter>();

            if (_settings.EnableFilter)
            {
                parameters.Add(CreateFilterParameter());
            }

            if (_settings.EnableSelect)
            {
                parameters.Add(CreateSelectParameter());
            }

            if (_settings.EnableExpand)
            {
                parameters.Add(CreateExpandParameter());
            }

            if (_settings.EnableOrderBy)
            {
                parameters.Add(CreateOrderByParameter());
            }

            if (_settings.EnableTop)
            {
                parameters.Add(CreateTopParameter());
            }

            if (_settings.EnableSkip)
            {
                parameters.Add(CreateSkipParameter());
            }

            if (_settings.EnableCount)
            {
                parameters.Add(CreateCountParameter());
            }

            if (_settings.EnableSearch)
            {
                parameters.Add(CreateSearchParameter());
            }

            if (_settings.EnableFormat)
            {
                parameters.Add(CreateFormatParameter());
            }

            // Add parameters to operation
            foreach (var parameter in parameters.Where(param => !operation.Parameters.Any(existing => existing.Name == param.Name)))
            {
                operation.Parameters.Add(parameter);
            }

            // Add example response with @odata.count, @odata.nextLink if pagination enabled
            if (_settings.EnablePagination && operation.Responses.TryGetValue("200", out var response))
            {
                AddPaginationResponseExample(response);
            }
        }

        private IOpenApiParameter CreateFilterParameter()
        {
            return new OpenApiParameter
            {
                Name = "$filter",
                In = ParameterLocation.Query,
                Description = "Filter the results using OData filter expressions. " +
                    "Examples: Name eq 'John', Age gt 18, contains(Name, 'Smith')",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                },
                Example = _settings.FilterExample != null ? JsonValue.Create(_settings.FilterExample) : null
            };
        }

        private IOpenApiParameter CreateSelectParameter()
        {
            return new OpenApiParameter
            {
                Name = "$select",
                In = ParameterLocation.Query,
                Description = "Select specific properties to include in the response. " +
                    "Example: $select=Name,Age,Email",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                },
                Example = _settings.SelectExample != null ? JsonValue.Create(_settings.SelectExample) : null
            };
        }

        private IOpenApiParameter CreateExpandParameter()
        {
            return new OpenApiParameter
            {
                Name = "$expand",
                In = ParameterLocation.Query,
                Description = "Expand related entities. " +
                    "Examples: $expand=Orders, $expand=Orders($filter=Amount gt 100)",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                },
                Example = _settings.ExpandExample != null ? JsonValue.Create(_settings.ExpandExample) : null
            };
        }

        private IOpenApiParameter CreateOrderByParameter()
        {
            return new OpenApiParameter
            {
                Name = "$orderby",
                In = ParameterLocation.Query,
                Description = "Order results by properties. " +
                    "Examples: $orderby=Name, $orderby=Age desc, $orderby=Name asc,Age desc",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                },
                Example = _settings.OrderByExample != null ? JsonValue.Create(_settings.OrderByExample) : null
            };
        }

        private IOpenApiParameter CreateTopParameter()
        {
            return new OpenApiParameter
            {
                Name = "$top",
                In = ParameterLocation.Query,
                Description = $"Limit the number of results. Maximum: {_settings.MaxTop}",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer,
                    Format = "int32",
                    Maximum = _settings.MaxTop.ToString(CultureInfo.InvariantCulture)
                },
                Example = JsonValue.Create(_settings.DefaultTop)
            };
        }

        private IOpenApiParameter CreateSkipParameter()
        {
            return new OpenApiParameter
            {
                Name = "$skip",
                In = ParameterLocation.Query,
                Description = "Skip the first N results (for pagination).",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Integer,
                    Format = "int32"
                },
                Example = JsonValue.Create(0)
            };
        }

        private IOpenApiParameter CreateCountParameter()
        {
            return new OpenApiParameter
            {
                Name = "$count",
                In = ParameterLocation.Query,
                Description = "Include total count of results in response. " +
                    "Set to true to include @odata.count in response.",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.Boolean
                },
                Example = JsonValue.Create(false)
            };
        }

        private IOpenApiParameter CreateSearchParameter()
        {
            return new OpenApiParameter
            {
                Name = "$search",
                In = ParameterLocation.Query,
                Description = "Free-text search across entity properties. " +
                    "Example: $search=blue shirt",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                }
            };
        }

        private IOpenApiParameter CreateFormatParameter()
        {
            return new OpenApiParameter
            {
                Name = "$format",
                In = ParameterLocation.Query,
                Description = "Specify response format. " +
                    "Examples: application/json, application/json;odata.metadata=minimal",
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Enum = new List<JsonNode>
                    {
                        JsonValue.Create("application/json")!,
                        JsonValue.Create("application/json;odata.metadata=none")!,
                        JsonValue.Create("application/json;odata.metadata=minimal")!,
                        JsonValue.Create("application/json;odata.metadata=full")!
                    }
                }
            };
        }

        private static void AddPaginationResponseExample(IOpenApiResponse response)
        {
            if (response.Content == null || !response.Content.Any())
            {
                return;
            }

            // Add odata.nextLink and odata.count to the schema description
            foreach (var content in response.Content.Values)
            {
                if (content.Schema?.Items != null)
                {
                    // Collection response - add description about odata annotations
                    content.Schema.Description = content.Schema.Description ?? "Response includes OData annotations: @odata.count, @odata.nextLink";
                }
            }
        }
    }

    /// <summary>
    /// Settings for OData query options documentation.
    /// </summary>
    public class ODataQueryOptionsSettings
    {
        /// <summary>
        /// Enable $filter parameter. Default: true.
        /// </summary>
        public bool EnableFilter { get; set; } = true;

        /// <summary>
        /// Enable $select parameter. Default: true.
        /// </summary>
        public bool EnableSelect { get; set; } = true;

        /// <summary>
        /// Enable $expand parameter. Default: true.
        /// </summary>
        public bool EnableExpand { get; set; } = true;

        /// <summary>
        /// Enable $orderby parameter. Default: true.
        /// </summary>
        public bool EnableOrderBy { get; set; } = true;

        /// <summary>
        /// Enable $top parameter. Default: true.
        /// </summary>
        public bool EnableTop { get; set; } = true;

        /// <summary>
        /// Maximum value for $top. Default: 100.
        /// </summary>
        public int MaxTop { get; set; } = 100;

        /// <summary>
        /// Default value for $top. Default: 50.
        /// </summary>
        public int DefaultTop { get; set; } = 50;

        /// <summary>
        /// Enable $skip parameter. Default: true.
        /// </summary>
        public bool EnableSkip { get; set; } = true;

        /// <summary>
        /// Enable $count parameter. Default: true.
        /// </summary>
        public bool EnableCount { get; set; } = true;

        /// <summary>
        /// Enable $search parameter. Default: false.
        /// </summary>
        public bool EnableSearch { get; set; } = false;

        /// <summary>
        /// Enable $format parameter. Default: true.
        /// </summary>
        public bool EnableFormat { get; set; } = true;

        /// <summary>
        /// Add pagination response examples. Default: true.
        /// </summary>
        public bool EnablePagination { get; set; } = true;

        /// <summary>
        /// Example filter expression for documentation.
        /// </summary>
        public string FilterExample { get; set; } = "Name eq 'John'";

        /// <summary>
        /// Example select expression for documentation.
        /// </summary>
        public string SelectExample { get; set; } = "Name,Age,Email";

        /// <summary>
        /// Example expand expression for documentation.
        /// </summary>
        public string ExpandExample { get; set; } = "Orders";

        /// <summary>
        /// Example orderby expression for documentation.
        /// </summary>
        public string OrderByExample { get; set; } = "Name asc";
    }
}
