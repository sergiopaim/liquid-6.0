using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class LiquidApiExtension
    {
        /// <summary>
        /// Adds APIs Conventions to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseOpenApiConventions(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OpenApiMiddleware>();
        }
    }

    /// <summary>
    /// Builds a middleware pipeline after receiving the pipeline from a swagger pipeline
    /// </summary>
    public class OpenApiMiddleware
    {
        static readonly string compatibleVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
        static readonly string COMPATIBLE_VERSION_HEADER = "app-compatible-version";

        private readonly RequestDelegate _next;
        private readonly ISwaggerProvider _swaggerProvider;
        private readonly TemplateMatcher _requestMatcher;

        /// <summary>
        /// Middleware of Open Api
        /// </summary>
        /// <param name="next"></param>
        /// <param name="swaggerProvider"></param>
        public OpenApiMiddleware(RequestDelegate next, ISwaggerProvider swaggerProvider)
        {
            _next = next;
            _swaggerProvider = swaggerProvider;
            _requestMatcher = new TemplateMatcher(TemplateParser.Parse("swagger/{documentName}/swagger.json"), new RouteValueDictionary());
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// (Intercept the swagger call)
        /// </summary>
        /// <param name="context">given request path</param>
        /// <returns>A Task that completes when the middleware has completed processing.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (!RequestingSwaggerDocument(context?.Request, out string documentName))
            {
                context.Response.Headers.AccessControlExposeHeaders = COMPATIBLE_VERSION_HEADER;
                context.Response.Headers.Add(COMPATIBLE_VERSION_HEADER, compatibleVersion);
                await _next(context);
                return;
            }

            string basePath;
            if (!string.IsNullOrEmpty(Swagger.Config?.BasePath))
            {
                basePath = Swagger.Config.BasePath;

                if (WorkBench.IsIntegrationEnvironment)
                    basePath = "/int" + basePath;
                else if (WorkBench.IsQualityEnvironment)
                    basePath = "/qa" + basePath;
                else if (WorkBench.IsDemonstrationEnvironment)
                    basePath = "/demo" + basePath;
                else
                    basePath = null;
            }
            else
            {
                basePath = null;
            }

            OpenApiDocument swagger = null;

            if (_swaggerProvider is not null)
            {
                swagger = _swaggerProvider.GetSwagger(documentName, null, basePath);
                swagger = FilterSchemas(swagger);
            }

            await RespondWithSwaggerJson(context.Response, swagger);
        }

        internal static OpenApiDocument FilterSchemas(OpenApiDocument swagger)
        {
            KeyValuePair<string, OpenApiSchema>[] schemas = new KeyValuePair<string, OpenApiSchema>[200];
            string[] filteredClasses = new string[]
            {
                "Assembly",
                "CallingConventions",
                "ConstructorInfo",
                "CustomAttributeData",
                "CustomAttributeNamedArgument",
                "CustomAttributeTypedArgument",
                "EventAttributes",
                "EventInfo",
                "Expression",
                "ExpressionType",
                "FieldAttributes",
                "FieldInfo",
                "GenericParameterAttributes",
                "ICustomAttributeProvider",
                "IntPtr",
                "IPropertyValidator",
                "IRuleComponent",
                "IStringSource",
                "IValidationRule",
                "LambdaExpression",
                "LayoutKind",
                "MemberInfo",
                "MemberTypes",
                "MemberTypes",
                "MemberTypes",
                "MethodAttributes",
                "MethodBase",
                "MethodImplAttributes",
                "MethodInfo",
                "Module",
                "ModuleHandle",
                "Object",
                "ParameterAttributes",
                "ParameterExpression",
                "ParameterInfo",
                "PropertyAttributes",
                "PropertyInfo",
                "PropertyValidatorContextBooleanFunc",
                "PropertyValidatorContextCancellationTokenBooleanTaskFunc",
                "PropertyValidatorContextObjectFunc",
                "PropertyValidatorContextSeverityFunc",
                "PropertyValidatorOptions",
                "RuntimeFieldHandle",
                "RuntimeMethodHandle",
                "RuntimeTypeHandle",
                "SecurityRuleSet",
                "Severity",
                "StructLayoutAttribute",
                "Type",
                "TypeAttributes",
                "TypeInfo"
            };

            swagger.Components.Schemas.CopyTo(schemas, 0);

            foreach (var schema in schemas)
            {
                if (schema.Value is not null)
                {
                    if (Array.Exists(filteredClasses, x => x == schema.Key))
                    {
                        swagger.Components.Schemas.Remove(schema.Key);
                    }
                }
            }
            return swagger;
        }

        /// <summary>
        /// Get a swagger document on http request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="documentName"></param>
        /// <returns></returns>
        private bool RequestingSwaggerDocument(HttpRequest request, out string documentName)
        {
            documentName = null;
            if (request.Method != "GET") return false;

            var routeValues = new RouteValueDictionary();
            if (!_requestMatcher.TryMatch(request.Path, routeValues) || !routeValues.ContainsKey("documentName")) return false;

            documentName = routeValues["documentName"].ToString();
            return true;
        }

        /// <summary>
        /// Send swagger modified
        /// </summary>
        /// <param name="response"></param>
        /// <param name="swagger"></param>
        /// <returns></returns>
        private static async Task RespondWithSwaggerJson(HttpResponse response, OpenApiDocument swagger)
        {
            response.StatusCode = 200;
            response.ContentType = "application/json;charset=utf-8";

#pragma warning disable IDE0063 // Use simple 'using' statement
            using (var textWriter = new StringWriter())
            {
                var jsonWriter = new OpenApiJsonWriter(textWriter);

                swagger.SerializeAsV3(jsonWriter);

                string ret = SwaggerConventions.ApllyConventions(textWriter.ToString());

                await response.WriteAsync(ret, new UTF8Encoding(false));
            }
#pragma warning restore IDE0063 // Use simple 'using' statement
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}