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
    /// <remarks>
    /// Initializes a new instance of the <see cref="ODataQueryOptionsDocumentFilter"/> class.
    /// </remarks>
    /// <param name="settings">The settings for OData query options.</param>
    public class ODataQueryOptionsDocumentFilter(ODataQueryOptionsSettings? settings = null) : IDocumentFilter
    {
        private readonly ODataQueryOptionsSettings settingsValue = settings ?? new ODataQueryOptionsSettings();

        /// <inheritdoc/>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var path in swaggerDoc.Paths)
            {
                if (path.Value?.Operations is not { } operations)
                {
                    continue;
                }

                if (operations.TryGetValue(HttpMethod.Get, out var getOperation)
                    && getOperation != null
                    && IsCollectionEndpoint(path.Key))
                {
                    this.AddODataQueryParameters(getOperation);
                }
            }
        }

        /// <summary>
        /// Determines if the path represents a collection endpoint.
        /// </summary>
        private static bool IsCollectionEndpoint(string path)
        {
            // Collection endpoints typically don't end with )} which indicates a single entity key
            // and don't contain $ref or property access.
            if (path.Contains("({") || path.Contains("($"))
            {
                return false;
            }

            // Check if it ends with a key segment pattern.
            if (path.EndsWith(')') && path.Contains('('))
            {
                return false;
            }

            // It's a collection if it doesn't have a key segment.
            return true;
        }

        /// <summary>
        /// Adds OData query parameters to the operation.
        /// </summary>
        private void AddODataQueryParameters(OpenApiOperation operation)
        {
            var operationParameters = operation.Parameters ??= [];

            var parameters = new List<OpenApiParameter>();

            if (this.settingsValue.EnableFilter)
            {
                parameters.Add(this.CreateFilterParameter());
            }

            if (this.settingsValue.EnableSelect)
            {
                parameters.Add(this.CreateSelectParameter());
            }

            if (this.settingsValue.EnableExpand)
            {
                parameters.Add(this.CreateExpandParameter());
            }

            if (this.settingsValue.EnableOrderBy)
            {
                parameters.Add(this.CreateOrderByParameter());
            }

            if (this.settingsValue.EnableTop)
            {
                parameters.Add(this.CreateTopParameter());
            }

            if (this.settingsValue.EnableSkip)
            {
                parameters.Add(CreateSkipParameter());
            }

            if (this.settingsValue.EnableCount)
            {
                parameters.Add(CreateCountParameter());
            }

            if (this.settingsValue.EnableSearch)
            {
                parameters.Add(CreateSearchParameter());
            }

            if (this.settingsValue.EnableFormat)
            {
                parameters.Add(CreateFormatParameter());
            }

            // Add parameters to operation.
            foreach (var parameter in parameters.Where(param => !operationParameters.Any(existing => existing.Name == param.Name)))
            {
                operationParameters.Add(parameter);
            }

            // Add example response with @odata.count, @odata.nextLink if pagination enabled.
            if (this.settingsValue.EnablePagination
                && operation.Responses is { } responses
                && responses.TryGetValue("200", out var response)
                && response != null)
            {
                AddPaginationResponseExample(response);
            }
        }

        private OpenApiParameter CreateFilterParameter() => new()
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
            Example = this.settingsValue.FilterExample != null ? JsonValue.Create(this.settingsValue.FilterExample) : null
        };

        private OpenApiParameter CreateSelectParameter() => new()
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
            Example = this.settingsValue.SelectExample != null ? JsonValue.Create(this.settingsValue.SelectExample) : null
        };

        private OpenApiParameter CreateExpandParameter() => new()
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
            Example = this.settingsValue.ExpandExample != null ? JsonValue.Create(this.settingsValue.ExpandExample) : null
        };

        private OpenApiParameter CreateOrderByParameter() => new()
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
            Example = this.settingsValue.OrderByExample != null ? JsonValue.Create(this.settingsValue.OrderByExample) : null
        };

        private OpenApiParameter CreateTopParameter() => new()
        {
            Name = "$top",
            In = ParameterLocation.Query,
            Description = $"Limit the number of results. Maximum: {this.settingsValue.MaxTop}",
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int32",
                Maximum = this.settingsValue.MaxTop.ToString(CultureInfo.InvariantCulture)
            },
            Example = JsonValue.Create(this.settingsValue.DefaultTop)
        };

        private static OpenApiParameter CreateSkipParameter() => new()
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

        private static OpenApiParameter CreateCountParameter() => new()
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

        private static OpenApiParameter CreateSearchParameter() => new()
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

        private static OpenApiParameter CreateFormatParameter() => new()
        {
            Name = "$format",
            In = ParameterLocation.Query,
            Description = "Specify response format. " +
                    "Examples: application/json, application/json;odata.metadata=minimal",
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum =
                    [
                        JsonValue.Create("application/json")!,
                        JsonValue.Create("application/json;odata.metadata=none")!,
                        JsonValue.Create("application/json;odata.metadata=minimal")!,
                        JsonValue.Create("application/json;odata.metadata=full")!
                    ]
            }
        };

        private static void AddPaginationResponseExample(IOpenApiResponse response)
        {
            if (response.Content == null || !response.Content.Any())
            {
                return;
            }

            // Add odata.nextLink and odata.count to the schema description.
            foreach (var content in response.Content.Values)
            {
                if (content.Schema?.Items != null)
                {
                    // Collection response - add description about odata annotations.
                    content.Schema.Description ??= "Response includes OData annotations: @odata.count, @odata.nextLink";
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
        public bool EnableSearch { get; set; }

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
