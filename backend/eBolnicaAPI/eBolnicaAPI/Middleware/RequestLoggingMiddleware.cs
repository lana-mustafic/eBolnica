using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace eBolnicaAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Log request start
            _logger.LogInformation("HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms",
              context.Request.Method, context.Request.Path,
              context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            
            try
            {
                await _next(context);
                stopwatch.Stop();

                // Log successful request
                _logger.LogInformation("HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method, context.Request.Path,
                    context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception)
            {
                stopwatch.Stop();

                // Log failed request (error will be logged by error handling middleware)
                _logger.LogError("HTTP {Method} {Path} failed after {ElapsedMs}ms",
                    context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}