using Microsoft.AspNetCore.Builder;

namespace eBolnicaAPI.Middleware
{
    public static class GlobalErrorHandlingExtensions
    {
        // ONLY this method - no service registration method
        public static IApplicationBuilder UseGlobalErrorHandling(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            return app;
        }
    }
}