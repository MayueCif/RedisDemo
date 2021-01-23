using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Part2
{
    public class RedisCacheMiddleware
    {

        private readonly RequestDelegate _next;

        public IConfiguration Configuration { get; }

        public RedisCacheMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            Configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            var path = context.Request.Path.Value.ToLower();
            
            if (path == "/home" || path == "/home/index")
            {

                var responseContent = "";

                //Copy a pointer to the original response body stream
                var originalBodyStream = context.Response.Body;

                using (var redis = ConnectionMultiplexer.Connect(Configuration.GetConnectionString("Redis")))
                {
                    var db = redis.GetDatabase();
                    if (db.KeyExists(path))
                    {
                        responseContent = db.StringGet(path);
                        await RespondWithIndexHtml(context.Response, responseContent);
                        return;
                    }
                    else
                    {
                        using (var responseBody = new MemoryStream())
                        {
                            //...and use that for the temporary response body
                            context.Response.Body = responseBody;

                            //Continue down the Middleware pipeline, eventually returning to this class
                            await _next(context);

                            //Format the response from the server
                            responseContent = await FormatResponse(context.Response);

                            //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
                            await responseBody.CopyToAsync(originalBodyStream);
                        }

                        db.StringSet(path, responseContent, expiry: TimeSpan.FromSeconds(30));
                    }
                }

            }

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }

        private async Task RespondWithIndexHtml(HttpResponse response,string html)
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";

            await response.WriteAsync(html, Encoding.UTF8);
        }


        private async Task<string> FormatResponse(HttpResponse response)
        {
            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            //Return the string for the response, including the status code (e.g. 200, 404, 401, etc.)
            return text;
        }

    }
}
