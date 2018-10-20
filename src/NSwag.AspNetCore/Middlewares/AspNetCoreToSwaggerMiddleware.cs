﻿//-----------------------------------------------------------------------
// <copyright file="SwaggerMiddleware.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.ApiDescription;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSwag.SwaggerGeneration;
using NSwag.SwaggerGeneration.AspNetCore;

namespace NSwag.AspNetCore.Middlewares
{
    /// <summary>Generates a Swagger specification on a given path.</summary>
    public class AspNetCoreToSwaggerMiddleware
    {
        private readonly RequestDelegate _nextDelegate;
        private readonly string _path;
        //private readonly SwaggerSettings<AspNetCoreToSwaggerGeneratorSettings> _settings;
        //private readonly SwaggerJsonSchemaGenerator _schemaGenerator;
        private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionGroupCollectionProvider;
        //private readonly IOptions<MvcOptions> _mvcOptions;
        //private readonly IOptions<MvcJsonOptions> _mvcJsonOptions;
        private readonly SwaggerDocumentProvider _documentProvider;

        private int _version;
        private string _schemaJson;
        private Exception _schemaException;
        private DateTimeOffset _schemaTimestamp;
        private string _documentName;

        /// <summary>Initializes a new instance of the <see cref="WebApiToSwaggerMiddleware"/> class.</summary>
        /// <param name="nextDelegate">The next delegate.</param>
        public AspNetCoreToSwaggerMiddleware(
            RequestDelegate nextDelegate,
            IServiceProvider serviceProvider,
            string path,
            string documentName)
        {
            //_settings = settings;
            //_path = settings.ActualSwaggerRoute;
            //_schemaGenerator = schemaGenerator;
            //_mvcOptions = mvcOptions;
            //_mvcJsonOptions = mvcJsonOptions;

            _nextDelegate = nextDelegate;

            _apiDescriptionGroupCollectionProvider = serviceProvider.GetService<IApiDescriptionGroupCollectionProvider>() ??
                throw new InvalidOperationException("API Explorer not registred in DI.");
            _documentProvider = serviceProvider.GetService<SwaggerDocumentProvider>() ??
                throw new InvalidOperationException("The NSwag DI services are not registered: Call " + nameof(SwaggerExtensions.AddSwagger) + " in ConfigureServices().");

            _path = path;
            _documentName = documentName;
        }

        /// <summary>Invokes the specified context.</summary>
        /// <param name="context">The context.</param>
        /// <returns>The task.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.HasValue && string.Equals(context.Request.Path.Value.Trim('/'), _path.Trim('/'), StringComparison.OrdinalIgnoreCase))
            {
                var schemaJson = await GenerateSwaggerAsync(context);
                context.Response.StatusCode = 200;
                context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
                await context.Response.WriteAsync(schemaJson);
            }
            else
                await _nextDelegate(context);
        }

        /// <summary>Generates the Swagger specification.</summary>
        /// <param name="context">The context.</param>
        /// <returns>The Swagger specification.</returns>
        protected virtual async Task<string> GenerateSwaggerAsync(HttpContext context)
        {
            //if (_schemaException != null && _schemaTimestamp + _settings.ExceptionCacheTime > DateTimeOffset.UtcNow)
            //    throw _schemaException;

            var apiDescriptionGroups = _apiDescriptionGroupCollectionProvider.ApiDescriptionGroups;
            if (apiDescriptionGroups.Version == Volatile.Read(ref _version) && _schemaJson != null)
                return _schemaJson;

            try
            {
                var document = await _documentProvider.GenerateAsync(_documentName);

                document.Host = context.Request.Host.Value ?? "";
                document.Schemes.Add(context.Request.Scheme == "http" ? SwaggerSchema.Http : SwaggerSchema.Https);
                document.BasePath = context.Request.PathBase.Value?.Substring(0, context.Request.PathBase.Value.Length - (/*_settings.MiddlewareBasePath?.Length ??*/ 0)) ?? "";

                //_settings.PostProcess?.Invoke(document);
                _schemaJson = document.ToJson();
                _schemaException = null;
                _version = apiDescriptionGroups.Version;
                _schemaTimestamp = DateTimeOffset.UtcNow;
            }
            catch (Exception exception)
            {
                _schemaJson = null;
                _schemaException = exception;
                _schemaTimestamp = DateTimeOffset.UtcNow;
                throw _schemaException;
            }

            return _schemaJson;
        }
    }
}
