using eBolnicaAPI.Models.Settings;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace eBolnicaAPI.Extensions
{
    public static class MedicationImageStaticFilesExtensions
    {
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public static IApplicationBuilder UseMedicationImageStaticFiles(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            var settings = app.ApplicationServices
                .GetRequiredService<IOptions<MedicationImageUploadSettings>>()
                .Value;

            var uploadsPath = Path.GetFullPath(
                Path.Combine(env.ContentRootPath, settings.StaticFilesPhysicalFolder));

            Directory.CreateDirectory(uploadsPath);

            var contentTypeProvider = CreateImageContentTypeProvider();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadsPath),
                RequestPath = settings.StaticFilesRequestPath,
                ContentTypeProvider = contentTypeProvider,
                ServeUnknownFileTypes = false,
                OnPrepareResponse = ctx => PrepareImageResponse(ctx, settings)
            });

            return app;
        }

        private static FileExtensionContentTypeProvider CreateImageContentTypeProvider()
        {
            var provider = new FileExtensionContentTypeProvider();

            provider.Mappings.Clear();
            provider.Mappings[".jpg"] = "image/jpeg";
            provider.Mappings[".jpeg"] = "image/jpeg";
            provider.Mappings[".png"] = "image/png";
            provider.Mappings[".webp"] = "image/webp";

            return provider;
        }

        private static void PrepareImageResponse(StaticFileResponseContext context, MedicationImageUploadSettings settings)
        {
            var extension = Path.GetExtension(context.File.Name);
            if (!AllowedImageExtensions.Contains(extension))
            {
                context.Context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Context.Response.ContentLength = 0;
                return;
            }

            var headers = context.Context.Response.Headers;

            var cacheControl = settings.StaticFileCacheImmutable
                ? $"public, max-age={settings.StaticFileCacheMaxAgeSeconds}, immutable"
                : $"public, max-age={settings.StaticFileCacheMaxAgeSeconds}";

            headers.CacheControl = cacheControl;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "SAMEORIGIN";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-site";
            headers.ContentDisposition = "inline";
        }
    }
}
